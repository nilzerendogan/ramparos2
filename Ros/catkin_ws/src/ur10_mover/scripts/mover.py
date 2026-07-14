#!/usr/bin/env python3

from __future__ import print_function

import rospy
import sys
import copy
import moveit_commander
import numpy as np
import os

from OneEuroFilter import OneEuroFilter
from sensor_msgs.msg import JointState
from moveit_msgs.msg import RobotState, RobotTrajectory
from geometry_msgs.msg import Pose
from trajectory_msgs.msg import JointTrajectoryPoint, MultiDOFJointTrajectoryPoint

from ur10_mover.srv import PlannerService, PlannerServiceRequest, PlannerServiceResponse
from ur10_mover.srv import StateService, StateServiceRequest, StateServiceResponse
from ur10_mover.srv import ExecutionService, ExecutionServiceRequest, ExecutionServiceResponse
from ur10_mover.srv import DiscardService, DiscardServiceRequest, DiscardServiceResponse

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
MIN_ACCEPTABLE_FRACTION = 0.99
# Default speed scaling applied when time-parameterizing a real-robot trajectory.
# 1.0 = as fast as the planned/joint-limit-respecting trajectory allows.
# Keep this low for your first few live runs, then raise it once you trust the motion.
EXECUTION_VELOCITY_SCALING = 0.3


HOME_POSE = [-3.10, -0.3, 0.0, 0.5, 0.0, 0.8, 0.0]  # radians — pick values you've verified are safe/reachable

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


def plan_trajectory(move_group, destination_pose, start_joint_angles):
    """
    Plan a single-waypoint segment from start_joint_angles to destination_pose
    using compute_cartesian_path(), seeded with the previous segment's actual
    ending joint angles, so IK stays in a consistent elbow/wrist configuration
    from waypoint to waypoint instead of RRTConnect picking a new one each time.
    """
    current_joint_state = JointState()
    current_joint_state.name = joint_names
    current_joint_state.position = start_joint_angles

    moveit_robot_state = RobotState()
    moveit_robot_state.joint_state = current_joint_state
    move_group.set_start_state(moveit_robot_state)

    waypoints = [copy.deepcopy(destination_pose)]
    (plan, fraction) = move_group.compute_cartesian_path(
        waypoints, CARTESIAN_EEF_STEP, avoid_collisions=True
    )

    if fraction < MIN_ACCEPTABLE_FRACTION or not plan.joint_trajectory.points:
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

        trajectory = plan_trajectory(move_group, pose, previous_ending_joint_angles)
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


def execute_on_real_robot(req):
    """
    1. Move to HOME_POSE (known-safe reference pose).
    2. Free (joint-space) plan+execute from HOME_POSE to the trajectory's
       first drawn point — NOT cartesian, since this can be a large jump
       away from home and forcing a straight-line EEF path here is both
       unnecessary and prone to rejection.
    3. Execute the recorded drawn trajectory itself via MoveIt, starting
       cleanly from traj[0] since we just arrived there in step 2.
    """
    response = ExecutionServiceResponse()

    traj = [list(joint_state.list) for joint_state in req.joint_states]
    if not traj:
        response.output_msg = "Error: empty trajectory"
        return response

    move_group.set_start_state_to_current_state()   # <-- ADD THIS: belt-and-suspenders, always plan from the real robot
    rospy.loginfo("MoveIt believes current state is: {}".format(move_group.get_current_joint_values()))

    rospy.loginfo("Executing trajectory with {} waypoints via MoveIt.".format(len(traj)))
    for joint_angles in traj:
        print('{}'.format(np.array(joint_angles) * 180 / 3.14))

    # --- Step 1: go to known-safe home pose ---
    rospy.loginfo("Homing before execution...")

    rospy.loginfo("Active joints: {}".format(move_group.get_active_joints()))
    for name in move_group.get_active_joints():
        joint = robot.get_joint(name)
        rospy.loginfo("{}: [{}, {}]".format(name, joint.bounds()[0], joint.bounds()[1]))
    rospy.loginfo("HOME_POSE: {}".format(HOME_POSE))

    move_group.set_joint_value_target(dict(zip(joint_names, HOME_POSE)))
    
    home_plan = planCombat(move_group.plan())
    if not home_plan or not home_plan.joint_trajectory.points:
        response.output_msg = "Failed: could not plan move to home pose"
        return response
    if not move_group.execute(home_plan, wait=True):
        response.output_msg = "Failed: could not reach home pose"
        return response
    move_group.stop()
    move_group.clear_pose_targets()

    if not wait_until_at_pose(move_group, HOME_POSE):
        response.output_msg = "Failed: robot state did not converge to home pose in time"
        return response

    # --- Step 2: free joint-space approach from home to traj[0] ---
    rospy.loginfo("Approaching trajectory start point...")
    move_group.set_joint_value_target(traj[0])
    approach_plan = planCombat(move_group.plan())
    if not approach_plan or not approach_plan.joint_trajectory.points:
        response.output_msg = "Failed: could not plan approach from home to trajectory start"
        return response
    if not move_group.execute(approach_plan, wait=True):
        response.output_msg = "Failed: approach move execution failed"
        return response
    move_group.stop()
    move_group.clear_pose_targets()

    # --- Step 3: execute the recorded drawn trajectory ---
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

    success = move_group.execute(robot_traj, wait=True)
    move_group.stop()

    if not success:
        rospy.logerr("MoveIt execute() reported failure.")
        response.output_msg = "Failed: move_group.execute() returned False"
        return response

    rospy.loginfo("Trajectory execution completed on physical hardware.")
    response.output_msg = "success"
    return response


def moveit_server():
    moveit_commander.roscpp_initialize(sys.argv)

    rospy.Service('planner', PlannerService, plan_pick_and_place)
    rospy.Service("get_joint_state", StateService, return_joint_state)
    rospy.Service("execute", ExecutionService, execute_on_real_robot)
    rospy.Service("discard", DiscardService, discard_last_trajectory)

    print("Service is ready to plan")
    rospy.spin()


rospy.init_node('ur10_mover_server')

# xArm7 MoveIt planning group. This talks to the arm ONLY through the
# connection realMove_exec.launch already established -- no direct
# XArmAPI socket is opened from this node.
group_name = "xarm7"
robot = moveit_commander.RobotCommander()
move_group = moveit_commander.MoveGroupCommander(group_name)

rospy.sleep(2)

if __name__ == "__main__":
    moveit_server()