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
# IMPORT XARM API (Replaces ur10_interface)
from xarm.wrapper import XArmAPI
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

# UPDATE: xArm7 uses 7 sequential joint names
joint_names = ['joint1', 'joint2', 'joint3', 'joint4', 'joint5', 'joint6', 'joint7']

# How densely to interpolate each waypoint-to-waypoint segment in Cartesian space.
# Smaller = smoother / more faithful to the drawn line, but more points to execute.
CARTESIAN_EEF_STEP = 0.01
# Minimum fraction of a segment that must be completed for us to accept the plan.
# If compute_cartesian_path can't get all the way to the goal in a straight line
# (e.g. near a joint limit / workspace edge), we still take what we got but warn loudly,
# rather than silently letting a downstream planner improvise a different elbow config.
MIN_ACCEPTABLE_FRACTION = 0.99


def planCombat(plan):
    if sys.version_info >= (3, 0):
        return plan[1]
    else:
        return plan


def plan_trajectory(move_group, destination_pose, start_joint_angles):
    """
    Plan a single-waypoint segment from start_joint_angles to destination_pose.

    CHANGED: previously this used set_pose_target() + plan(), which hands the
    problem to OMPL/RRTConnect (a sampling-based JOINT-SPACE planner). For a
    kinematically redundant 7-DOF arm like the xArm7 (vs. the UR10e's 6-DOF),
    that lets the planner return a valid solution using a different elbow/wrist
    configuration than the previous waypoint used -- the interpolated joint path
    then no longer traces a straight line in Cartesian space, and the error is
    worst near the top/bottom of the reachable workspace, which is exactly the
    symptom you were seeing.

    Now we use compute_cartesian_path() for the single segment instead. Because
    set_start_state() below seeds it with the actual joint angles the arm ended
    the previous segment in, IK stays in a consistent configuration from one
    waypoint to the next, and the path between the two poses is a genuine
    straight line rather than "whatever RRTConnect happened to find".
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


def ensure_arm_connected():
    """
    Make sure `arm` has a live connection before we try to use it.

    The USB-Ethernet adapter used to talk to the physical xArm7 has been known
    to drop after a sleep/restart cycle on the host machine. Without this
    check, that shows up as `return_joint_state`/`execute_on_real_robot`
    silently failing (or throwing) until `mover.py` is killed and restarted
    by hand. Instead: if `arm.connected` is False, try to (re)connect once
    before giving up. This does NOT retry indefinitely / block for a long
    time -- if the adapter itself is unplugged or the robot is powered off,
    this will still fail fast and report why.
    """
    global arm
    if arm is not None and arm.connected:
        return True

    rospy.logwarn("xArm connection not active, attempting to reconnect to {}...".format(robot_ip))
    try:
        if arm is not None:
            try:
                arm.disconnect()
            except Exception:
                pass
        arm = XArmAPI(robot_ip)
        arm.motion_enable(enable=True)
        arm.set_mode(0)
        arm.set_state(state=0)
        if arm.connected:
            rospy.loginfo("Reconnected to physical xArm7 at {}".format(robot_ip))
            return True
        rospy.logerr("Reconnect attempt to {} did not report connected.".format(robot_ip))
        return False
    except Exception as e:
        rospy.logerr("Reconnect attempt to {} failed: {}".format(robot_ip, e))
        return False


def return_joint_state(req):
    response = StateServiceResponse()
    try:
        # UPDATE: Fetch joints directly from the xArm API instead of the UR10 class
        if ensure_arm_connected():
            code, current_joint_angles = arm.get_servo_angle(is_radian=True)
            if code != 0 or len(current_joint_angles) < 7:
                response.output_msg = "Driver could not be reached or invalid joint count"
                return response
        else:
            response.output_msg = "Hardware not connected"
            return response
    except Exception as e:
        response.output_msg = f"Driver could not be reached: {e}"
        return response

    response.output_msg = "success"
    response.current_joint_angles = current_joint_angles
    return response


def execute_on_real_robot(req):
    response = ExecutionServiceResponse()

    if not ensure_arm_connected():
        response.output_msg = "Error: Physical xArm is not connected."
        rospy.logerr("Execute triggered, but no physical xArm7 is connected.")
        return response

    traj = []
    for joint_state in req.joint_states:
        traj.append([])
        for joint in joint_state.list:
            traj[-1].append(joint)

    traj = np.array(traj)
    rospy.loginfo(f"Executing trajectory with {traj.shape[0]} waypoints.")

    for joint_angles in traj:
        print(f'{joint_angles * 180 / 3.14}')  # Keep your original logging

    # UPDATE: Execution logic using xArm API
    arm.clean_error()
    arm.clean_warn()
    arm.motion_enable(enable=True)
    arm.set_state(state=0)

    # Send the trajectory to the real robot
    # You may need to tune speed and mvacc for your specific RAMPA use-case
    for joint_angles in traj:
        code = arm.set_servo_angle(angle=joint_angles, speed=0.5, mvacc=5, wait=True, radius=0, is_radian=True)
        if code != 0:
            rospy.logerr(f"xArm execution failed with error code: {code}")
            response.output_msg = f"Failed with code {code}"
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

# ---------------------------------------------------------
# XARM HARDWARE INITIALIZATION
# ---------------------------------------------------------
robot_ip = '192.168.1.225'  # CHANGE THIS TO YOUR XARM IP
arm = None

try:
    arm = XArmAPI(robot_ip)
    arm.motion_enable(enable=True)
    arm.set_mode(0)
    arm.set_state(state=0)
    rospy.loginfo(f"Successfully connected to physical xArm7 at {robot_ip}")
except Exception as e:
    rospy.logwarn(f"Could not connect to physical xArm7 at {robot_ip}. Running without hardware execution. Error: {e}")

# UPDATE: MoveIt Planning Group for xArm7
group_name = "xarm7"
move_group = moveit_commander.MoveGroupCommander(group_name)

rospy.sleep(2)

if __name__ == "__main__":
    moveit_server()
