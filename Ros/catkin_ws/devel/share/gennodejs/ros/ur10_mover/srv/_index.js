
"use strict";

let StateService = require('./StateService.js')
let SampleService = require('./SampleService.js')
let TrainingDataService = require('./TrainingDataService.js')
let GripperService = require('./GripperService.js')
let GetTrainingDataService = require('./GetTrainingDataService.js')
let TrainingService = require('./TrainingService.js')
let PlannerService = require('./PlannerService.js')
let DiscardService = require('./DiscardService.js')
let ExecutionService = require('./ExecutionService.js')

module.exports = {
  StateService: StateService,
  SampleService: SampleService,
  TrainingDataService: TrainingDataService,
  GripperService: GripperService,
  GetTrainingDataService: GetTrainingDataService,
  TrainingService: TrainingService,
  PlannerService: PlannerService,
  DiscardService: DiscardService,
  ExecutionService: ExecutionService,
};
