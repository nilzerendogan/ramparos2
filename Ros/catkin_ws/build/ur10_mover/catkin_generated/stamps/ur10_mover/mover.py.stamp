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

def planCombat(plan):
    if sys.version_info >= (3,0):
        return plan[1]
    else:
        return plan
        
def plan_trajectory(move_group, destination_pose, start_joint_angles): 
    current_joint_state = JointState()
    current_joint_state.name = joint_names
    current_joint_state.position = start_joint_angles

    moveit_robot_state = RobotState()
    moveit_robot_state.joint_state = current_joint_state
    move_group.set_start_state(moveit_robot_state)

    move_group.set_pose_target(destination_pose)
    plan = move_group.plan()

    if not plan:
        exception_str = """
            Trajectory could not be planned for a destination of {} with starting joint angles {}.
            Please make sure target and destination are reachable by the robot.
        """.format(destination_pose, destination_pose)
        raise Exception(exception_str)

    return planCombat(plan)

def execute_joint_angles(joint_angles,group):
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

        trajectory = plan_trajectory(move_group,pose,previous_ending_joint_angles) 
        if not trajectory.joint_trajectory.points:
            rospy.logerr("AN ERROR OCCURED WHILE PLANNING")
            rospy.logerr(pose)
            response.output_msg = "Timeout"
            if len(response.trajectories) > 0:
                save_trajectory(response.trajectories)
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
        point= [float(i) for i in point[1:-2].split(',')]
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
    
    (plan, fraction) = move_group.compute_cartesian_path(waypoints, 0.01, 0.0)
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
    response = StateServiceResponse()
    try:
        # UPDATE: Fetch joints directly from the xArm API instead of the UR10 class
        if arm is not None and arm.connected:
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
    
    if arm is None or not arm.connected:
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
        print(f'{joint_angles * 180 / 3.14}') # Keep your original logging
        
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
    rospy.Service("execute",ExecutionService, execute_on_real_robot)
    rospy.Service("discard",DiscardService, discard_last_trajectory)

    print("Service is ready to plan")
    rospy.spin()


rospy.init_node('ur10_mover_server')

# ---------------------------------------------------------
# XARM HARDWARE INITIALIZATION
# ---------------------------------------------------------
robot_ip = '192.168.1.225' # CHANGE THIS TO YOUR XARM IP
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