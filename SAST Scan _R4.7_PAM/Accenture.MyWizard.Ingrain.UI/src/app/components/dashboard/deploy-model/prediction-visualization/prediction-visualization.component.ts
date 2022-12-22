import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';
import { LocalStorageService } from 'src/app/_services/local-storage.service';
import { MyVisualizationService } from '../../my-visualization/my-visualization.service';


@Component({
  selector: 'app-prediction-visualization',
  templateUrl: './prediction-visualization.component.html',
  styleUrls: ['./prediction-visualization.component.scss']
})
export class PredictionVisualization implements OnInit {
  public isMonitoring: string;
  chartData;
  ylabels;
  visualizationResponse;

  accuracyChartData;
  inputDriftChartData;
  targetVarianceChartData;
  dataQualityChartData;
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

  constructor(
    private prediction: MyVisualizationService, private appUtilsService: AppUtilsService, private ls: LocalStorageService,
    private coreUtilService: CoreUtilsService, private router: Router) { }

  ngOnInit() {
      this.openPrediction();
  }

  openPrediction() {
    this.coreUtilService.disableTabs(3, 1);
    const correlationId = this.ls.getCorrelationId();
    const modelName = sessionStorage.getItem('modelVersion');

    // const correlationId ='a172115f-4f9a-4cc1-b226-103305cba305';
    // const modelName = 'Random Forest Classifier';

    // const correlationId ='27a0476d-ab64-41d0-9a7b-c1bf612c7b18';
    // const modelName = 'Lasso Regressor';

    this.appUtilsService.loadingStarted();
    this.prediction.getVisualizationDetails(correlationId,modelName, true).subscribe(
      (data)=>{
        const response = data;
        this.appUtilsService.loadingEnded();
        this.setModelDetails(response);
        this.visualizationResponse = response;
        this.problemType = response.ProblemType;
        if (this.problemType === 'Multi_Class' || this.problemType == 'Classification' || this.problemType == 'classification') {

          if (response.xlabel.length && response.predictionproba.length) {
            this.createDataForClassification(response);
          } else {
    
          }
        } else if (this.problemType === 'Regression' || this.problemType === 'regression') {
          this.lineChart = [];
          if (response.xlabel.length && response.predictionproba.length) {
            this.createDataForRegression(response);
          } 
        }
      }
    )

  }


  createDataForClassification(data) {
    //  this.chartData = data;
    this.isClassification = true;
    const xlabelAray = data['xlabel'];
    const legend = data['legend'];
    const plottingdata = data['predictionproba'];
    this.ylabels = data['legend'];
    let structuredData = {};
    const chartArray = [];
    for (const prediction in plottingdata) {
      if (plottingdata.hasOwnProperty(prediction)) {
        structuredData = {};
        const singleRowData = plottingdata[prediction];
        for (let i = 0; i < singleRowData.length; i++) {
          const key = legend[i];
          structuredData[key] = singleRowData[i] * 100;
        }
        const xvalue = xlabelAray[prediction];
        structuredData['xlabel'] = xvalue;
        chartArray.push(structuredData);
      }
    }
    this.chartData = chartArray;
  }

  createDataForRegression(response) {
    for (let index = 0; index < response.predictionproba.length; index++) {
      const objLineChart = {};
      objLineChart['XAxis'] = response.xlabel[index];
      objLineChart['YAxis'] = response.predictionproba[index];
      this.lineChart[index] = objLineChart;
      this.isRegression = true;
    }
    this.lineChartDataCount = this.lineChart.length;
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
