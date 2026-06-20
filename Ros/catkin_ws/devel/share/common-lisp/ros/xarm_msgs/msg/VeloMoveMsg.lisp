; Auto-generated. Do not edit!


(cl:in-package xarm_msgs-msg)


;//! \htmlinclude VeloMoveMsg.msg.html

(cl:defclass <VeloMoveMsg> (roslisp-msg-protocol:ros-message)
  ((speeds
    :reader speeds
    :initarg :speeds
    :type (cl:vector cl:float)
   :initform (cl:make-array 0 :element-type 'cl:float :initial-element 0.0))
   (is_sync
    :reader is_sync
    :initarg :is_sync
    :type cl:boolean
    :initform cl:nil)
   (is_tool_coord
    :reader is_tool_coord
    :initarg :is_tool_coord
    :type cl:boolean
    :initform cl:nil)
   (duration
    :reader duration
    :initarg :duration
    :type cl:float
    :initform 0.0))
)

(cl:defclass VeloMoveMsg (<VeloMoveMsg>)
  ())

(cl:defmethod cl:initialize-instance :after ((m <VeloMoveMsg>) cl:&rest args)
  (cl:declare (cl:ignorable args))
  (cl:unless (cl:typep m 'VeloMoveMsg)
    (roslisp-msg-protocol:msg-deprecation-warning "using old message class name xarm_msgs-msg:<VeloMoveMsg> is deprecated: use xarm_msgs-msg:VeloMoveMsg instead.")))

(cl:ensure-generic-function 'speeds-val :lambda-list '(m))
(cl:defmethod speeds-val ((m <VeloMoveMsg>))
  (roslisp-msg-protocol:msg-deprecation-warning "Using old-style slot reader xarm_msgs-msg:speeds-val is deprecated.  Use xarm_msgs-msg:speeds instead.")
  (speeds m))

(cl:ensure-generic-function 'is_sync-val :lambda-list '(m))
(cl:defmethod is_sync-val ((m <VeloMoveMsg>))
  (roslisp-msg-protocol:msg-deprecation-warning "Using old-style slot reader xarm_msgs-msg:is_sync-val is deprecated.  Use xarm_msgs-msg:is_sync instead.")
  (is_sync m))

(cl:ensure-generic-function 'is_tool_coord-val :lambda-list '(m))
(cl:defmethod is_tool_coord-val ((m <VeloMoveMsg>))
  (roslisp-msg-protocol:msg-deprecation-warning "Using old-style slot reader xarm_msgs-msg:is_tool_coord-val is deprecated.  Use xarm_msgs-msg:is_tool_coord instead.")
  (is_tool_coord m))

(cl:ensure-generic-function 'duration-val :lambda-list '(m))
(cl:defmethod duration-val ((m <VeloMoveMsg>))
  (roslisp-msg-protocol:msg-deprecation-warning "Using old-style slot reader xarm_msgs-msg:duration-val is deprecated.  Use xarm_msgs-msg:duration instead.")
  (duration m))
(cl:defmethod roslisp-msg-protocol:serialize ((msg <VeloMoveMsg>) ostream)
  "Serializes a message object of type '<VeloMoveMsg>"
  (cl:let ((__ros_arr_len (cl:length (cl:slot-value msg 'speeds))))
    (cl:write-byte (cl:ldb (cl:byte 8 0) __ros_arr_len) ostream)
    (cl:write-byte (cl:ldb (cl:byte 8 8) __ros_arr_len) ostream)
    (cl:write-byte (cl:ldb (cl:byte 8 16) __ros_arr_len) ostream)
    (cl:write-byte (cl:ldb (cl:byte 8 24) __ros_arr_len) ostream))
  (cl:map cl:nil #'(cl:lambda (ele) (cl:let ((bits (roslisp-utils:encode-single-float-bits ele)))
    (cl:write-byte (cl:ldb (cl:byte 8 0) bits) ostream)
    (cl:write-byte (cl:ldb (cl:byte 8 8) bits) ostream)
    (cl:write-byte (cl:ldb (cl:byte 8 16) bits) ostream)
    (cl:write-byte (cl:ldb (cl:byte 8 24) bits) ostream)))
   (cl:slot-value msg 'speeds))
  (cl:write-byte (cl:ldb (cl:byte 8 0) (cl:if (cl:slot-value msg 'is_sync) 1 0)) ostream)
  (cl:write-byte (cl:ldb (cl:byte 8 0) (cl:if (cl:slot-value msg 'is_tool_coord) 1 0)) ostream)
  (cl:let ((bits (roslisp-utils:encode-single-float-bits (cl:slot-value msg 'duration))))
    (cl:write-byte (cl:ldb (cl:byte 8 0) bits) ostream)
    (cl:write-byte (cl:ldb (cl:byte 8 8) bits) ostream)
    (cl:write-byte (cl:ldb (cl:byte 8 16) bits) ostream)
    (cl:write-byte (cl:ldb (cl:byte 8 24) bits) ostream))
)
(cl:defmethod roslisp-msg-protocol:deserialize ((msg <VeloMoveMsg>) istream)
  "Deserializes a message object of type '<VeloMoveMsg>"
  (cl:let ((__ros_arr_len 0))
    (cl:setf (cl:ldb (cl:byte 8 0) __ros_arr_len) (cl:read-byte istream))
    (cl:setf (cl:ldb (cl:byte 8 8) __ros_arr_len) (cl:read-byte istream))
    (cl:setf (cl:ldb (cl:byte 8 16) __ros_arr_len) (cl:read-byte istream))
    (cl:setf (cl:ldb (cl:byte 8 24) __ros_arr_len) (cl:read-byte istream))
  (cl:setf (cl:slot-value msg 'speeds) (cl:make-array __ros_arr_len))
  (cl:let ((vals (cl:slot-value msg 'speeds)))
    (cl:dotimes (i __ros_arr_len)
    (cl:let ((bits 0))
      (cl:setf (cl:ldb (cl:byte 8 0) bits) (cl:read-byte istream))
      (cl:setf (cl:ldb (cl:byte 8 8) bits) (cl:read-byte istream))
      (cl:setf (cl:ldb (cl:byte 8 16) bits) (cl:read-byte istream))
      (cl:setf (cl:ldb (cl:byte 8 24) bits) (cl:read-byte istream))
    (cl:setf (cl:aref vals i) (roslisp-utils:decode-single-float-bits bits))))))
    (cl:setf (cl:slot-value msg 'is_sync) (cl:not (cl:zerop (cl:read-byte istream))))
    (cl:setf (cl:slot-value msg 'is_tool_coord) (cl:not (cl:zerop (cl:read-byte istream))))
    (cl:let ((bits 0))
      (cl:setf (cl:ldb (cl:byte 8 0) bits) (cl:read-byte istream))
      (cl:setf (cl:ldb (cl:byte 8 8) bits) (cl:read-byte istream))
      (cl:setf (cl:ldb (cl:byte 8 16) bits) (cl:read-byte istream))
      (cl:setf (cl:ldb (cl:byte 8 24) bits) (cl:read-byte istream))
    (cl:setf (cl:slot-value msg 'duration) (roslisp-utils:decode-single-float-bits bits)))
  msg
)
(cl:defmethod roslisp-msg-protocol:ros-datatype ((msg (cl:eql '<VeloMoveMsg>)))
  "Returns string type for a message object of type '<VeloMoveMsg>"
  "xarm_msgs/VeloMoveMsg")
(cl:defmethod roslisp-msg-protocol:ros-datatype ((msg (cl:eql 'VeloMoveMsg)))
  "Returns string type for a message object of type 'VeloMoveMsg"
  "xarm_msgs/VeloMoveMsg")
(cl:defmethod roslisp-msg-protocol:md5sum ((type (cl:eql '<VeloMoveMsg>)))
  "Returns md5sum for a message object of type '<VeloMoveMsg>"
  "3ff5ed26eb25726c663dac7efa05cc61")
(cl:defmethod roslisp-msg-protocol:md5sum ((type (cl:eql 'VeloMoveMsg)))
  "Returns md5sum for a message object of type 'VeloMoveMsg"
  "3ff5ed26eb25726c663dac7efa05cc61")
(cl:defmethod roslisp-msg-protocol:message-definition ((type (cl:eql '<VeloMoveMsg>)))
  "Returns full string definition for message of type '<VeloMoveMsg>"
  (cl:format cl:nil "# request: command specification for velocity executions.~%# Units:~%#	joint space/angles: radian/s~%#	Cartesian space: mm/s, radian/s.~%~%# speeds: the velocity list of the joints/tcp~%#   For velo_move_joint_timed topic: [joint1_velocity, ..., joint7_velocity]~%#   For velo_move_line_timed topic: [x_velocity, y_velocity, z_velocity, rx_velocity, ry_velocity, rz_velocity (axis-angle)]~%float32[] speeds~%~%# is_sync: this is special for velo_move_joint_timed topic, meaning whether all joints accelerate and decelerate synchronously, true for yes, false for no.~%# avaiable for topic velo_move_joint_timed~%bool is_sync~%~%# is_tool_coord: this is special for velo_move_line_timed topic, meaning whether motion is in tool coordinate(true) or not(false)~%# avaiable for topic velo_move_line_timed~%bool is_tool_coord~%~%# the maximum duration of the speed, over this time will automatically set the speed to 0~%#   duration > 0: seconds, indicates the maximum number of seconds that this speed can be maintained~%#   duration == 0: always effective, will not stop automativally~%#   duration < 0: only used to be compatible with the old protocol, equivalent to 0~%# avaiable for firmware_version >= 1.8.0~%float32 duration~%~%"))
(cl:defmethod roslisp-msg-protocol:message-definition ((type (cl:eql 'VeloMoveMsg)))
  "Returns full string definition for message of type 'VeloMoveMsg"
  (cl:format cl:nil "# request: command specification for velocity executions.~%# Units:~%#	joint space/angles: radian/s~%#	Cartesian space: mm/s, radian/s.~%~%# speeds: the velocity list of the joints/tcp~%#   For velo_move_joint_timed topic: [joint1_velocity, ..., joint7_velocity]~%#   For velo_move_line_timed topic: [x_velocity, y_velocity, z_velocity, rx_velocity, ry_velocity, rz_velocity (axis-angle)]~%float32[] speeds~%~%# is_sync: this is special for velo_move_joint_timed topic, meaning whether all joints accelerate and decelerate synchronously, true for yes, false for no.~%# avaiable for topic velo_move_joint_timed~%bool is_sync~%~%# is_tool_coord: this is special for velo_move_line_timed topic, meaning whether motion is in tool coordinate(true) or not(false)~%# avaiable for topic velo_move_line_timed~%bool is_tool_coord~%~%# the maximum duration of the speed, over this time will automatically set the speed to 0~%#   duration > 0: seconds, indicates the maximum number of seconds that this speed can be maintained~%#   duration == 0: always effective, will not stop automativally~%#   duration < 0: only used to be compatible with the old protocol, equivalent to 0~%# avaiable for firmware_version >= 1.8.0~%float32 duration~%~%"))
(cl:defmethod roslisp-msg-protocol:serialization-length ((msg <VeloMoveMsg>))
  (cl:+ 0
     4 (cl:reduce #'cl:+ (cl:slot-value msg 'speeds) :key #'(cl:lambda (ele) (cl:declare (cl:ignorable ele)) (cl:+ 4)))
     1
     1
     4
))
(cl:defmethod roslisp-msg-protocol:ros-message-to-list ((msg <VeloMoveMsg>))
  "Converts a ROS message object to a list"
  (cl:list 'VeloMoveMsg
    (cl:cons ':speeds (speeds msg))
    (cl:cons ':is_sync (is_sync msg))
    (cl:cons ':is_tool_coord (is_tool_coord msg))
    (cl:cons ':duration (duration msg))
))
