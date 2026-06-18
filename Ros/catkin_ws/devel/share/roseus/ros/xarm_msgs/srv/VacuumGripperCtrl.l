;; Auto-generated. Do not edit!


(when (boundp 'xarm_msgs::VacuumGripperCtrl)
  (if (not (find-package "XARM_MSGS"))
    (make-package "XARM_MSGS"))
  (shadow 'VacuumGripperCtrl (find-package "XARM_MSGS")))
(unless (find-package "XARM_MSGS::VACUUMGRIPPERCTRL")
  (make-package "XARM_MSGS::VACUUMGRIPPERCTRL"))
(unless (find-package "XARM_MSGS::VACUUMGRIPPERCTRLREQUEST")
  (make-package "XARM_MSGS::VACUUMGRIPPERCTRLREQUEST"))
(unless (find-package "XARM_MSGS::VACUUMGRIPPERCTRLRESPONSE")
  (make-package "XARM_MSGS::VACUUMGRIPPERCTRLRESPONSE"))

(in-package "ROS")





(defclass xarm_msgs::VacuumGripperCtrlRequest
  :super ros::object
  :slots (_on _wait _timeout _delay_sec _sync _hardware_version ))

(defmethod xarm_msgs::VacuumGripperCtrlRequest
  (:init
   (&key
    ((:on __on) nil)
    ((:wait __wait) nil)
    ((:timeout __timeout) 0.0)
    ((:delay_sec __delay_sec) 0.0)
    ((:sync __sync) nil)
    ((:hardware_version __hardware_version) 0)
    )
   (send-super :init)
   (setq _on __on)
   (setq _wait __wait)
   (setq _timeout (float __timeout))
   (setq _delay_sec (float __delay_sec))
   (setq _sync __sync)
   (setq _hardware_version (round __hardware_version))
   self)
  (:on
   (&optional (__on :null))
   (if (not (eq __on :null)) (setq _on __on)) _on)
  (:wait
   (&optional (__wait :null))
   (if (not (eq __wait :null)) (setq _wait __wait)) _wait)
  (:timeout
   (&optional __timeout)
   (if __timeout (setq _timeout __timeout)) _timeout)
  (:delay_sec
   (&optional __delay_sec)
   (if __delay_sec (setq _delay_sec __delay_sec)) _delay_sec)
  (:sync
   (&optional (__sync :null))
   (if (not (eq __sync :null)) (setq _sync __sync)) _sync)
  (:hardware_version
   (&optional __hardware_version)
   (if __hardware_version (setq _hardware_version __hardware_version)) _hardware_version)
  (:serialization-length
   ()
   (+
    ;; bool _on
    1
    ;; bool _wait
    1
    ;; float32 _timeout
    4
    ;; float32 _delay_sec
    4
    ;; bool _sync
    1
    ;; int16 _hardware_version
    2
    ))
  (:serialize
   (&optional strm)
   (let ((s (if strm strm
              (make-string-output-stream (send self :serialization-length)))))
     ;; bool _on
       (if _on (write-byte -1 s) (write-byte 0 s))
     ;; bool _wait
       (if _wait (write-byte -1 s) (write-byte 0 s))
     ;; float32 _timeout
       (sys::poke _timeout (send s :buffer) (send s :count) :float) (incf (stream-count s) 4)
     ;; float32 _delay_sec
       (sys::poke _delay_sec (send s :buffer) (send s :count) :float) (incf (stream-count s) 4)
     ;; bool _sync
       (if _sync (write-byte -1 s) (write-byte 0 s))
     ;; int16 _hardware_version
       (write-word _hardware_version s)
     ;;
     (if (null strm) (get-output-stream-string s))))
  (:deserialize
   (buf &optional (ptr- 0))
   ;; bool _on
     (setq _on (not (= 0 (sys::peek buf ptr- :char)))) (incf ptr- 1)
   ;; bool _wait
     (setq _wait (not (= 0 (sys::peek buf ptr- :char)))) (incf ptr- 1)
   ;; float32 _timeout
     (setq _timeout (sys::peek buf ptr- :float)) (incf ptr- 4)
   ;; float32 _delay_sec
     (setq _delay_sec (sys::peek buf ptr- :float)) (incf ptr- 4)
   ;; bool _sync
     (setq _sync (not (= 0 (sys::peek buf ptr- :char)))) (incf ptr- 1)
   ;; int16 _hardware_version
     (setq _hardware_version (sys::peek buf ptr- :short)) (incf ptr- 2)
   ;;
   self)
  )

(defclass xarm_msgs::VacuumGripperCtrlResponse
  :super ros::object
  :slots (_ret _message ))

(defmethod xarm_msgs::VacuumGripperCtrlResponse
  (:init
   (&key
    ((:ret __ret) 0)
    ((:message __message) "")
    )
   (send-super :init)
   (setq _ret (round __ret))
   (setq _message (string __message))
   self)
  (:ret
   (&optional __ret)
   (if __ret (setq _ret __ret)) _ret)
  (:message
   (&optional __message)
   (if __message (setq _message __message)) _message)
  (:serialization-length
   ()
   (+
    ;; int16 _ret
    2
    ;; string _message
    4 (length _message)
    ))
  (:serialize
   (&optional strm)
   (let ((s (if strm strm
              (make-string-output-stream (send self :serialization-length)))))
     ;; int16 _ret
       (write-word _ret s)
     ;; string _message
       (write-long (length _message) s) (princ _message s)
     ;;
     (if (null strm) (get-output-stream-string s))))
  (:deserialize
   (buf &optional (ptr- 0))
   ;; int16 _ret
     (setq _ret (sys::peek buf ptr- :short)) (incf ptr- 2)
   ;; string _message
     (let (n) (setq n (sys::peek buf ptr- :integer)) (incf ptr- 4) (setq _message (subseq buf ptr- (+ ptr- n))) (incf ptr- n))
   ;;
   self)
  )

(defclass xarm_msgs::VacuumGripperCtrl
  :super ros::object
  :slots ())

(setf (get xarm_msgs::VacuumGripperCtrl :md5sum-) "b01133253841b00d2aa64b79e0937eb6")
(setf (get xarm_msgs::VacuumGripperCtrl :datatype-) "xarm_msgs/VacuumGripperCtrl")
(setf (get xarm_msgs::VacuumGripperCtrl :request) xarm_msgs::VacuumGripperCtrlRequest)
(setf (get xarm_msgs::VacuumGripperCtrl :response) xarm_msgs::VacuumGripperCtrlResponse)

(defmethod xarm_msgs::VacuumGripperCtrlRequest
  (:response () (instance xarm_msgs::VacuumGripperCtrlResponse :init)))

(setf (get xarm_msgs::VacuumGripperCtrlRequest :md5sum-) "b01133253841b00d2aa64b79e0937eb6")
(setf (get xarm_msgs::VacuumGripperCtrlRequest :datatype-) "xarm_msgs/VacuumGripperCtrlRequest")
(setf (get xarm_msgs::VacuumGripperCtrlRequest :definition-)
      "bool on
bool wait
float32 timeout
float32 delay_sec
# sync: whether to execute in the motion queue
bool sync
# hardware_version==1: Plug-in Connection, default
# hardware_version==2: Contact Connection
int16 hardware_version

---

int16 ret
string message

")

(setf (get xarm_msgs::VacuumGripperCtrlResponse :md5sum-) "b01133253841b00d2aa64b79e0937eb6")
(setf (get xarm_msgs::VacuumGripperCtrlResponse :datatype-) "xarm_msgs/VacuumGripperCtrlResponse")
(setf (get xarm_msgs::VacuumGripperCtrlResponse :definition-)
      "bool on
bool wait
float32 timeout
float32 delay_sec
# sync: whether to execute in the motion queue
bool sync
# hardware_version==1: Plug-in Connection, default
# hardware_version==2: Contact Connection
int16 hardware_version

---

int16 ret
string message

")



(provide :xarm_msgs/VacuumGripperCtrl "b01133253841b00d2aa64b79e0937eb6")


