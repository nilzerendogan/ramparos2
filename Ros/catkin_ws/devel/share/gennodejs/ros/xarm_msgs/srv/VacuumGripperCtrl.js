// Auto-generated. Do not edit!

// (in-package xarm_msgs.srv)


"use strict";

const _serializer = _ros_msg_utils.Serialize;
const _arraySerializer = _serializer.Array;
const _deserializer = _ros_msg_utils.Deserialize;
const _arrayDeserializer = _deserializer.Array;
const _finder = _ros_msg_utils.Find;
const _getByteLength = _ros_msg_utils.getByteLength;

//-----------------------------------------------------------


//-----------------------------------------------------------

class VacuumGripperCtrlRequest {
  constructor(initObj={}) {
    if (initObj === null) {
      // initObj === null is a special case for deserialization where we don't initialize fields
      this.on = null;
      this.wait = null;
      this.timeout = null;
      this.delay_sec = null;
      this.sync = null;
      this.hardware_version = null;
    }
    else {
      if (initObj.hasOwnProperty('on')) {
        this.on = initObj.on
      }
      else {
        this.on = false;
      }
      if (initObj.hasOwnProperty('wait')) {
        this.wait = initObj.wait
      }
      else {
        this.wait = false;
      }
      if (initObj.hasOwnProperty('timeout')) {
        this.timeout = initObj.timeout
      }
      else {
        this.timeout = 0.0;
      }
      if (initObj.hasOwnProperty('delay_sec')) {
        this.delay_sec = initObj.delay_sec
      }
      else {
        this.delay_sec = 0.0;
      }
      if (initObj.hasOwnProperty('sync')) {
        this.sync = initObj.sync
      }
      else {
        this.sync = false;
      }
      if (initObj.hasOwnProperty('hardware_version')) {
        this.hardware_version = initObj.hardware_version
      }
      else {
        this.hardware_version = 0;
      }
    }
  }

  static serialize(obj, buffer, bufferOffset) {
    // Serializes a message object of type VacuumGripperCtrlRequest
    // Serialize message field [on]
    bufferOffset = _serializer.bool(obj.on, buffer, bufferOffset);
    // Serialize message field [wait]
    bufferOffset = _serializer.bool(obj.wait, buffer, bufferOffset);
    // Serialize message field [timeout]
    bufferOffset = _serializer.float32(obj.timeout, buffer, bufferOffset);
    // Serialize message field [delay_sec]
    bufferOffset = _serializer.float32(obj.delay_sec, buffer, bufferOffset);
    // Serialize message field [sync]
    bufferOffset = _serializer.bool(obj.sync, buffer, bufferOffset);
    // Serialize message field [hardware_version]
    bufferOffset = _serializer.int16(obj.hardware_version, buffer, bufferOffset);
    return bufferOffset;
  }

  static deserialize(buffer, bufferOffset=[0]) {
    //deserializes a message object of type VacuumGripperCtrlRequest
    let len;
    let data = new VacuumGripperCtrlRequest(null);
    // Deserialize message field [on]
    data.on = _deserializer.bool(buffer, bufferOffset);
    // Deserialize message field [wait]
    data.wait = _deserializer.bool(buffer, bufferOffset);
    // Deserialize message field [timeout]
    data.timeout = _deserializer.float32(buffer, bufferOffset);
    // Deserialize message field [delay_sec]
    data.delay_sec = _deserializer.float32(buffer, bufferOffset);
    // Deserialize message field [sync]
    data.sync = _deserializer.bool(buffer, bufferOffset);
    // Deserialize message field [hardware_version]
    data.hardware_version = _deserializer.int16(buffer, bufferOffset);
    return data;
  }

  static getMessageSize(object) {
    return 13;
  }

  static datatype() {
    // Returns string type for a service object
    return 'xarm_msgs/VacuumGripperCtrlRequest';
  }

  static md5sum() {
    //Returns md5sum for a message object
    return 'bc32f2abd5876101c06705ab6e53499a';
  }

  static messageDefinition() {
    // Returns full string definition for message
    return `
    bool on
    bool wait
    float32 timeout
    float32 delay_sec
    # sync: whether to execute in the motion queue
    bool sync
    # hardware_version==1: Plug-in Connection, default
    # hardware_version==2: Contact Connection
    int16 hardware_version
    
    
    `;
  }

  static Resolve(msg) {
    // deep-construct a valid message object instance of whatever was passed in
    if (typeof msg !== 'object' || msg === null) {
      msg = {};
    }
    const resolved = new VacuumGripperCtrlRequest(null);
    if (msg.on !== undefined) {
      resolved.on = msg.on;
    }
    else {
      resolved.on = false
    }

    if (msg.wait !== undefined) {
      resolved.wait = msg.wait;
    }
    else {
      resolved.wait = false
    }

    if (msg.timeout !== undefined) {
      resolved.timeout = msg.timeout;
    }
    else {
      resolved.timeout = 0.0
    }

    if (msg.delay_sec !== undefined) {
      resolved.delay_sec = msg.delay_sec;
    }
    else {
      resolved.delay_sec = 0.0
    }

    if (msg.sync !== undefined) {
      resolved.sync = msg.sync;
    }
    else {
      resolved.sync = false
    }

    if (msg.hardware_version !== undefined) {
      resolved.hardware_version = msg.hardware_version;
    }
    else {
      resolved.hardware_version = 0
    }

    return resolved;
    }
};

class VacuumGripperCtrlResponse {
  constructor(initObj={}) {
    if (initObj === null) {
      // initObj === null is a special case for deserialization where we don't initialize fields
      this.ret = null;
      this.message = null;
    }
    else {
      if (initObj.hasOwnProperty('ret')) {
        this.ret = initObj.ret
      }
      else {
        this.ret = 0;
      }
      if (initObj.hasOwnProperty('message')) {
        this.message = initObj.message
      }
      else {
        this.message = '';
      }
    }
  }

  static serialize(obj, buffer, bufferOffset) {
    // Serializes a message object of type VacuumGripperCtrlResponse
    // Serialize message field [ret]
    bufferOffset = _serializer.int16(obj.ret, buffer, bufferOffset);
    // Serialize message field [message]
    bufferOffset = _serializer.string(obj.message, buffer, bufferOffset);
    return bufferOffset;
  }

  static deserialize(buffer, bufferOffset=[0]) {
    //deserializes a message object of type VacuumGripperCtrlResponse
    let len;
    let data = new VacuumGripperCtrlResponse(null);
    // Deserialize message field [ret]
    data.ret = _deserializer.int16(buffer, bufferOffset);
    // Deserialize message field [message]
    data.message = _deserializer.string(buffer, bufferOffset);
    return data;
  }

  static getMessageSize(object) {
    let length = 0;
    length += _getByteLength(object.message);
    return length + 6;
  }

  static datatype() {
    // Returns string type for a service object
    return 'xarm_msgs/VacuumGripperCtrlResponse';
  }

  static md5sum() {
    //Returns md5sum for a message object
    return '76c68a2c5e109f4d6a4dc1cfc355f405';
  }

  static messageDefinition() {
    // Returns full string definition for message
    return `
    
    int16 ret
    string message
    
    
    `;
  }

  static Resolve(msg) {
    // deep-construct a valid message object instance of whatever was passed in
    if (typeof msg !== 'object' || msg === null) {
      msg = {};
    }
    const resolved = new VacuumGripperCtrlResponse(null);
    if (msg.ret !== undefined) {
      resolved.ret = msg.ret;
    }
    else {
      resolved.ret = 0
    }

    if (msg.message !== undefined) {
      resolved.message = msg.message;
    }
    else {
      resolved.message = ''
    }

    return resolved;
    }
};

module.exports = {
  Request: VacuumGripperCtrlRequest,
  Response: VacuumGripperCtrlResponse,
  md5sum() { return 'b01133253841b00d2aa64b79e0937eb6'; },
  datatype() { return 'xarm_msgs/VacuumGripperCtrl'; }
};
