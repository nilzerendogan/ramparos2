FROM tiryoh/ros-desktop-vnc:noetic

# İmaj zaten root olarak çalıştığı için USER komutlarına ihtiyacımız yok

RUN apt-get update && apt-get install -y \
    ros-noetic-gazebo-ros-pkgs \
    ros-noetic-gazebo-ros \
    ros-noetic-moveit \
    ros-noetic-universal-robots \
    ros-noetic-ur-gazebo \
    ros-noetic-rosbridge-suite \
    ros-noetic-joint-state-controller \
    ros-noetic-joint-trajectory-controller \
    ros-noetic-ros-control \
    ros-noetic-ros-controllers \
    python3-pip \
    ros-noetic-moveit-visual-tools \
    ros-noetic-rviz-visual-tools \
    ros-noetic-joy \
    ros-noetic-moveit-servo \
    build-essential \
    python3-catkin-tools \
    && rm -rf /var/lib/apt/lists/*

RUN pip3 install \
    movement-primitives \
    OneEuroFilter \
    pytransform3d \
    "numpy<1.24" \
    gmr

# Root olarak çalıştırdığımız için ROS'un uyarı verip build'i durdurmasını 
# engellemek adına sonuna '|| true' ekliyoruz
RUN rosdep update || true