xArm7的urdf文件

#运行官方示例后将/robot_description话题导出为urdf

ros2 launch xarm_description xarm7_rviz_display.launch.py 

ros2 param get /robot_state_publisher robot_description > ~/xarm7.urdf

#重新缩进 运行需要删除urdf第一行的一句话 否则报错

sudo apt install libxml2-utils   # 如果没安装

xmllint --format xarm7.urdf -o xarm7_formatted.urdf
