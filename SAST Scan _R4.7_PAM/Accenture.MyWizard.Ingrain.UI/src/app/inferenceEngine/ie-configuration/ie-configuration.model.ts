export class DataInflow {
  'DateColumn': '';
  'TrendForecast': string;
  'Frequency': Array<any>;
  'Dimensions': Set<string>
}

export class MeasureInflow {
  'MetricColumn': '';
  'DateColumn': '';
  'Features': Set<string>;
  'FeatureCombinations': Array<any>
}