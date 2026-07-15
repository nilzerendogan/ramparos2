#!/usr/bin/env python3
"""
go_home.py

Standalone helper node — run manually from a ROS terminal to send the
xArm7 to the same HOME_POSE used in ur10_mover_server.py.

Usage:
    rosrun <your_package> go_home.py

Requirements:
    - realMove_exec.launch (or the sim equivalent) must already be running,
      since this connects to the SAME move_group action interface, not a
      separate hardware connection.
    - chmod +x go_home.py before rosrun will find it as executable.
"""

from __future__ import print_function

import sys
import rospy
import moveit_commander

# Keep this identical to HOME_POSE in ur10_mover_server.py so both scripts
# always agree on where "home" is.
HOME_POSE = [-3.10, -0.3, 0.0, 0.5, 0.0, 0.8, 0.0]  # radians

GROUP_NAME = "xarm7"


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
                rospy.sleep(0.3)
                return True
        else:
            consecutive = 0
        rospy.sleep(0.05)
    return False


def go_home():
    moveit_commander.roscpp_initialize(sys.argv)
    rospy.init_node('go_home_node', anonymous=True)

    robot = moveit_commander.RobotCommander()
    move_group = moveit_commander.MoveGroupCommander(GROUP_NAME)

    rospy.sleep(1)

    rospy.loginfo("Current joint values: {}".format(move_group.get_current_joint_values()))
    rospy.loginfo("Planning move to HOME_POSE: {}".format(HOME_POSE))

    move_group.set_start_state_to_current_state()
    move_group.set_joint_value_target(HOME_POSE)

    plan = planCombat(move_group.plan())
    if not plan or not plan.joint_trajectory.points:
        rospy.logerr("Failed: could not plan a path to HOME_POSE.")
        return False

    success = move_group.execute(plan, wait=True)
    move_group.stop()
    move_group.clear_pose_targets()

    if not success:
        rospy.logerr("Failed: move_group.execute() returned False.")
        return False

    if not wait_until_at_pose(move_group, HOME_POSE):
        rospy.logwarn("Execute reported success but robot did not settle at HOME_POSE within timeout.")
        return False

    rospy.loginfo("Robot reached HOME_POSE successfully.")
    return True


if __name__ == "__main__":
    try:
        ok = go_home()
        sys.exit(0 if ok else 1)
    except rospy.ROSInterruptException:
        pass
