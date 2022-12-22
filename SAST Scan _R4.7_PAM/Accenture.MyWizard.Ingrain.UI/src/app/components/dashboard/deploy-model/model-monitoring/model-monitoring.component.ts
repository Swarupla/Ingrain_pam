import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';
import { ModelMonitoringService } from './model-monitoring.service';

@Component({
  selector: 'app-model-monitoring',
  templateUrl: './model-monitoring.component.html',
  styleUrls: ['./model-monitoring.component.scss']
})
export class ModelMonitoringComponent implements OnInit {
  public isMonitoring: string;
  chartData;
  ylabels;
  visualizationResponse;

  accuracyChartData;
  inputDriftChartData;
  targetVarianceChartData;
  dataQualityChartData;

  accuracyCurrentValue;
  inputDriftCurrentValue;
  targetVarianceCurrentValue;
  dataQualityCurrentValue;

  accuracyCurrentValueThershold;
  inputDriftCurrentValueThershold;
  targetVarianceCurrentValueThershold;
  dataQualityCurrentValueThershold;

  trainedModelHistory;
  lineChart;
  isClassification: boolean;
  isRegression: boolean;
  lineChartDataCount: any;
  problemType;

  
  modelName: String;
  datasource;
  useCase;
  correlationId;
  deliveryTypeName;
  ModelHealth: any;

  constructor( private modelMonitoringService: ModelMonitoringService,
     private coreUtilService: CoreUtilsService,
     private router: Router,
     private appUtilsService: AppUtilsService) { }

  ngOnInit() {
      this.openMonitoring();
  }

  openMonitoring() {
    this.coreUtilService.disableTabs(3, 1);

    const sampleData = {
      CorId: 1,
      ReqId: 2,
      "ModelHealth": "Healthy",
      "Accuracy": {
        "ThresholdValue": 65.0,
        "CurrentValue": 82.0,
        "GraphData": {
          date: ['21/06/2019 00:00:00', '21/07/2019 00:00:00', '21/08/2019 00:00:00', '21/09/2019 00:00:00', '21/10/2019 00:00:00'],
          metricvalue: [70, 40, 80, 40, 50]
        }
      },
      "InputDrift": {
        "ThresholdValue": 70.0,
        "CurrentValue": 82.0,
        "GraphData": {
          date: ['21/06/2019 00:00:00', '21/07/2019 00:00:00', '21/08/2019 00:00:00', '21/09/2019 00:00:00', '21/10/2019 00:00:00'],
          metricvalue: [30, 70, 85, 98, 50]
        }
      },
      "TargetVariance": {
        "ThresholdValue": 90.0,
        "CurrentValue": 82.0,
        "GraphData": {
          date: ['21/06/2019 00:00:00', '21/07/2019 00:00:00', '21/08/2019 00:00:00', '21/09/2019 00:00:00', '21/10/2019 00:00:00'],
          metricvalue: [60, 70, 80, 30, 50]
        }
      },
      "DataQuality": {
        "ThresholdValue": 40.0,
        "CurrentValue": 82.0,
        "GraphData": {
          date: ['21/06/2019 00:00:00', '21/07/2019 00:00:00', '21/08/2019 00:00:00', '21/09/2019 00:00:00', '21/10/2019 00:00:00'],
          metricvalue: [60, 70, 80, 30, 50]
        }
      }
      // yyyy-mm-dd dd/mm/yyyy
    };

    this.appUtilsService.loadingStarted();
    this.modelMonitoringService.getModelData().subscribe(
      (data) => {
        if (data) {
          const sampleData = data.modelMetrics
          this.ModelHealth = sampleData.ModelHealth;
          this.correlationId = sampleData.CorrelationId;
          this.accuracyChartData = this.setAccuracyChart(sampleData.Accuracy);
          this.inputDriftChartData = this.setAccuracyChart(sampleData.InputDrift);
          this.targetVarianceChartData = this.setAccuracyChart(sampleData.TargetVariance);
          this.dataQualityChartData = this.setAccuracyChart(sampleData.DataQuality);
 
          this.accuracyCurrentValue = sampleData.Accuracy.CurrentValue;
          this.inputDriftCurrentValue = sampleData.InputDrift.CurrentValue;
          this.targetVarianceCurrentValue = sampleData.TargetVariance.CurrentValue;
          this.dataQualityCurrentValue = sampleData.DataQuality.CurrentValue;

          this.accuracyCurrentValueThershold = sampleData.Accuracy.ThresholdValue;
          this.inputDriftCurrentValueThershold = sampleData.InputDrift.ThresholdValue;
          this.targetVarianceCurrentValueThershold = sampleData.TargetVariance.ThresholdValue;
          this.dataQualityCurrentValueThershold = sampleData.DataQuality.ThresholdValue;

          this.setModelDetails(data.modelDetails)
          this.appUtilsService.loadingEnded();

        }
      },
      (error) => {
        this.appUtilsService.loadingEnded();
      }
    )

  }

  setAccuracyChart(data) {
    const dataFormatted = [];
    if ( data && data.hasOwnProperty('GraphData')) {
    for (let index = 0; index < data.GraphData.Date.length; index++) {
      const objLineChart = {};
      objLineChart['Actual'] = data.ThresholdValue;
      objLineChart['Forecast'] = data.GraphData.MetricValue[index];
      objLineChart['RangeTime'] = (index + 1) + "" + data.GraphData.Date[index].split(' ')[0];
      dataFormatted[index] = objLineChart;
    }
    return dataFormatted;
  } else {
    return data;
  }
   
  }


  TrainingModelHistoryData() {
    // const data = {
    //     "CorrelationId": "96dd0668-77ce-4d9b-a760-897ed911fc49",
    //     "LastRequestId": "3daac474-df5d-4463-98ba-95916ac0c2db",
    //     "ClientUId": "00100000-0000-0000-0000-000000000000",
    //     "DeliveryConstructUID": "1e4272c7-d9e6-4df8-b3da-da3d056f35a1",
    //     "CreatedOn": "2020-10-07 06:13:22",
    //     "CreatedByUser": "mywizardsystemdataadmin@mwphoenix.onmicrosoft.com",
    //     "ModifiedOn": "2020-10-07 06:55:26",
    //     "ModifiedByUser": "mywizardsystemdataadmin@mwphoenix.onmicrosoft.com",
    //     "LastDeployedDate": "2020-10-07 10:14:28",
    //     "TrainedModelHistory": [
    //       {
    //         "Accuracy": {"CurrentValue":100,"ThresholdValue":77},
    //         "InputDrift": {"CurrentValue":23,"ThresholdValue":85},
    //         "TargetVariance": {"CurrentValue":20,"ThresholdValue":14},
    //         "DataQuality": {"CurrentValue":95,"ThresholdValue":75},
            
    //         "Date": "2020-10-07 10:08:25"
    //       },
    //       {
    //         "Accuracy": {"CurrentValue":100,"ThresholdValue":85},
    //         "InputDrift": {"CurrentValue":23,"ThresholdValue":85},
    //         "TargetVariance": {"CurrentValue":20,"ThresholdValue":14},
    //         "DataQuality": {"CurrentValue":100,"ThresholdValue":85},
    //         "Date": "2020-10-07 10:14:28"
    //       }
    //     ]
    //   };
  this.trainedModelHistory = [];
   this.modelMonitoringService.getTrainedHistory().subscribe(
     (data) => {
          if ( data && data[0].TrainedModelHistory) {
            this.trainedModelHistory = data[0].TrainedModelHistory;
          }
     });
  //  this.trainedModelHistory = data.TrainedModelHistory;
  }


  setModelDetails(data) {
  
    this.useCase = data.BusinessProblems; //  "test"
    this.deliveryTypeName = data.Category; // "AD"
    this.datasource = data.DataSource; // "insurance.csv"
    this.modelName = data.ModelName; //  "test3516"
    // this.correlationId = 
  }

  navigateToMyModels() {
    // this.uts.usageTracking('Problem Statement', 'My Models');
    this.router.navigate(['/dashboard/problemstatement/templates']);
  }

  navigateTodeployModel() {
   this.router.navigate(['/dashboard/deploymodel/deployedmodel']);
  }
}
