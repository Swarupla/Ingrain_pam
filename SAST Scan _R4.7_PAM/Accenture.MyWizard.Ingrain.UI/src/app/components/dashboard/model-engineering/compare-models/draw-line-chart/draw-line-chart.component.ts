import {
  Component, OnInit, OnChanges, Input, Output, EventEmitter,
  ViewChild, ElementRef, Inject, ChangeDetectorRef, AfterContentChecked, ChangeDetectionStrategy
} from '@angular/core';
import { CookieService } from 'ngx-cookie-service';
import { CoreUtilsService } from '../../../../../_services/core-utils.service';
import { AppUtilsService } from '../../../../../_services/app-utils.service';
@Component({
  selector: 'app-draw-line-chart',
  templateUrl: './draw-line-chart.component.html',
  styleUrls: ['./draw-line-chart.component.scss']
})
export class DrawLineChartComponent implements OnChanges {
  @Input() divId: any;
  @Input() testScenarioAllList: any = [];
  @Input() testCaseScenarioOptions: any;
  @Input() freequecnyTypeonSelection: any;
  @Input() stepsData: any = [];
  // tslint:disable-next-line: no-output-rename
  @Output('selectedValues') selectedValues = new EventEmitter();
  selectedModelLineChart: any;
  selectedModelName: string;
  frequency: string;
  modelType: string;
  selectedModelAccuracy: string;
  selectedModelRunTime: string;
  lineChartDataCount: Number;
  stepsValueonSelection: any;
  numbers: any;
  seletedDiv: number;
  selectedValue: string;
  divMainId: any;
  seletedDivBool: boolean;
  formattedSteps: any = [];
  padTop: string;
  showOptions: boolean;
  disableTestScenButton: boolean;
  testCaseScenarioCount: number;
  screenWidth:any;
  constructor(@Inject(ElementRef) private eleRef: ElementRef, private cookieService: CookieService,
    private coreUtilsService: CoreUtilsService, private appUtilsService: AppUtilsService) { }


  ngOnChanges() {
    this.seletedDiv = 1;
    this.screenWidth = window.innerWidth;
    this.padTop = this.divId === 0 ? '' : 'pad-top-15';
    this.selectedModelName = localStorage.getItem('SelectedRecommendedModel'); // this.cookieService.get('SelectedRecommendedModel');
    this.selectedModelAccuracy = localStorage.getItem('SelectedModelAccuracy'); // this.cookieService.get('SelectedModelAccuracy');
    this.selectedModelRunTime = localStorage.getItem('SelectedModelRunTime'); // this.cookieService.get('SelectedModelRunTime');
    this.frequency = this.cookieService.get('Frequency');
    this.modelType = this.cookieService.get('ProblemType');
    this.divMainId = 'div' + this.divId;
    if (this.divId === 0 && !this.coreUtilsService.isNil(this.testCaseScenarioOptions) &&
      this.testCaseScenarioOptions.length <= 1) {
      this.disableTestScenButton = this.testCaseScenarioOptions.length <= 1 ? true : false;
    }
    this.testCaseScenarioCount = !this.coreUtilsService.isNil(this.testCaseScenarioOptions)
      ? this.testCaseScenarioOptions.length : 0;
    const filteredTestScenarios = [];
    this.showOptions = this.coreUtilsService.isNil(this.testCaseScenarioOptions) || this.testCaseScenarioOptions.length === 0
      ? false : true;
    if (!this.coreUtilsService.isNil(this.testCaseScenarioOptions)) {
      this.testScenarioAllList.forEach(testCaseScenario => {
        for (let index = 0; index < this.testCaseScenarioOptions.length; index++) {
          if (testCaseScenario.ScenarioName === this.testCaseScenarioOptions[index].scenarioName) {
            filteredTestScenarios.push(testCaseScenario);
          }
        }
      });
    }

    const scenarioName = filteredTestScenarios.length > 0 ? filteredTestScenarios[0].ScenarioName : '';
    this.selectedModelLineChart = [];
    const testdata = this.testScenarioAllList.find(testCase => testCase.ScenarioName === scenarioName);

    this.formattedSteps = [];
    for (let index = 0; index < this.stepsData.length; index++) {
      const objStepsData = {};
      for (let indexobj = 0; indexobj < this.stepsData[index].length; indexobj++) {
        const splitArray = this.stepsData[index][indexobj].split(':');
        if (!this.coreUtilsService.isNil(splitArray) && splitArray.length > 1) {
          const key = splitArray[0].trim();
          const value = splitArray[1].trim();
          objStepsData[key] = value;
        }
      }
      this.formattedSteps[index] = objStepsData;
    }
    this.setLineChart(testdata, scenarioName);

  }
  setLineChart(testdata: any, scenarioName: string) {
    if (!this.coreUtilsService.isNil(testdata)) {
      const filterStepsData = this.formattedSteps.find(steps => steps.WFId === testdata.WFId && steps.ScenarioName === scenarioName);
      this.stepsValueonSelection = !this.coreUtilsService.isNil(filterStepsData) ? filterStepsData.Steps : filterStepsData;

      this.freequecnyTypeonSelection = testdata.Frequency;
      for (let index = 0; index < testdata.Forecast.length; index++) {
        const objLineChart = {};
        objLineChart['Forecast'] = testdata.Forecast[index];
        objLineChart['RangeTime'] = testdata.RangeTime[index];
        this.selectedModelLineChart[index] = objLineChart;
      }
      this.lineChartDataCount = this.selectedModelLineChart.length;
    }
  }
  onTestScenarioChange(scenarioName: string, divId: number) {
    this.selectedModelLineChart = [];
    const testdata = this.testScenarioAllList.find(testCase => testCase.ScenarioName === scenarioName);
    this.setLineChart(testdata, scenarioName);
    this.selectedValue = scenarioName;
    const objData = {};

    const firstDiv = this.eleRef.nativeElement.parentElement.parentElement.querySelector('#div1');
    if (!this.coreUtilsService.isNil(firstDiv)) {
      const selectedDiv = firstDiv.firstElementChild.className;
      const secondDiv = this.eleRef.nativeElement.parentElement.parentElement.querySelector('#div2');
      if (!this.coreUtilsService.isNil(secondDiv)) {
        const selectedSDiv = secondDiv.firstElementChild.className;
        if (selectedDiv.indexOf('hide-div') > -1 && selectedSDiv.indexOf('hide-div') > -1) {
          objData['scenarioName'] = scenarioName;
          objData['divId'] = divId;
          this.selectedValues.emit(objData);
        }
      } else if (selectedDiv.indexOf('hide-div') > -1) {
        objData['scenarioName'] = scenarioName;
        objData['divId'] = divId;
        this.selectedValues.emit(objData);
      }
    }
  }
  addTestCaseScnerio(divId: any, testScenarioCount: any) {
    if (this.divId < 3) {
      this.seletedDivBool = true;
      const firstDiv = this.eleRef.nativeElement.parentElement.parentElement.querySelector('#div1');
      const btnAddTestScenario = this.eleRef.nativeElement.parentElement.parentElement.querySelector('#btnAddTestScenatio');
      let selectedDiv = '';
      if (this.seletedDivBool && !this.coreUtilsService.isNil(firstDiv)) {
        selectedDiv = firstDiv.firstElementChild.className;
        if (selectedDiv.indexOf('hide-div') > -1) {
          firstDiv.firstElementChild.className = 'ingrAI-box-container display-div';
          this.seletedDivBool = false;
        }
      }
      const secondDiv = this.eleRef.nativeElement.parentElement.parentElement.querySelector('#div2');
      let selectedSDiv;
      if (this.seletedDivBool === true && !this.coreUtilsService.isNil(secondDiv)) {
        selectedSDiv = secondDiv.firstElementChild.className;
        if (selectedSDiv.indexOf('hide-div') > -1) {
          secondDiv.firstElementChild.className = 'ingrAI-box-container display-div';

        }

      }
      if (!this.coreUtilsService.isNil(firstDiv) &&
        firstDiv.firstElementChild.className.indexOf('display-div') > -1 &&
        !this.coreUtilsService.isNil(secondDiv) &&
        secondDiv.firstElementChild.className.indexOf('display-div') > -1) {
        btnAddTestScenario.setAttribute('disabled', true);
      } else if (this.coreUtilsService.isNil(secondDiv) && testScenarioCount === 2) {
        btnAddTestScenario.setAttribute('disabled', true);
      } else {
        btnAddTestScenario.removeAttribute('disabled');
      }
    }
  }
  onClose(divId: Number) {
    if (divId > 0 && divId < 3) {
      const firstDiv = this.eleRef.nativeElement.parentElement.parentElement.querySelector('#div1');
      const btnAddTestScenario = this.eleRef.nativeElement.parentElement.parentElement.querySelector('#btnAddTestScenatio');
      if (divId === 1 && !this.coreUtilsService.isNil(firstDiv)) {
        const selectedDiv = firstDiv.firstElementChild.className;
        if (selectedDiv.indexOf('display-div') > -1) {
          firstDiv.firstElementChild.className = 'ingrAI-box-container hide-div';
          btnAddTestScenario.removeAttribute('disabled');
        }
      }
      const secondDiv = this.eleRef.nativeElement.parentElement.parentElement.querySelector('#div2');
      if (divId === 2 && !this.coreUtilsService.isNil(secondDiv)) {
        const selectedSDiv = secondDiv.firstElementChild.className;
        if (selectedSDiv.indexOf('display-div') > -1) {
          secondDiv.firstElementChild.className = 'ingrAI-box-container hide-div';
          btnAddTestScenario.removeAttribute('disabled');
        }
      }
      const objData = {};
      objData['scenarioName'] = this.selectedValue;
      objData['divId'] = divId;
      this.selectedValues.emit(objData);
    }
  }
}
