
"use strict";

let SetInt16 = require('./SetInt16.js')
let FtIdenLoad = require('./FtIdenLoad.js')
let ConfigToolModbus = require('./ConfigToolModbus.js')
let ClearErr = require('./ClearErr.js')
let Call = require('./Call.js')
let PlayTraj = require('./PlayTraj.js')
let GripperConfig = require('./GripperConfig.js')
let SetFloat32 = require('./SetFloat32.js')
let MoveVelo = require('./MoveVelo.js')
let GetControllerDigitalIO = require('./GetControllerDigitalIO.js')
let GripperMove = require('./GripperMove.js')
let SetAxis = require('./SetAxis.js')
let MoveAxisAngle = require('./MoveAxisAngle.js')
let TCPOffset = require('./TCPOffset.js')
let SetMultipleInts = require('./SetMultipleInts.js')
let GetFloat32List = require('./GetFloat32List.js')
let SetDigitalIO = require('./SetDigitalIO.js')
let MoveVelocity = require('./MoveVelocity.js')
let GripperState = require('./GripperState.js')
let GetDigitalIO = require('./GetDigitalIO.js')
let GetInt32 = require('./GetInt32.js')
let FtCaliLoad = require('./FtCaliLoad.js')
let SetLoad = require('./SetLoad.js')
let SetModbusTimeout = require('./SetModbusTimeout.js')
let GetAnalogIO = require('./GetAnalogIO.js')
let SetToolModbus = require('./SetToolModbus.js')
let VacuumGripperCtrl = require('./VacuumGripperCtrl.js')
let Move = require('./Move.js')
let GetSetModbusData = require('./GetSetModbusData.js')
let GetErr = require('./GetErr.js')
let SetString = require('./SetString.js')
let SetControllerAnalogIO = require('./SetControllerAnalogIO.js')

module.exports = {
  SetInt16: SetInt16,
  FtIdenLoad: FtIdenLoad,
  ConfigToolModbus: ConfigToolModbus,
  ClearErr: ClearErr,
  Call: Call,
  PlayTraj: PlayTraj,
  GripperConfig: GripperConfig,
  SetFloat32: SetFloat32,
  MoveVelo: MoveVelo,
  GetControllerDigitalIO: GetControllerDigitalIO,
  GripperMove: GripperMove,
  SetAxis: SetAxis,
  MoveAxisAngle: MoveAxisAngle,
  TCPOffset: TCPOffset,
  SetMultipleInts: SetMultipleInts,
  GetFloat32List: GetFloat32List,
  SetDigitalIO: SetDigitalIO,
  MoveVelocity: MoveVelocity,
  GripperState: GripperState,
  GetDigitalIO: GetDigitalIO,
  GetInt32: GetInt32,
  FtCaliLoad: FtCaliLoad,
  SetLoad: SetLoad,
  SetModbusTimeout: SetModbusTimeout,
  GetAnalogIO: GetAnalogIO,
  SetToolModbus: SetToolModbus,
  VacuumGripperCtrl: VacuumGripperCtrl,
  Move: Move,
  GetSetModbusData: GetSetModbusData,
  GetErr: GetErr,
  SetString: SetString,
  SetControllerAnalogIO: SetControllerAnalogIO,
};
