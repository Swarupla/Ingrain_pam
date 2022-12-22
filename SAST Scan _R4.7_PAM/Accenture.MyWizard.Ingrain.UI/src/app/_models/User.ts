export class User {
  id: number;
  userName: string;
  password: string;
  firstName: string;
  lastName: string;
}

export class TrainedModels {
  Accuracy: any;
  modelName: any;
  RunTime: any;
  FeatureWeight: any;
  mseValues: any;
  maeValues: any;
  classificationReport = {};
  matthewsCoefficient: number;
  confustionMatirxImg_64: any;
  img_64: any;
  f1Score: number;
  ROCAUCvalue :number;
}
