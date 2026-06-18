;; Auto-generated. Do not edit!


(when (boundp 'xarm_msgs::VeloMoveMsg)
  (if (not (find-package "XARM_MSGS"))
    (make-package "XARM_MSGS"))
  (shadow 'VeloMoveMsg (find-package "XARM_MSGS")))
(unless (find-package "XARM_MSGS::VELOMOVEMSG")
  (make-package "XARM_MSGS::VELOMOVEMSG"))

(in-package "ROS")
;;//! \htmlinclude VeloMoveMsg.msg.html


(defclass xarm_msgs::VeloMoveMsg
  :super ros::object
  :slots (_speeds _is_sync _is_tool_coord _duration ))

(defmethod xarm_msgs::VeloMoveMsg
  (:init
   (&key
    ((:speeds __speeds) (make-array 0 :initial-element 0.0 :element-type :float))
    ((:is_sync __is_sync) nil)
    ((:is_tool_coord __is_tool_coord) nil)
    ((:duration __duration) 0.0)
    )
   (send-super :init)
   (setq _speeds __speeds)
   (setq _is_sync __is_sync)
   (setq _is_tool_coord __is_tool_coord)
   (setq _duration (float __duration))
   self)
  (:speeds
   (&optional __speeds)
   (if __speeds (setq _speeds __speeds)) _speeds)
  (:is_sync
   (&optional (__is_sync :null))
   (if (not (eq __is_sync :null)) (setq _is_sync __is_sync)) _is_sync)
  (:is_tool_coord
   (&optional (__is_tool_coord :null))
   (if (not (eq __is_tool_coord :null)) (setq _is_tool_coord __is_tool_coord)) _is_tool_coord)
  (:duration
   (&optional __duration)
   (if __duration (setq _duration __duration)) _duration)
  (:serialization-length
   ()
   (+
    ;; float32[] _speeds
    (* 4    (length _speeds)) 4
    ;; bool _is_sync
    1
    ;; bool _is_tool_coord
    1
    ;; float32 _duration
    4
    ))
  (:serialize
   (&optional strm)
   (let ((s (if strm strm
              (make-string-output-stream (send self :serialization-length)))))
     ;; float32[] _speeds
     (write-long (length _speeds) s)
     (dotimes (i (length _speeds))
       (sys::poke (elt _speeds i) (send s :buffer) (send s :count) :float) (incf (stream-count s) 4)
       )
     ;; bool _is_sync
       (if _is_sync (write-byte -1 s) (write-byte 0 s))
     ;; bool _is_tool_coord
       (if _is_tool_coord (write-byte -1 s) (write-byte 0 s))
     ;; float32 _duration
       (sys::poke _duration (send s :buffer) (send s :count) :float) (incf (stream-count s) 4)
     ;;
     (if (null strm) (get-output-stream-string s))))
  (:deserialize
   (buf &optional (ptr- 0))
   ;; float32[] _speeds
   (let (n)
     (setq n (sys::peek buf ptr- :integer)) (incf ptr- 4)
     (setq _speeds (instantiate float-vector n))
     (dotimes (i n)
     (setf (elt _speeds i) (sys::peek buf ptr- :float)) (incf ptr- 4)
     ))
   ;; bool _is_sync
     (setq _is_sync (not (= 0 (sys::peek buf ptr- :char)))) (incf ptr- 1)
   ;; bool _is_tool_coord
     (setq _is_tool_coord (not (= 0 (sys::peek buf ptr- :char)))) (incf ptr- 1)
   ;; float32 _duration
     (setq _duration (sys::peek buf ptr- :float)) (incf ptr- 4)
   ;;
   self)
  )

(setf (get xarm_msgs::VeloMoveMsg :md5sum-) "3ff5ed26eb25726c663dac7efa05cc61")
(setf (get xarm_msgs::VeloMoveMsg :datatype-) "xarm_msgs/VeloMoveMsg")
(setf (get xarm_msgs::VeloMoveMsg :definition-)
      "# request: command specification for velocity executions.
# Units:
#	joint space/angles: radian/s
#	Cartesian space: mm/s, radian/s.

# speeds: the velocity list of the joints/tcp
#   For velo_move_joint_timed topic: [joint1_velocity, ..., joint7_velocity]
#   For velo_move_line_timed topic: [x_velocity, y_velocity, z_velocity, rx_velocity, ry_velocity, rz_velocity (axis-angle)]
float32[] speeds

# is_sync: this is special for velo_move_joint_timed topic, meaning whether all joints accelerate and decelerate synchronously, true for yes, false for no.
# avaiable for topic velo_move_joint_timed
bool is_sync

# is_tool_coord: this is special for velo_move_line_timed topic, meaning whether motion is in tool coordinate(true) or not(false)
# avaiable for topic velo_move_line_timed
bool is_tool_coord

# the maximum duration of the speed, over this time will automatically set the speed to 0
#   duration > 0: seconds, indicates the maximum number of seconds that this speed can be maintained
#   duration == 0: always effective, will not stop automativally
#   duration < 0: only used to be compatible with the old protocol, equivalent to 0
# avaiable for firmware_version >= 1.8.0
float32 duration
")



(provide :xarm_msgs/VeloMoveMsg "3ff5ed26eb25726c663dac7efa05cc61")


