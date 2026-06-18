; Auto-generated. Do not edit!


(cl:in-package xarm_msgs-srv)


;//! \htmlinclude VacuumGripperCtrl-request.msg.html

(cl:defclass <VacuumGripperCtrl-request> (roslisp-msg-protocol:ros-message)
  ((on
    :reader on
    :initarg :on
    :type cl:boolean
    :initform cl:nil)
   (wait
    :reader wait
    :initarg :wait
    :type cl:boolean
    :initform cl:nil)
   (timeout
    :reader timeout
    :initarg :timeout
    :type cl:float
    :initform 0.0)
   (delay_sec
    :reader delay_sec
    :initarg :delay_sec
    :type cl:float
    :initform 0.0)
   (sync
    :reader sync
    :initarg :sync
    :type cl:boolean
    :initform cl:nil)
   (hardware_version
    :reader hardware_version
    :initarg :hardware_version
    :type cl:fixnum
    :initform 0))
)

(cl:defclass VacuumGripperCtrl-request (<VacuumGripperCtrl-request>)
  ())

(cl:defmethod cl:initialize-instance :after ((m <VacuumGripperCtrl-request>) cl:&rest args)
  (cl:declare (cl:ignorable args))
  (cl:unless (cl:typep m 'VacuumGripperCtrl-request)
    (roslisp-msg-protocol:msg-deprecation-warning "using old message class name xarm_msgs-srv:<VacuumGripperCtrl-request> is deprecated: use xarm_msgs-srv:VacuumGripperCtrl-request instead.")))

(cl:ensure-generic-function 'on-val :lambda-list '(m))
(cl:defmethod on-val ((m <VacuumGripperCtrl-request>))
  (roslisp-msg-protocol:msg-deprecation-warning "Using old-style slot reader xarm_msgs-srv:on-val is deprecated.  Use xarm_msgs-srv:on instead.")
  (on m))

(cl:ensure-generic-function 'wait-val :lambda-list '(m))
(cl:defmethod wait-val ((m <VacuumGripperCtrl-request>))
  (roslisp-msg-protocol:msg-deprecation-warning "Using old-style slot reader xarm_msgs-srv:wait-val is deprecated.  Use xarm_msgs-srv:wait instead.")
  (wait m))

(cl:ensure-generic-function 'timeout-val :lambda-list '(m))
(cl:defmethod timeout-val ((m <VacuumGripperCtrl-request>))
  (roslisp-msg-protocol:msg-deprecation-warning "Using old-style slot reader xarm_msgs-srv:timeout-val is deprecated.  Use xarm_msgs-srv:timeout instead.")
  (timeout m))

(cl:ensure-generic-function 'delay_sec-val :lambda-list '(m))
(cl:defmethod delay_sec-val ((m <VacuumGripperCtrl-request>))
  (roslisp-msg-protocol:msg-deprecation-warning "Using old-style slot reader xarm_msgs-srv:delay_sec-val is deprecated.  Use xarm_msgs-srv:delay_sec instead.")
  (delay_sec m))

(cl:ensure-generic-function 'sync-val :lambda-list '(m))
(cl:defmethod sync-val ((m <VacuumGripperCtrl-request>))
  (roslisp-msg-protocol:msg-deprecation-warning "Using old-style slot reader xarm_msgs-srv:sync-val is deprecated.  Use xarm_msgs-srv:sync instead.")
  (sync m))

(cl:ensure-generic-function 'hardware_version-val :lambda-list '(m))
(cl:defmethod hardware_version-val ((m <VacuumGripperCtrl-request>))
  (roslisp-msg-protocol:msg-deprecation-warning "Using old-style slot reader xarm_msgs-srv:hardware_version-val is deprecated.  Use xarm_msgs-srv:hardware_version instead.")
  (hardware_version m))
(cl:defmethod roslisp-msg-protocol:serialize ((msg <VacuumGripperCtrl-request>) ostream)
  "Serializes a message object of type '<VacuumGripperCtrl-request>"
  (cl:write-byte (cl:ldb (cl:byte 8 0) (cl:if (cl:slot-value msg 'on) 1 0)) ostream)
  (cl:write-byte (cl:ldb (cl:byte 8 0) (cl:if (cl:slot-value msg 'wait) 1 0)) ostream)
  (cl:let ((bits (roslisp-utils:encode-single-float-bits (cl:slot-value msg 'timeout))))
    (cl:write-byte (cl:ldb (cl:byte 8 0) bits) ostream)
    (cl:write-byte (cl:ldb (cl:byte 8 8) bits) ostream)
    (cl:write-byte (cl:ldb (cl:byte 8 16) bits) ostream)
    (cl:write-byte (cl:ldb (cl:byte 8 24) bits) ostream))
  (cl:let ((bits (roslisp-utils:encode-single-float-bits (cl:slot-value msg 'delay_sec))))
    (cl:write-byte (cl:ldb (cl:byte 8 0) bits) ostream)
    (cl:write-byte (cl:ldb (cl:byte 8 8) bits) ostream)
    (cl:write-byte (cl:ldb (cl:byte 8 16) bits) ostream)
    (cl:write-byte (cl:ldb (cl:byte 8 24) bits) ostream))
  (cl:write-byte (cl:ldb (cl:byte 8 0) (cl:if (cl:slot-value msg 'sync) 1 0)) ostream)
  (cl:let* ((signed (cl:slot-value msg 'hardware_version)) (unsigned (cl:if (cl:< signed 0) (cl:+ signed 65536) signed)))
    (cl:write-byte (cl:ldb (cl:byte 8 0) unsigned) ostream)
    (cl:write-byte (cl:ldb (cl:byte 8 8) unsigned) ostream)
    )
)
(cl:defmethod roslisp-msg-protocol:deserialize ((msg <VacuumGripperCtrl-request>) istream)
  "Deserializes a message object of type '<VacuumGripperCtrl-request>"
    (cl:setf (cl:slot-value msg 'on) (cl:not (cl:zerop (cl:read-byte istream))))
    (cl:setf (cl:slot-value msg 'wait) (cl:not (cl:zerop (cl:read-byte istream))))
    (cl:let ((bits 0))
      (cl:setf (cl:ldb (cl:byte 8 0) bits) (cl:read-byte istream))
      (cl:setf (cl:ldb (cl:byte 8 8) bits) (cl:read-byte istream))
      (cl:setf (cl:ldb (cl:byte 8 16) bits) (cl:read-byte istream))
      (cl:setf (cl:ldb (cl:byte 8 24) bits) (cl:read-byte istream))
    (cl:setf (cl:slot-value msg 'timeout) (roslisp-utils:decode-single-float-bits bits)))
    (cl:let ((bits 0))
      (cl:setf (cl:ldb (cl:byte 8 0) bits) (cl:read-byte istream))
      (cl:setf (cl:ldb (cl:byte 8 8) bits) (cl:read-byte istream))
      (cl:setf (cl:ldb (cl:byte 8 16) bits) (cl:read-byte istream))
      (cl:setf (cl:ldb (cl:byte 8 24) bits) (cl:read-byte istream))
    (cl:setf (cl:slot-value msg 'delay_sec) (roslisp-utils:decode-single-float-bits bits)))
    (cl:setf (cl:slot-value msg 'sync) (cl:not (cl:zerop (cl:read-byte istream))))
    (cl:let ((unsigned 0))
      (cl:setf (cl:ldb (cl:byte 8 0) unsigned) (cl:read-byte istream))
      (cl:setf (cl:ldb (cl:byte 8 8) unsigned) (cl:read-byte istream))
      (cl:setf (cl:slot-value msg 'hardware_version) (cl:if (cl:< unsigned 32768) unsigned (cl:- unsigned 65536))))
  msg
)
(cl:defmethod roslisp-msg-protocol:ros-datatype ((msg (cl:eql '<VacuumGripperCtrl-request>)))
  "Returns string type for a service object of type '<VacuumGripperCtrl-request>"
  "xarm_msgs/VacuumGripperCtrlRequest")
(cl:defmethod roslisp-msg-protocol:ros-datatype ((msg (cl:eql 'VacuumGripperCtrl-request)))
  "Returns string type for a service object of type 'VacuumGripperCtrl-request"
  "xarm_msgs/VacuumGripperCtrlRequest")
(cl:defmethod roslisp-msg-protocol:md5sum ((type (cl:eql '<VacuumGripperCtrl-request>)))
  "Returns md5sum for a message object of type '<VacuumGripperCtrl-request>"
  "b01133253841b00d2aa64b79e0937eb6")
(cl:defmethod roslisp-msg-protocol:md5sum ((type (cl:eql 'VacuumGripperCtrl-request)))
  "Returns md5sum for a message object of type 'VacuumGripperCtrl-request"
  "b01133253841b00d2aa64b79e0937eb6")
(cl:defmethod roslisp-msg-protocol:message-definition ((type (cl:eql '<VacuumGripperCtrl-request>)))
  "Returns full string definition for message of type '<VacuumGripperCtrl-request>"
  (cl:format cl:nil "bool on~%bool wait~%float32 timeout~%float32 delay_sec~%# sync: whether to execute in the motion queue~%bool sync~%# hardware_version==1: Plug-in Connection, default~%# hardware_version==2: Contact Connection~%int16 hardware_version~%~%~%~%"))
(cl:defmethod roslisp-msg-protocol:message-definition ((type (cl:eql 'VacuumGripperCtrl-request)))
  "Returns full string definition for message of type 'VacuumGripperCtrl-request"
  (cl:format cl:nil "bool on~%bool wait~%float32 timeout~%float32 delay_sec~%# sync: whether to execute in the motion queue~%bool sync~%# hardware_version==1: Plug-in Connection, default~%# hardware_version==2: Contact Connection~%int16 hardware_version~%~%~%~%"))
(cl:defmethod roslisp-msg-protocol:serialization-length ((msg <VacuumGripperCtrl-request>))
  (cl:+ 0
     1
     1
     4
     4
     1
     2
))
(cl:defmethod roslisp-msg-protocol:ros-message-to-list ((msg <VacuumGripperCtrl-request>))
  "Converts a ROS message object to a list"
  (cl:list 'VacuumGripperCtrl-request
    (cl:cons ':on (on msg))
    (cl:cons ':wait (wait msg))
    (cl:cons ':timeout (timeout msg))
    (cl:cons ':delay_sec (delay_sec msg))
    (cl:cons ':sync (sync msg))
    (cl:cons ':hardware_version (hardware_version msg))
))
;//! \htmlinclude VacuumGripperCtrl-response.msg.html

(cl:defclass <VacuumGripperCtrl-response> (roslisp-msg-protocol:ros-message)
  ((ret
    :reader ret
    :initarg :ret
    :type cl:fixnum
    :initform 0)
   (message
    :reader message
    :initarg :message
    :type cl:string
    :initform ""))
)

(cl:defclass VacuumGripperCtrl-response (<VacuumGripperCtrl-response>)
  ())

(cl:defmethod cl:initialize-instance :after ((m <VacuumGripperCtrl-response>) cl:&rest args)
  (cl:declare (cl:ignorable args))
  (cl:unless (cl:typep m 'VacuumGripperCtrl-response)
    (roslisp-msg-protocol:msg-deprecation-warning "using old message class name xarm_msgs-srv:<VacuumGripperCtrl-response> is deprecated: use xarm_msgs-srv:VacuumGripperCtrl-response instead.")))

(cl:ensure-generic-function 'ret-val :lambda-list '(m))
(cl:defmethod ret-val ((m <VacuumGripperCtrl-response>))
  (roslisp-msg-protocol:msg-deprecation-warning "Using old-style slot reader xarm_msgs-srv:ret-val is deprecated.  Use xarm_msgs-srv:ret instead.")
  (ret m))

(cl:ensure-generic-function 'message-val :lambda-list '(m))
(cl:defmethod message-val ((m <VacuumGripperCtrl-response>))
  (roslisp-msg-protocol:msg-deprecation-warning "Using old-style slot reader xarm_msgs-srv:message-val is deprecated.  Use xarm_msgs-srv:message instead.")
  (message m))
(cl:defmethod roslisp-msg-protocol:serialize ((msg <VacuumGripperCtrl-response>) ostream)
  "Serializes a message object of type '<VacuumGripperCtrl-response>"
  (cl:let* ((signed (cl:slot-value msg 'ret)) (unsigned (cl:if (cl:< signed 0) (cl:+ signed 65536) signed)))
    (cl:write-byte (cl:ldb (cl:byte 8 0) unsigned) ostream)
    (cl:write-byte (cl:ldb (cl:byte 8 8) unsigned) ostream)
    )
  (cl:let ((__ros_str_len (cl:length (cl:slot-value msg 'message))))
    (cl:write-byte (cl:ldb (cl:byte 8 0) __ros_str_len) ostream)
    (cl:write-byte (cl:ldb (cl:byte 8 8) __ros_str_len) ostream)
    (cl:write-byte (cl:ldb (cl:byte 8 16) __ros_str_len) ostream)
    (cl:write-byte (cl:ldb (cl:byte 8 24) __ros_str_len) ostream))
  (cl:map cl:nil #'(cl:lambda (c) (cl:write-byte (cl:char-code c) ostream)) (cl:slot-value msg 'message))
)
(cl:defmethod roslisp-msg-protocol:deserialize ((msg <VacuumGripperCtrl-response>) istream)
  "Deserializes a message object of type '<VacuumGripperCtrl-response>"
    (cl:let ((unsigned 0))
      (cl:setf (cl:ldb (cl:byte 8 0) unsigned) (cl:read-byte istream))
      (cl:setf (cl:ldb (cl:byte 8 8) unsigned) (cl:read-byte istream))
      (cl:setf (cl:slot-value msg 'ret) (cl:if (cl:< unsigned 32768) unsigned (cl:- unsigned 65536))))
    (cl:let ((__ros_str_len 0))
      (cl:setf (cl:ldb (cl:byte 8 0) __ros_str_len) (cl:read-byte istream))
      (cl:setf (cl:ldb (cl:byte 8 8) __ros_str_len) (cl:read-byte istream))
      (cl:setf (cl:ldb (cl:byte 8 16) __ros_str_len) (cl:read-byte istream))
      (cl:setf (cl:ldb (cl:byte 8 24) __ros_str_len) (cl:read-byte istream))
      (cl:setf (cl:slot-value msg 'message) (cl:make-string __ros_str_len))
      (cl:dotimes (__ros_str_idx __ros_str_len msg)
        (cl:setf (cl:char (cl:slot-value msg 'message) __ros_str_idx) (cl:code-char (cl:read-byte istream)))))
  msg
)
(cl:defmethod roslisp-msg-protocol:ros-datatype ((msg (cl:eql '<VacuumGripperCtrl-response>)))
  "Returns string type for a service object of type '<VacuumGripperCtrl-response>"
  "xarm_msgs/VacuumGripperCtrlResponse")
(cl:defmethod roslisp-msg-protocol:ros-datatype ((msg (cl:eql 'VacuumGripperCtrl-response)))
  "Returns string type for a service object of type 'VacuumGripperCtrl-response"
  "xarm_msgs/VacuumGripperCtrlResponse")
(cl:defmethod roslisp-msg-protocol:md5sum ((type (cl:eql '<VacuumGripperCtrl-response>)))
  "Returns md5sum for a message object of type '<VacuumGripperCtrl-response>"
  "b01133253841b00d2aa64b79e0937eb6")
(cl:defmethod roslisp-msg-protocol:md5sum ((type (cl:eql 'VacuumGripperCtrl-response)))
  "Returns md5sum for a message object of type 'VacuumGripperCtrl-response"
  "b01133253841b00d2aa64b79e0937eb6")
(cl:defmethod roslisp-msg-protocol:message-definition ((type (cl:eql '<VacuumGripperCtrl-response>)))
  "Returns full string definition for message of type '<VacuumGripperCtrl-response>"
  (cl:format cl:nil "~%int16 ret~%string message~%~%~%~%"))
(cl:defmethod roslisp-msg-protocol:message-definition ((type (cl:eql 'VacuumGripperCtrl-response)))
  "Returns full string definition for message of type 'VacuumGripperCtrl-response"
  (cl:format cl:nil "~%int16 ret~%string message~%~%~%~%"))
(cl:defmethod roslisp-msg-protocol:serialization-length ((msg <VacuumGripperCtrl-response>))
  (cl:+ 0
     2
     4 (cl:length (cl:slot-value msg 'message))
))
(cl:defmethod roslisp-msg-protocol:ros-message-to-list ((msg <VacuumGripperCtrl-response>))
  "Converts a ROS message object to a list"
  (cl:list 'VacuumGripperCtrl-response
    (cl:cons ':ret (ret msg))
    (cl:cons ':message (message msg))
))
(cl:defmethod roslisp-msg-protocol:service-request-type ((msg (cl:eql 'VacuumGripperCtrl)))
  'VacuumGripperCtrl-request)
(cl:defmethod roslisp-msg-protocol:service-response-type ((msg (cl:eql 'VacuumGripperCtrl)))
  'VacuumGripperCtrl-response)
(cl:defmethod roslisp-msg-protocol:ros-datatype ((msg (cl:eql 'VacuumGripperCtrl)))
  "Returns string type for a service object of type '<VacuumGripperCtrl>"
  "xarm_msgs/VacuumGripperCtrl")