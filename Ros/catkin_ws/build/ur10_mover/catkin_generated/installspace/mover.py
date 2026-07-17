#!/usr/bin/env python3
from __future__ import print_function
import rospy
import sys
import copy
import moveit_commander
import numpy as np
import os
import actionlib
import threading
from OneEuroFilter import OneEuroFilter
from sensor_msgs.msg import JointState
from moveit_msgs.msg import RobotState, RobotTrajectory
from geometry_msgs.msg import Pose
from trajectory_msgs.msg import JointTrajectoryPoint, MultiDOFJointTrajectoryPoint
from control_msgs.msg import GripperCommandAction, GripperCommandGoal
from std_msgs.msg import String
from ur10_mover.srv import PlannerService, PlannerServiceRequest, PlannerServiceResponse
from ur10_mover.srv import StateService, StateServiceRequest, StateServiceResponse
from ur10_mover.srv import ExecutionService, ExecutionServiceRequest, ExecutionServiceResponse
from ur10_mover.srv import DiscardService, DiscardServiceRequest, DiscardServiceResponse
from ur10_mover.srv import GripperService, GripperServiceRequest, GripperServiceResponse
from geometry_msgs.msg import Transform
# ---------------------------------------------------------
# NOTE: We deliberately do NOT import/instantiate XArmAPI here anymore.
#
# realMove_exec.launch (terminal 1) already opens the xarm_ros driver's
# connection to the physical arm and exposes it to the rest of ROS via
# MoveIt's move_group action/topic interface. xArm controllers only accept
# one active control session at a time. A second raw `XArmAPI(robot_ip)`
# connection from this node fights that one for write access, which is what
# produced the `Write() failed, failed_ret=9` errors -> Robot State 5 (STOP)
# -> Hardware Emergency STOP on your last hardware run.
#
# Everything that used to go through a second XArmAPI connection (reading
# joint state, executing trajectories on the real arm) now goes through
# `move_group`, which reuses the ONE connection terminal 1 already owns.
#
# The gripper is the one exception: it is controlled via the standard
# control_msgs/GripperCommandAction action server that realMove_exec.launch
# already exposes at /xarm/xarm_gripper/gripper_action (enabled by that
# launch file's add_gripper:=true argument), so this does NOT open a second
# XArmAPI connection either.
# ---------------------------------------------------------
config_one_euro_filter = {
    'freq': 120,
    'mincutoff': 1,
    'beta': 0.001,
    'dcutoff': 1.0
}
f_x = OneEuroFilter(**config_one_euro_filter)
f_y = OneEuroFilter(**config_one_euro_filter)
f_z = OneEuroFilter(**config_one_euro_filter)
# xArm7 uses 7 sequential joint names
joint_names = ['joint1', 'joint2', 'joint3', 'joint4', 'joint5', 'joint6', 'joint7']
# How densely to interpolate each waypoint-to-waypoint segment in Cartesian space.
# Smaller = smoother / more faithful to the drawn line, but more points to execute.
CARTESIAN_EEF_STEP = 0.01
# Minimum fraction of a segment that must be completed for us to accept the plan.
MIN_ACCEPTABLE_FRACTION = 0.3
# --- Real-time hand-tracking overrides (plan_trajectory only) ---
# Sahnede collidable obje olmayacağı için real-time yolda collision kontrolü
# tamamen kapatılıyor ve interpolasyon adımı büyütülüyor; bu ikisi round-trip
# gecikmesini düşürüp ghost'un ele daha az gecikmeyle tepki vermesini sağlar.
# NOT: self-collision kontrolü de devre dışı kalıyor — kolun kendine
# çarpmasına karşı artık hiçbir yazılımsal koruma yok.
REALTIME_CARTESIAN_EEF_STEP = 0.1
# Real-time'da hedefin sadece %30'una kadar giden bir plan bile kabul
# edilip yürütülüyor. Bu, el çok hızlı hareket ettiğinde kolun hedefin
# önemli ölçüde gerisinde bir noktada durabileceği anlamına gelir.
REALTIME_MIN_ACCEPTABLE_FRACTION = 0.3
# Default speed scaling applied when time-parameterizing a real-robot trajectory.
# 1.0 = as fast as the planned/joint-limit-respecting trajectory allows.
# Keep this low for your first few live runs, then raise it once you trust the motion.
EXECUTION_VELOCITY_SCALING = 0.1
HOME_POSE = [-3.10, -0.3, 0.0, 0.5, 0.0, 0.8, 0.0]  # radians — pick values you've verified are safe/reachable

_last_realtime_pose = None
_last_realtime_stamp = None
ESTIMATED_ROUND_TRIP = 0.08 

def go_to_home(move_group):
    move_group.set_joint_value_target(HOME_POSE)
    success = move_group.go(wait=True)
    move_group.stop()
    move_group.clear_pose_targets()
    return success
def planCombat(plan):
    if sys.version_info >= (3, 0):
        return plan[1]
    else:
        return plan
def wait_until_at_pose(move_group, target, tolerance=0.01, timeout=3.0, stable_reads=3):
    start_time = rospy.Time.now()
    consecutive = 0
    while (rospy.Time.now() - start_time).to_sec() < timeout:
        current = move_group.get_current_joint_values()
        if all(abs(c - t) < tolerance for c, t in zip(current, target)):
            consecutive += 1
            if consecutive >= stable_reads:
                rospy.sleep(0.3)  # extra buffer for other subscribers to catch up
                return True
        else:
            consecutive = 0
        rospy.sleep(0.05)
    return False
def plan_trajectory(move_group, destination_pose, start_joint_angles, use_cartesian=True):
    """
    ...
    use_cartesian=False -> OMPL/RRTConnect. Real-time el takibinde bu yolu
                            kullanıyoruz. Ardışık istekler birbirine çok yakın
                            küçük diferansiyel hareketler olduğu için IK zaten
                            kolay; planning_time/attempts'i yüksek tutmak
                            round-trip'i gereksiz uzatıp Unity tarafında
                            backlog'a (ghost'un elin gerisinde kalmasına)
                            yol açıyordu. Bu yüzden düşürüldü.
    """
    current_joint_state = JointState()
    current_joint_state.name = joint_names
    current_joint_state.position = start_joint_angles
    moveit_robot_state = RobotState()
    moveit_robot_state.joint_state = current_joint_state
    move_group.set_start_state(moveit_robot_state)
    if not use_cartesian:
        move_group.set_planning_time(0.1)        # eskiden 0.5 — real-time için düşürüldü
        move_group.set_num_planning_attempts(1)   # eskiden 5 — round-trip'i hızlandırmak için
        move_group.set_pose_target(destination_pose)
        plan = planCombat(move_group.plan())
        move_group.clear_pose_targets()
        if not plan or not plan.joint_trajectory.points:
            rospy.logwarn(
                "OMPL plan failed for pose {} (starting from {})".format(
                    destination_pose, start_joint_angles
                )
            )
            return None
        return plan
    waypoints = [copy.deepcopy(destination_pose)]
    (plan, fraction) = move_group.compute_cartesian_path(
        waypoints, REALTIME_CARTESIAN_EEF_STEP, avoid_collisions=False
    )
    if fraction < REALTIME_MIN_ACCEPTABLE_FRACTION or not plan.joint_trajectory.points:
        rospy.logwarn(
            "Cartesian path only {:.1f}% complete for pose {} (starting from {}) — "
            "rejecting rather than executing a partial path.".format(
                fraction * 100, destination_pose, start_joint_angles
            )
        )
        return None
    return plan
def execute_joint_angles(joint_angles, group):
    group.set_joint_value_target(joint_angles)
    plan = group.plan()
    group.execute(plan[1])
    return
def plan_pick_and_place(req):
    rospy.loginfo("Pose received:")
    for pose in req.pose_list:
        rospy.loginfo(pose)
    rospy.loginfo(rospy.get_caller_id() + "Plan Requested:\n")
    response = PlannerServiceResponse()
    response.request_type = req.request_type
    if (req.request_type == "poses_"):
        robot_trajectory = cartesian_path(response, req)
        robot_trajectory.multi_dof_joint_trajectory.points = []
        for point in robot_trajectory.joint_trajectory.points:
            move_group.set_joint_value_target(point.positions)
            move_group.go()
            end_pose = move_group.get_current_pose().pose
            multi_dof = MultiDOFJointTrajectoryPoint()
            transform = Transform()
            transform.translation.x = end_pose.position.x
            transform.translation.y = end_pose.position.y
            transform.translation.z = end_pose.position.z
            transform.rotation.x = end_pose.orientation.x
            transform.rotation.y = end_pose.orientation.y
            transform.rotation.z = end_pose.orientation.z
            transform.rotation.w = end_pose.orientation.w
            multi_dof.transforms = [transform]
            robot_trajectory.multi_dof_joint_trajectory.points.append(multi_dof)
        rospy.loginfo(robot_trajectory)
        response.trajectories = [robot_trajectory]
        return response
    rospy.loginfo("Recieved pose count is:")
    rospy.loginfo(len(req.pose_list))
    current_pose = move_group.get_current_pose().pose
    previous_ending_joint_angles = req.joints_input
    for pose in req.pose_list:
        norm = (pose.orientation.x**2 + pose.orientation.y**2 + pose.orientation.z**2 + pose.orientation.w**2)**0.5
        pose.orientation.x /= norm
        pose.orientation.y /= norm
        pose.orientation.z /= norm
        pose.orientation.w /= norm
        rospy.loginfo(pose)
        trajectory = plan_trajectory(
            move_group, pose, previous_ending_joint_angles,
            use_cartesian=True
            #use_cartesian=(req.request_type != "realTime")
        )
        if trajectory is None or not trajectory.joint_trajectory.points:
            rospy.logerr("AN ERROR OCCURED WHILE PLANNING")
            rospy.logerr(pose)
            response.output_msg = "Timeout"
            return response
        previous_ending_joint_angles = trajectory.joint_trajectory.points[-1].positions
        response.trajectories.append(trajectory)
    move_group.set_start_state_to_current_state() 
    move_group.clear_pose_targets()
    save_trajectory(response.trajectories)
    response.pose_list = req.pose_list
    return response
def convert_data_file_to_list(input_file):
    traj = []
    saved_trajectory = input_file.readlines()
    input_file.close()
    for point in saved_trajectory:
        point = [float(i) for i in point[1:-2].split(',')]
        traj.append(point)
    rospy.loginfo("traj")
    rospy.loginfo(traj)
    return traj
def cartesian_path(response, req):
    rospy.loginfo("Calculating cartesian path")
    waypoints = []
    for pose in req.pose_list:
        pose.orientation.x = round(pose.orientation.x, 2)
        pose.orientation.y = round(pose.orientation.y, 2)
        pose.orientation.z = round(pose.orientation.z, 2)
        pose.orientation.w = round(pose.orientation.w, 2)
        rospy.loginfo(pose)
        waypoints.append(copy.deepcopy(pose))
    (plan, fraction) = move_group.compute_cartesian_path(waypoints, CARTESIAN_EEF_STEP, avoid_collisions=True)
    if fraction < MIN_ACCEPTABLE_FRACTION:
        rospy.logwarn("Cartesian path only {:.1f}% complete across full waypoint list.".format(fraction * 100))
    return plan
def save_trajectory(trajectory):
    traj = []
    for joint_state in trajectory:
        for point in joint_state.joint_trajectory.points:
            point = point.positions
            traj.append(point)
    traj = np.array(traj)
    np.save('trajectory.npy', traj)
def discard_last_trajectory(req):
    response = DiscardServiceResponse()
    if os.path.exists('trajectory.npy'):
        os.remove('trajectory.npy')
    response.output_msg = "success"
    return response
def extrapolate_pose(pose):
    global _last_realtime_pose, _last_realtime_stamp
    now = rospy.Time.now().to_sec()
    if _last_realtime_pose is not None and _last_realtime_stamp is not None:
        dt = now - _last_realtime_stamp
        if dt > 1e-3:
            vx = (pose.position.x - _last_realtime_pose.position.x) / dt
            vy = (pose.position.y - _last_realtime_pose.position.y) / dt
            vz = (pose.position.z - _last_realtime_pose.position.z) / dt

            filtered_vx = f_x(vx, now)
            filtered_vy = f_y(vy, now)
            filtered_vz = f_z(vz, now)

            pose.position.x += filtered_vx * ESTIMATED_ROUND_TRIP
            pose.position.y += filtered_vy * ESTIMATED_ROUND_TRIP
            pose.position.z += filtered_vz * ESTIMATED_ROUND_TRIP

    _last_realtime_pose = copy.deepcopy(pose)
    _last_realtime_stamp = now
    return pose
def handle_gripper(req):
    """
    Opens/closes the xArm7 gripper via the control_msgs/GripperCommandAction
    action server already exposed by realMove_exec.launch (add_gripper:=true),
    at /xarm/xarm_gripper/gripper_action. This avoids opening a second
    XArmAPI connection to the robot (see the note at the top of this file).
    """
    response = GripperServiceResponse()
    goal = GripperCommandGoal()
    if req.input_msg == "close":
        goal.command.position = 0.41  # ESTIMATE for ~6cm gap — needs hardware verification
        # NOTE: known data points on this gripper's command scale (larger
        # value = more closed): 0.085 = fully open (~max gap), 0.6 = ~3cm gap,
        # 0.8 = nearly fully closed. This 0.32 is a rough linear extrapolation
        # toward a larger (~6cm) gap, NOT a measured value. Test on hardware
        # and adjust with bisection: if the gap is too small, raise the
        # number a bit (e.g. +0.03); if too large, lower it; halve the step
        # each time you overshoot, rather than jumping straight to a new
        # round number.
        goal.command.max_effort = 5.0
    elif req.input_msg == "open":
        goal.command.position = 0.085  # fully open
        goal.command.max_effort = 5.0
    else:
        response.output_msg = "Unknown command: {}".format(req.input_msg)
        return response
    gripper_client.send_goal(goal)
    finished_in_time = gripper_client.wait_for_result(rospy.Duration(3.0))
    if not finished_in_time:
        rospy.logwarn("Gripper action did not finish within timeout for command: {}".format(req.input_msg))
        response.output_msg = "Timeout waiting for gripper action result"
        return response
    rospy.loginfo("Gripper command '{}' completed.".format(req.input_msg))
    response.output_msg = "success"
    return response
def return_joint_state(req):
    """
    Read current joint angles from MoveIt's view of the robot state, which is
    fed by the /joint_states topic published by the xarm_ros driver that
    terminal 1 already owns. No separate hardware connection needed.
    """
    response = StateServiceResponse()
    try:
        current_joint_angles = move_group.get_current_joint_values()
        if len(current_joint_angles) < 7:
            response.output_msg = "Invalid joint count from MoveIt ({})".format(len(current_joint_angles))
            return response
    except Exception as e:
        response.output_msg = "Could not read joint state via MoveIt: {}".format(e)
        return response
    response.output_msg = "success"
    response.current_joint_angles = current_joint_angles
    return response
# ---------------------------------------------------------
# execute_on_real_robot is split into two parts so the "execute" ROS Service
# returns IMMEDIATELY instead of blocking until the whole trajectory (which
# can take several seconds) has finished.
#
# Two separate problems were stacked here:
#  1. move_group.execute(..., wait=True) blocked this Service handler until
#     the whole motion finished. Because the ROS-TCP-Connector endpoint
#     processes incoming requests over a single connection, ANY other
#     service call from Unity (e.g. the gripper B-button toggle) sat queued
#     behind it until execute() finally returned.
#  2. Simply moving that same wait=True call onto a background thread is
#     NOT enough to fix it: moveit_commander's execute() is a blocking call
#     into compiled (C++) bindings, and depending on how that binding is
#     implemented it may hold the Python GIL for the whole motion duration.
#     If it does, no other Python thread -- including the one handling the
#     gripper service -- gets to run either, even though it's "in a thread".
#
# The actual fix is wait=False: this dispatches the trajectory goal to the
# joint trajectory controller and returns almost immediately, without
# blocking Python at all for the motion duration. We track completion with
# lightweight polling (wait_until_at_pose, which only does cheap
# get_current_joint_values() calls) instead of a long blocking call.
# ---------------------------------------------------------
execution_in_progress = threading.Event()
execution_status_pub = rospy.Publisher('/rampa/execution_status', String, queue_size=1)
def _run_execution(traj):
    try:
        move_group.set_start_state_to_current_state()  # always plan from wherever the real robot currently is
        rospy.loginfo("MoveIt believes current state is: {}".format(move_group.get_current_joint_values()))
        rospy.loginfo("Executing trajectory with {} waypoints via MoveIt.".format(len(traj)))
        for joint_angles in traj:
            print('{}'.format(np.array(joint_angles) * 180 / 3.14))
        # --- Step 1: free joint-space approach from current pose to traj[0] ---
        rospy.loginfo("Approaching trajectory start point...")
        move_group.set_joint_value_target(traj[0])
        approach_plan = planCombat(move_group.plan())
        if not approach_plan or not approach_plan.joint_trajectory.points:
            rospy.logerr("Failed: could not plan approach from current pose to trajectory start")
            execution_status_pub.publish("failed: could not plan approach from current pose to trajectory start")
            return

        # wait=False dispatches the goal to the trajectory controller and
        # returns almost immediately -- unlike wait=True, this does NOT hold
        # the Python interpreter (or the GIL) blocked for the whole motion
        # duration. We poll get_current_joint_values() instead, which are
        # quick, cheap calls that don't starve other threads (like the
        # gripper service handler) the way a long blocking execute() call can.
        move_group.execute(approach_plan, wait=False)
        approach_target = approach_plan.joint_trajectory.points[-1].positions
        approach_duration = approach_plan.joint_trajectory.points[-1].time_from_start.to_sec()
        if not wait_until_at_pose(move_group, approach_target, tolerance=0.02,
                                   timeout=max(approach_duration * 2.0, 10.0)):
            rospy.logwarn("Approach did not settle within timeout, continuing anyway")
        move_group.stop()
        move_group.clear_pose_targets()
        # --- Step 2: execute the recorded drawn trajectory ---
        robot_traj = RobotTrajectory()
        robot_traj.joint_trajectory.joint_names = joint_names
        for positions in traj:
            pt = JointTrajectoryPoint()
            pt.positions = list(positions)
            robot_traj.joint_trajectory.points.append(pt)
        try:
            current_state = robot.get_current_state()
            robot_traj = move_group.retime_trajectory(
                current_state, robot_traj, velocity_scaling_factor=EXECUTION_VELOCITY_SCALING
            )
        except Exception as e:
            rospy.logwarn("retime_trajectory failed, executing with default timing: {}".format(e))

        move_group.execute(robot_traj, wait=False)
        final_target = robot_traj.joint_trajectory.points[-1].positions
        final_duration = robot_traj.joint_trajectory.points[-1].time_from_start.to_sec()
        success = wait_until_at_pose(move_group, final_target, tolerance=0.02,
                                      timeout=max(final_duration * 2.0, 15.0))
        move_group.stop()
        if not success:
            rospy.logerr("Trajectory did not reach final pose within timeout.")
            execution_status_pub.publish("failed: did not reach final pose within timeout")
            return
        rospy.loginfo("Trajectory execution completed on physical hardware.")
        execution_status_pub.publish("success")
    finally:
        execution_in_progress.clear()
def execute_on_real_robot(req):
    """
    Kicks off trajectory execution on a background thread and returns
    immediately with "started". See the note above _run_execution() for why.
    Actual success/failure is published on /rampa/execution_status.
    """
    response = ExecutionServiceResponse()
    traj = [list(joint_state.list) for joint_state in req.joint_states]
    if not traj:
        response.output_msg = "Error: empty trajectory"
        return response
    if execution_in_progress.is_set():
        response.output_msg = "Error: execution already in progress"
        return response
    execution_in_progress.set()
    threading.Thread(target=_run_execution, args=(traj,), daemon=True).start()
    response.output_msg = "started"
    return response
def moveit_server():
    moveit_commander.roscpp_initialize(sys.argv)
    rospy.Service('planner', PlannerService, plan_pick_and_place)
    rospy.Service("get_joint_state", StateService, return_joint_state)
    rospy.Service("execute", ExecutionService, execute_on_real_robot)
    rospy.Service("discard", DiscardService, discard_last_trajectory)
    rospy.Service("gripper", GripperService, handle_gripper)
    print("Service is ready to plan")
    rospy.spin()
rospy.init_node('ur10_mover_server')
# xArm7 MoveIt planning group. This talks to the arm ONLY through the
# connection realMove_exec.launch already established -- no direct
# XArmAPI socket is opened from this node.
group_name = "xarm7"
robot = moveit_commander.RobotCommander()
move_group = moveit_commander.MoveGroupCommander(group_name)
# Gripper action client — talks to the gripper action server that
# realMove_exec.launch already exposes (add_gripper:=true), reusing the
# single hardware connection terminal 1 owns rather than opening a second one.
gripper_client = actionlib.SimpleActionClient(
    '/xarm/xarm_gripper/gripper_action', GripperCommandAction
)
gripper_client.wait_for_server()
rospy.sleep(2)
if __name__ == "__main__":
    moveit_server()