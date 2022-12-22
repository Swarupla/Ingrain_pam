import { Component, OnInit, ElementRef, Input } from '@angular/core';
import { MyVisualizationService } from './my-visualization.service';
import { LocalStorageService } from 'src/app/_services/local-storage.service';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { Router } from '@angular/router';
import { ModelMonitoringComponent } from '../deploy-model/model-monitoring/model-monitoring.component';
import { DialogService } from 'src/app/dialog/dialog.service';
import { tap } from 'rxjs/operators';

@Component({
  selector: 'app-my-visualization',
  templateUrl: './my-visualization.component.html',
  styleUrls: ['./my-visualization.component.scss'],
  host: {
    '(document:click)': 'onClick($event)'
  }
})
export class MyVisualizationComponent implements OnInit {
  show: boolean;
  correlationId: string;
  @Input() model;
  @Input() parentComponent: string;
  lineChart = [];
  screenWidth: any;
  isData: boolean;
  freequecnyTypeonSelection: any;
  lineChartDataCount: any;
  isTimeSeries: boolean;
  isRegression: boolean;
  problemType: string;
  chartData;
  ylabels;
  showErrorMessage: boolean;
  isClassification: boolean;
  visualizationResponse: any;

  constructor(private _eref: ElementRef,
    private visualizationService: MyVisualizationService,
    private _localStorageService: LocalStorageService,
    private _appUtilsService: AppUtilsService,
    private dialogService: DialogService,
    private router: Router) { }

  ngOnInit() {
    this.correlationId = this._localStorageService.getCorrelationId();
    this.screenWidth = window.innerWidth;
  }

  onClick(event) {
    if (!this._eref.nativeElement.contains(event.target)) {
      this.show = false;
    } else {
      if (document.getElementsByClassName('nav-active').length) {
        if ( event.target.className === 'btn btn-primary btn-sm mt-1 ingrAI-visual-close') {
          this.router.navigate(['/dashboard/deploymodel/Prediction']);
          // this.openModalForMonitoring();
        }
        this.show = false;
      } else {
        this.showErrorMessage = false;
        if (this.model && this.model.CorrelationId && this.model.ModelVersion) {
          this._appUtilsService.loadingStarted();
          this._localStorageService.setLocalStorageData('correlationId', this.model.CorrelationId);
          this._localStorageService.setLocalStorageData('modelName', this.model.ModelName);
          sessionStorage.setItem('modelVersion', this.model.ModelVersion);
          this.visualizationService.getVisualizationDetails(this.model.CorrelationId, this.model.ModelVersion).subscribe(response => {
            this._appUtilsService.loadingEnded();
            this.problemType = response.ProblemType;
            this.showErrorMessage = false;
            this.visualizationResponse = response;
            if (this.problemType === 'Multi_Class' || this.problemType == 'Classification' || this.problemType == 'classification') {
              this.show = true;
              if (response.xlabel.length && response.predictionproba.length) {
                this.createDataForClassification(response);
              } else {
                this.showErrorMessage = true;
              }
            } else if (this.problemType === 'TimeSeries') {
              this.show = true;
              this.lineChart = [];
              if (response.Forecast.length && response.RangeTime.length) {
                this.createDataForTimeSeries(response);
              } else {
                this.showErrorMessage = true;
              }
            } else if (this.problemType === 'Regression' || this.problemType === 'regression') {
              this.show = true;
              this.lineChart = [];
              if (response.xlabel.length && response.predictionproba.length) {
                this.createDataForRegression(response);
              } else {
                this.showErrorMessage = true;
              }

            }

          }, err => {
            this._appUtilsService.loadingEnded();
            this.show = true;
            this.showErrorMessage = true;
          }
          )
        }

      }
    }
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
          structuredData[key] = singleRowData[i];
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

  createDataForTimeSeries(response) {
    this.freequecnyTypeonSelection = response.Frequency;
    for (let index = 0; index < response.Forecast.length; index++) {
      const objLineChart = {};
      objLineChart['Forecast'] = response.Forecast[index];
      objLineChart['RangeTime'] = response.RangeTime[index];
      this.lineChart[index] = objLineChart;
      this.isTimeSeries = true;
    }
    this.lineChartDataCount = this.lineChart.length;
  }

  openModalForMonitoring() {
    // this._appUtilsService.loadingImmediateEnded();
    const apiUploadAfterClosed = this.dialogService.open(ModelMonitoringComponent,
        {data: { }}).afterClosed.pipe(
          tap(data => '')
      );

    apiUploadAfterClosed.subscribe();
}


}


