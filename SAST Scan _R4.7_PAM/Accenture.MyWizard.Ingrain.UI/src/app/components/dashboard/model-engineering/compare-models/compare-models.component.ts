import {
  Component, OnInit, Input, ViewChild, ElementRef, Inject,
  ChangeDetectorRef, AfterContentChecked, ChangeDetectionStrategy
} from '@angular/core';
import { LocalStorageService } from '../../../../_services/local-storage.service';
import { CookieService } from 'ngx-cookie-service';
import { empty, throwError, timer, of } from 'rxjs';
import { Router, ActivatedRoute } from '@angular/router';
import { CoreUtilsService } from '../../../../_services/core-utils.service';
import { AppUtilsService } from '../../../../_services/app-utils.service';
import { DialogService } from 'src/app/dialog/dialog.service';
import { tap, catchError, switchMap, filter } from 'rxjs/operators';
import { TemplateNameModalComponent } from '../../../template-name-modal/template-name-modal.component';
import { ProblemStatementService } from '../../../../_services/problem-statement.service';
import { NotificationService } from '../../../../_services/notification-service.service';
import { CompareModelsService } from '../../../../_services/compare-models.service';
import * as _ from 'lodash';
import { UsageTrackingService } from 'src/app/_services/usage-tracking.service';

@Component({
  selector: 'app-compare-models',
  templateUrl: './compare-models.component.html',
  styleUrls: ['./compare-models.component.scss']
})
export class CompareModelsComponent implements OnInit {
  selectedModelName: any;
  displayDataSourceLink: boolean;
  modelName: string;
  useCase: string;
  dataSourceName: string;
  correlationId: string;
  newModelName: string;
  newCorrelationIdAfterCloned: string;
  accuracy: any;
  testCaseScenarioOptions: any = [];
  testCaseScenarioOptions0: any = [];
  testCaseScenarioOptions1: any = [];
  testCaseScenarioOptions2: any = [];
  testDataListCount: number;
  testDataListCount1: number;
  testDataListCount2: number;
  testScenarioAllList;
  steps;
  testScenarioAllListNew;
  testDataList;
  testDataList1;
  testDataList2;
  page = 1;
  page1 = 1;
  page2 = 1;
  chartData: any;
  showSaveAs: boolean;
  testCaseDataList;
  selectedModelAccuracy: string;
  selectedModelRunTime: string;
  firstDivSelectedScenarioName: string;
  secondDivSelectedScenarioName: string;
  thirdDivSelectedScenarioName: string;
  addTileCount = 0;
  modelType: string;
  filterTestScenarioList;
  filterTestScenarioList1;
  addTimeSeriesCounter = 0;
  allTestScenario: any = [];
  disableTestSecenarioBtn: boolean;
  deliveryTypeName;
  isTextPreProcessing = false;
  isNLPClassification: boolean;
  isClusteringFlag: boolean;
  PredictionOutcomeChart = {
    height: 220,
    width:  500
  };
  readOnly;

  constructor(@Inject(ElementRef) private eleRef: ElementRef, private notificationService: NotificationService,
    private localStorService: LocalStorageService, private cookieService: CookieService,
    private dialogService: DialogService,
    private appUtilsService: AppUtilsService, private coreUtilsService: CoreUtilsService,
    private _problemStatementService: ProblemStatementService,
    private _router: Router, private compareModelsService: CompareModelsService,
    private uts: UsageTrackingService) { }

  ngOnInit() {
    this.readOnly = sessionStorage.getItem('viewEditAccess');
    this.addTileCount = 0;
    this.displayDataSourceLink = true;
    this.testDataListCount1 = 0;
    this.testDataListCount2 = 0;
    this.testDataListCount = 0;
    this.testDataList = [];
    this.testDataList1 = [];
    this.testDataList2 = [];
    this.filterTestScenarioList = [];
    this.filterTestScenarioList1 = [];
    this.page = 1;
    this.page1 = 1;
    this.page2 = 1;
    this.selectedModelName = localStorage.getItem('SelectedRecommendedModel'); // this.cookieService.get('SelectedRecommendedModel');
    this.selectedModelAccuracy = localStorage.getItem('SelectedModelAccuracy'); // this.cookieService.get('SelectedModelAccuracy');
    this.selectedModelRunTime = localStorage.getItem('SelectedModelRunTime'); // this.cookieService.get('SelectedModelRunTime');
    this.modelType = this.cookieService.get('ProblemType');
    this.correlationId = this.localStorService.getCorrelationId();
    // const this = this;
    this.appUtilsService.loadingStarted();
    this.testDataList = [];
    this.uts.usageTracking('Model Engineering', 'Compare Test Scenarios');
    this.compareModelsService.getCompareTestScenarios(this.correlationId, this.selectedModelName).subscribe(
      data => {

        this.modelName = data.ModelName;
        this.dataSourceName = data.DataSource;
        this.useCase = data.BusinessProblem;
        this.modelType = data.ModelType;
        this.cookieService.set('ModelTypeForInstaML', data.ModelType);
        this.testScenarioAllList = data.TestData;
        this.deliveryTypeName = data.Category;

        const testScenarioss = data.TestScenarios;
        this.allTestScenario = [];
        if (this.coreUtilsService.isNil(data.TestData) || this.coreUtilsService.isNil(data.TestScenarios)) {
          this.notificationService.warning(' There is no test scenarios created for selected model');
        } else {
          if (!this.coreUtilsService.isNil(testScenarioss)) {
            this.testCaseScenarioOptions = [];
            this.testCaseScenarioOptions0 = [];
            for (let index = 0; index < testScenarioss.length; index++) {
              const testCaseName = testScenarioss[index].trim();
              this.testCaseScenarioOptions.push({ scenarioName: testCaseName, value: testCaseName });
              this.testCaseScenarioOptions0.push({ scenarioName: testCaseName, value: testCaseName });
            }
            this.allTestScenario.push(this.testCaseScenarioOptions);
          }
          if (this.modelType === 'TimeSeries') {
            this.steps = data.steps;
            this.setTestScenarioTimeSeries(this.testCaseScenarioOptions[0].scenarioName);
          } else {
            if (!this.coreUtilsService.isNil(data.TestData)) {
              this.isClusteringFlag = data.TestData[0].Clustering_Flag;
              this.firstDivSelectedScenarioName = this.testCaseScenarioOptions[0].value;
              this.setTestScenarioList(this.testCaseScenarioOptions[0].value);
              this.disableTestSecenarioBtn = this.testCaseScenarioOptions.length <= 1 ? true : false;
            }
          }
        }


        this.appUtilsService.loadingEnded();
      }, error => {
        this.appUtilsService.loadingEnded();
        this.notificationService.error('Something went wrong to getTeachTestData.');
      }
    );
  }

  setTestScenarioList(scenarioName: string) {
    this.testScenarioAllListNew = [];
    this.testScenarioAllList.forEach(data => {
      this.testDataList = [];
      if (!this.coreUtilsService.isNil(data) && !this.coreUtilsService.isNil(data.Predictions)) {
        Object.entries(data.Predictions).forEach(
          ([key, value]) => {
            this.testDataList.push(value);
          });
        for (let index = 0; index < this.testDataList.length; index++) {
          if (this.testDataList[index].FeatureWeights != null) {
          const barchartdata = this.generateBarChartData(this.testDataList[index].FeatureWeights);
          this.testDataList[index]['FeatureWeights'] = barchartdata;
        }
          this.testDataList[index]['ScenarioName'] = data.ScenarioName;

          this.testDataList[index]['Target'] = this.coreUtilsService.isNil(data.Target) ? '' : data.Target;
          this.testDataList[index]['ProblemType'] = this.coreUtilsService.isNil(data.ProblemType) ? '' : data.ProblemType;
          if (this.testDataList[index]['ProblemType'] === 'classification') {
            this.testDataList[index].Probablities.Probab1 = (Math.round(this.testDataList[index].Probablities.Probab1 * 100));
            this.testDataList[index].Probablities.Probab2 = (Math.round(this.testDataList[index].Probablities.Probab2 * 100));
            const data1 = {};
            // Setting Prediction Outcome Chart data
            data1[this.testDataList[index].Prediction] = this.testDataList[index].Probablities.Probab1;
            data1[this.testDataList[index].OthPred] = this.testDataList[index].Probablities.Probab2;

           const barchartdata1 = this.generateBarChartData(data1);

           this.testDataList[index]['OutcomePrediction']  = barchartdata1;
          }
          if (this.testDataList[index]['ProblemType'] === 'regression') {
            this.testDataList[index].Prediction = Math.round(this.testDataList[index].Prediction);
          }
          if (this.testDataList[index]['ProblemType'] === 'Multi_Class') {
            const probablityData = this.setProbabilityData(this.testDataList[index].Probablities);
            this.testDataList[index]['OutcomePrediction']  = probablityData;
            this.testDataList[index].Probablities = probablityData;
          } if (this.testDataList[index]['ProblemType'] === 'Text_Classification') {
            this.isTextPreProcessing = true;
            if (data.ProblemTypeFlag === false) {
              this.isNLPClassification = true;
              this.testDataList[index].Probablities.Probab1 = (Math.round(this.testDataList[index].Probablities.Probab1 * 100));
              this.testDataList[index].Probablities.Probab2 = (Math.round(this.testDataList[index].Probablities.Probab2 * 100));

            } else if (data.ProblemTypeFlag === true) {
              this.isNLPClassification = false;
              const probablityData = this.setProbabilityData(this.testDataList[index].Probablities);
              this.testDataList[index].Probablities = probablityData;

            }
          }
        }
        this.testScenarioAllListNew.push(this.testDataList);
      }
    });
    this.testDataList = [];

    this.testScenarioAllListNew.forEach(allTestSenario => {
      const testdata = allTestSenario.filter(testCase => testCase.ScenarioName === scenarioName);
      if (testdata.length > 0) {
        this.testDataList = testdata;
        this.testDataListCount = this.testDataList.length;

      }
    });
  }

  saveAs($event) {
    const openTemplateAfterClosed = this.dialogService.open(TemplateNameModalComponent, {}).afterClosed.pipe(
      switchMap(modelName => {
        this.newModelName = modelName;
        if (!this.coreUtilsService.isNil(modelName)) {
          return this._problemStatementService.clone(this.correlationId, modelName);
        }
        // tslint:disable-next-line: deprecation
        return empty();
      }),
      tap(data => this.newCorrelationIdAfterCloned = data),
      tap(data => {
        if (data) {
          this._router.navigate(['dashboard/modelengineering/CompareModels'], {
            queryParams: { modelName: this.newModelName, correlationId: data }
          });
          this.notificationService.success('Cloned Successfully');
        }
      })
    );

    openTemplateAfterClosed.subscribe();
  }
  setProbabilityData(probablitiesData) {
    const probablitiesList = [];
    let d = 0;
    Object.entries(probablitiesData).forEach(
      ([key, value]) => {
        const values = [];
        values.push(value);
        const percentVal = values[0];
        probablitiesList.push({
          'featurename': key, 'prcntvalue': Math.round(percentVal * 100), 'color': this.getRandomColor(d++)
        });
      });
    return probablitiesList;
  }
  generateBarChartData(featureWeights) {
    this.chartData = [];
    let d = 0;
    Object.entries(featureWeights).forEach(
      ([key, value]) => {
        this.chartData.push({
          'featurename': key, 'prcntvalue': value, 'color': this.getRandomColor(d++)
        });
      });
    return this.chartData;
  }
  getRandomColor(d) {
      /* const color = Math.floor(0x1000000 * Math.random()).toString(16);
      return '#' + ('000000' + color).slice(-6); */
      /* return 'hsl(' + 360 * Math.random() + ',' +
               (25 + 70 * Math.random()) + '%,' +
               (85 + 10 * Math.random()) + '%)'; */
      if ( d % 2 === 1 ) {
        return '#10ADD3';
      } else {
        return '#286e99';
      }
  }
  onTestScenarioChange(scenarioName: string, divId: number) {
    if (!this.coreUtilsService.isNil(divId) && divId === 0) {
      this.page = 1;
      this.firstDivSelectedScenarioName = scenarioName;
      this.updateTestCaseScenarioOptions(scenarioName, divId);
      this.setAddedTestScenarioList(scenarioName, divId);
    }
    if (!this.coreUtilsService.isNil(divId) && divId === 1) {
      this.page1 = 1;
      this.secondDivSelectedScenarioName = scenarioName;
      this.updateTestCaseScenarioOptions(scenarioName, divId);
      this.setAddedTestScenarioList(scenarioName, divId);
    }
    if (!this.coreUtilsService.isNil(divId) && divId === 2) {
      this.page2 = 1;
      this.thirdDivSelectedScenarioName = scenarioName;
      this.updateTestCaseScenarioOptions(scenarioName, divId);
      this.setAddedTestScenarioList(scenarioName, divId);
    }
  }
  setAddedTestScenarioList(scenarioName: string, divId: number) {
    if (divId === 0) {
      this.testDataList = [];
      this.testScenarioAllListNew.forEach(allTestSenario => {
        const testdata = allTestSenario.filter(testCase => testCase.ScenarioName === scenarioName);
        if (testdata.length > 0) {
          this.testDataList = testdata;
          this.testDataListCount = this.testDataList.length;
        }
      });
    } else if (divId === 1) {
      this.testDataList1 = [];
      this.testScenarioAllListNew.forEach(allTestSenario => {
        const testdata = allTestSenario.filter(testCase => testCase.ScenarioName === scenarioName);
        if (testdata.length > 0) {
          this.testDataList1 = testdata;
          this.testDataListCount1 = this.testDataList1.length;
        }
      });
    } else if (divId === 2) {
      this.testDataList2 = [];
      this.testScenarioAllListNew.forEach(allTestSenario => {
        const testdata = allTestSenario.filter(testCase => testCase.ScenarioName === scenarioName);
        if (testdata.length > 0) {
          this.testDataList2 = testdata;
          this.testDataListCount2 = this.testDataList2.length;
        }
      });
    }
  }

  updateTestCaseScenarioOptions(scenarioName: string, divId: number) {
    if (divId === 0) {
      if (!this.coreUtilsService.isNil(this.thirdDivSelectedScenarioName)) {
        this.testCaseScenarioOptions2 = this.testCaseScenarioOptions.filter(testcase =>
          testcase.scenarioName !== this.firstDivSelectedScenarioName && testcase.scenarioName !== this.secondDivSelectedScenarioName);
          this.testCaseScenarioOptions1 = this.testCaseScenarioOptions.filter(testcase =>
            testcase.scenarioName !== this.firstDivSelectedScenarioName && testcase.scenarioName !== this.thirdDivSelectedScenarioName);
      } else if (!this.coreUtilsService.isNil(this.secondDivSelectedScenarioName)) {
        this.testCaseScenarioOptions1 = this.testCaseScenarioOptions.filter(testcase =>
          testcase.scenarioName !== this.firstDivSelectedScenarioName);
      }
    }
    if (divId === 1) {
      if (!this.coreUtilsService.isNil(this.thirdDivSelectedScenarioName)) {
        this.testCaseScenarioOptions2 = this.testCaseScenarioOptions.filter(testcase =>
          testcase.scenarioName !== this.firstDivSelectedScenarioName && testcase.scenarioName !== this.secondDivSelectedScenarioName);
          this.testCaseScenarioOptions0 = this.testCaseScenarioOptions.filter(testcase =>
            testcase.scenarioName !== this.secondDivSelectedScenarioName && testcase.scenarioName !== this.thirdDivSelectedScenarioName);
      } else if (!this.coreUtilsService.isNil(this.secondDivSelectedScenarioName)) {
        this.testCaseScenarioOptions0 = this.testCaseScenarioOptions.filter(testcase =>
          testcase.scenarioName !== this.secondDivSelectedScenarioName);
      }
    }
    if (divId === 2) {
      if (!this.coreUtilsService.isNil(this.thirdDivSelectedScenarioName)) {
        this.testCaseScenarioOptions1 = this.testCaseScenarioOptions.filter(testcase =>
          testcase.scenarioName !== this.firstDivSelectedScenarioName && testcase.scenarioName !== this.thirdDivSelectedScenarioName);
          this.testCaseScenarioOptions0 = this.testCaseScenarioOptions.filter(testcase =>
            testcase.scenarioName !== this.secondDivSelectedScenarioName && testcase.scenarioName !== this.thirdDivSelectedScenarioName);
      }
    }
  }

  addTestCaseScnerio() {
    if (this.testDataListCount > 0) {
      this.testCaseScenarioOptions1 = this.testCaseScenarioOptions.filter(testcase =>
        testcase.scenarioName !== this.firstDivSelectedScenarioName);
      if (this.testCaseScenarioOptions1.length > 0) {
        this.setAddedTestScenarioList(this.testCaseScenarioOptions1[0].value, 1);
        this.secondDivSelectedScenarioName = this.testCaseScenarioOptions1[0].value;
      }
      this.testCaseScenarioOptions0 = this.testCaseScenarioOptions.filter(testcase =>
        testcase.scenarioName !== this.secondDivSelectedScenarioName);
    }
    if (this.testDataListCount > 0 && this.testDataListCount1 > 0 && this.addTileCount === 1) {
      this.testCaseScenarioOptions2 = this.testCaseScenarioOptions.filter(testcase =>
        testcase.scenarioName !== this.firstDivSelectedScenarioName
        && testcase.scenarioName !== this.secondDivSelectedScenarioName);
      if (this.testCaseScenarioOptions2.length > 0) {
        this.setAddedTestScenarioList(this.testCaseScenarioOptions2[0].value, 2);
        this.thirdDivSelectedScenarioName = this.testCaseScenarioOptions2[0].value;
      }
      this.testCaseScenarioOptions0 = this.testCaseScenarioOptions.filter(testcase =>
        testcase.scenarioName !== this.secondDivSelectedScenarioName
        && testcase.scenarioName !== this.thirdDivSelectedScenarioName);

      if (!this.coreUtilsService.isNil(this.thirdDivSelectedScenarioName)) {
        this.testCaseScenarioOptions1 = this.testCaseScenarioOptions.filter(testcase =>
          testcase.scenarioName !== this.firstDivSelectedScenarioName
          && testcase.scenarioName !== this.thirdDivSelectedScenarioName);
      }
    }
    if (this.addTileCount < 3) {
      this.addTileCount++;
    }
    if (this.addTileCount === 0 && this.testCaseScenarioOptions.length === 0) {
      this.disableTestSecenarioBtn = true;
    } else if (this.addTileCount === 1 && this.testCaseScenarioOptions.length < 3) {
      this.disableTestSecenarioBtn = true;
    } else if (this.addTileCount === 2) {
      this.disableTestSecenarioBtn = true;
    } else {
      this.disableTestSecenarioBtn = false;
    }

  }
  onClose(divId: Number) {
    if (this.addTileCount > 0 && divId === 1) {
      this.testDataList1 = [];
      this.testDataListCount1 = 0;
      this.addTileCount--;
      this.page1 = 1;
      this.testCaseScenarioOptions1.forEach(element => {
          this.testCaseScenarioOptions0.push(element);
      });

      // Remove Duplicates
      this.removeDuplicates(0);
      this.testCaseScenarioOptions1 = [];
      this.secondDivSelectedScenarioName = undefined;
    }
    if (this.addTileCount > 0 && divId === 2) {
      this.testDataList2 = [];
      this.testDataListCount2 = 0;
      this.addTileCount--;
      this.page2 = 1;
      this.testCaseScenarioOptions2.forEach(element => {
        this.testCaseScenarioOptions0.forEach(ele0 => {
          if (element.value !== ele0.value)
          this.testCaseScenarioOptions0.push(element);
        });
        this.testCaseScenarioOptions1.forEach(ele1 => {
          if (element.value !== ele1.value)
          this.testCaseScenarioOptions1.push(element);
        });
      });

      // Remove Duplicates
      this.removeDuplicates(0);
      this.removeDuplicates(1);
      this.testCaseScenarioOptions2 = [];
      this.thirdDivSelectedScenarioName = undefined;
    }
    if (this.addTileCount === 0 && this.testCaseScenarioOptions.length > 0) {
      this.disableTestSecenarioBtn = false;
    } else if (this.addTileCount === 1 && this.testCaseScenarioOptions.length > 2) {
      this.disableTestSecenarioBtn = false;
    } else {
      this.disableTestSecenarioBtn = true;
    }
  }

  removeDuplicates(testCaseScenario: number) {
    if (testCaseScenario === 0) {
      this.testCaseScenarioOptions0 = this.testCaseScenarioOptions0.filter((testcase, index, self) =>
        index === self.findIndex((test) => (
          test.value === testcase.value
        ))
      )
    } else if (testCaseScenario === 1) {
      this.testCaseScenarioOptions1 = this.testCaseScenarioOptions1.filter((testcase, index, self) =>
        index === self.findIndex((test) => (
          test.value === testcase.value
        ))
      )
    } else if (testCaseScenario === 2) {
      this.testCaseScenarioOptions2 = this.testCaseScenarioOptions2.filter((testcase, index, self) =>
        index === self.findIndex((test) => (
          test.value === testcase.value
        ))
      )
    }
  }

  pageChanged(event: any) {

    this.page = event;
  }
  pageChanged1(event: any) {

    this.page1 = event;
  }
  pageChanged2(event: any) {

    this.page2 = event;
  }
  setFilterTestScenarios(data: any) {
    this.filterTestScenarioList = [];
    if (data.divId === 0) {
      this.allTestScenario = [];
      this.filterTestScenarioList = [];
      this.allTestScenario.push(this.testCaseScenarioOptions);

      this.setTestScenarioTimeSeries(data.scenarioName);
    }

  }
  getScenarioList(divId: number) {
    const filterArr = [];
    if (divId === 1) {
      const uniqueArray = _.differenceBy(this.testCaseScenarioOptions, this.filterTestScenarioList, 'scenarioName');
      this.addTimeSeriesCounter++;
      this.testCaseScenarioOptions1 = uniqueArray;
      return uniqueArray;
    }
    if (divId === 2) {
      const uniqueArray = _.differenceBy(this.testCaseScenarioOptions, this.filterTestScenarioList, 'scenarioName');
      this.addTimeSeriesCounter++;
      this.testCaseScenarioOptions2 = uniqueArray;
      return uniqueArray;
    }
    return this.testCaseScenarioOptions;
  }
  setTestScenarioTimeSeries(scenarioName: any) {
    if (!this.coreUtilsService.isNil(this.testCaseScenarioOptions) && this.testCaseScenarioOptions.length > 0) {
      this.filterTestScenarioList.push({ scenarioName: scenarioName, value: scenarioName });
      const firstUniqueArray = _.differenceBy(this.testCaseScenarioOptions, this.filterTestScenarioList, 'scenarioName');
      this.testCaseScenarioOptions1 = firstUniqueArray;
      this.allTestScenario.push(this.testCaseScenarioOptions1);
      if (!this.coreUtilsService.isNil(firstUniqueArray) && firstUniqueArray.length > 0) {
        this.filterTestScenarioList.push({ scenarioName: firstUniqueArray[0].scenarioName, value: firstUniqueArray[0].scenarioName });
        const secondUniqueArray = _.differenceBy(this.testCaseScenarioOptions, this.filterTestScenarioList, 'scenarioName');
        this.testCaseScenarioOptions2 = secondUniqueArray;
        this.allTestScenario.push(this.testCaseScenarioOptions2);
      }

    }
  }

}
