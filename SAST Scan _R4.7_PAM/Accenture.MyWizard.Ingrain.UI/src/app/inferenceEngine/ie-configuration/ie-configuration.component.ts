import { AfterViewInit, Component, OnInit } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { ActivatedRoute, NavigationStart, Router } from '@angular/router';
import { timer } from 'rxjs';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { InfoTextService } from '../info-text/info-text.service';
import { IeConfigurationService } from './ie-configuration.service';
import { DataInflow, MeasureInflow } from './ie-configuration.model';
import { ShowDataComponent } from 'src/app/components/dashboard/data-engineering/preprocess-data/show-data/show-data.component';
import { DialogService } from 'src/app/dialog/dialog.service';

@Component({
  selector: 'app-ie-configuration',
  templateUrl: './ie-configuration.component.html',
  styleUrls: ['./ie-configuration.component.css']
})
export class IeConfigurationComponent implements OnInit {
  isnavBarToggle: boolean;
  ieParams;
  VolumetricConfigInput: DataInflow;
  MetricConfigInput: MeasureInflow;

  payloadForSaveConfigAPI: {
    'CorrelationId': '',
    'ConfigName': '',
    'UserId': string,
    'VolumetricConfigInput': DataInflow,
    'MetricConfigInput': MeasureInflow;
  }

  timerSubscripton: any;
  showSaveCongfigModal: boolean;
  sampleConfig: boolean;

  saveAPI: FormGroup;

  measureIconInfo;
  dateTypeIconInfo;
  frequencylist = ['daily', 'weekly', 'monthly', 'quarterly'];
  savedConfigValues;

  viewConfigDetails;
  previousURL: string;
  isApplyDisabled: boolean;
  nameOfFilterType: any;
  valuesOfFilterType: any;
  filterValues: any;
  isNavBarLabelsToggle = false;

  deselectedMetricConfigInput = [];
  deselectedMetricConfigCount = 0;
  selectedFeatureCombinationForSave = [];
  deselectedFeatureCombinationForSave = [];
  ifSavedDeselectedCombination = false;
  decimalPoint;

  constructor(private dialogService: DialogService, public router: Router, private activatedRoute: ActivatedRoute, private ieConfigService: IeConfigurationService,
    private _appUtilsService: AppUtilsService, private coreUtilsService: CoreUtilsService, private s: InfoTextService, private ns: NotificationService) {
    this.decimalPoint = sessionStorage.getItem('decimalPoint') ? parseInt(sessionStorage.getItem('decimalPoint')) : 2;
    this.measureIconInfo = this.s.measureIconInfo
    this.dateTypeIconInfo = this.s.dateTypeIconInfo
  }

  ngOnInit(): void {
    this.activatedRoute.queryParams
      .subscribe(params => {
        this.ieParams = params;
        // 'correlationId': this.correlationId,
        // 'autoGenerated': true,
        // 'modelName': this.modelName
        // 'InferenceConfigId' : 'id' // for Edit Config
        // 'InferenceConfigName : 'name // for Edit Config
        // 'analysis': single = measure 
        //             multiple = measure and datainflow 

      });

    this.propertyOfClass();
  }
  /* ----------------- Initialize All required class members  ------------------------*/
  propertyOfClass() {
    window.scroll(0, 0);
    this.isApplyDisabled = false;
    this.showSaveCongfigModal = false;
    this.sampleConfig = false;
    this.viewConfigDetails = {
      'DateColumnList': '',
      'DimensionsList': '',
      'MetricColumnList': ''
    }

    const setObject1: Set<string> = new Set();
    const setObject2: Set<string> = new Set();
    const setObject3 = [];
    this.payloadForSaveConfigAPI = {
      'CorrelationId': '',
      'ConfigName': '',
      'UserId': '',
      'VolumetricConfigInput': {
        'DateColumn': '',
        'TrendForecast': '',
        'Frequency': [],
        'Dimensions': setObject1
      },
      'MetricConfigInput': {
        'MetricColumn': '',
        'DateColumn': '',
        'Features': setObject2,
        'FeatureCombinations': setObject3
      }
    }

    this.VolumetricConfigInput = this.payloadForSaveConfigAPI['VolumetricConfigInput'];
    this.MetricConfigInput = this.payloadForSaveConfigAPI['MetricConfigInput'];

    this.payloadForSaveConfigAPI.CorrelationId = this.ieParams.correlationId;
    this.payloadForSaveConfigAPI.UserId = this._appUtilsService.getCookies().UserId;

    this.nameOfFilterType = [];
    this.valuesOfFilterType = {};
    this.filterValues = {};

    if (this.ieParams.hasOwnProperty('InferenceConfigId') && this.ieParams.InferenceConfigId) {
      /****** Edit Existing Config *********/
      this.editExistingConfig();
    } else {
      /****** Edit Existing Config *********/
      this.newlyCreatedConfig();
    }
  }

  /* -----------------Existing Edit Config  ------------------------*/
  editExistingConfig() {
    // api/ViewConfiguration?correlationId=abcdefg&InferenceConfigId=7e9523f0-d3e3-46a3-972a-11111111
    this.ieConfigService.viewConfiguration({ 'correlationId': this.ieParams.correlationId, 'InferenceConfigId': this.ieParams.InferenceConfigId }).subscribe(
      (data) => {
        // console.log(data);
        if (data) {
          this.setListOfDataConfig(data.AllConfigValues, data.SavedConfigValues);
        } else {
          this.gotoModelList();
          this.ns.error('No Data Found');
        }
      },
      (error) => {
        this.gotoModelList();
        this.ns.error(error.error);
      }
    )
  }

  /* ----------------- Newly Creating Config  ------------------------*/
  newlyCreatedConfig() {
    // API GetDateMeasureAttribute
    this.ieConfigService.getDateMeasureAttribute(this.ieParams.correlationId).subscribe(
      (data) => {
        // data = SAMPLE_DATEMESAURE; To Check offline
        //  'DateColumnList ': [ // Date Attribute for Analysis
        //  'DimensionsList ': [  // Correlated Attributes Will Be Utilized To Analyze Data Inflow Trends
        //  'MetricColumnList ': [ // Measure to be analyzed
        if (data) {
          this.setListOfDataConfig(data);
        } else {
          this.gotoModelList();
          this.ns.error('No Data Found');
        }
      },
      (error) => {
        this.gotoModelList();
        this.ns.error(error.error);
      }
    )
  }

  /* ----------------- To Bind the Edit/New Created Data  ------------------------*/
  setListOfDataConfig(allConfigValues, savedConfigValues?) {
    this.selectedFeatureCombinationForSave = allConfigValues.hasOwnProperty('FeatureCombinations') ? allConfigValues['FeatureCombinations'] : [];
    const e = this.viewConfigDetails;
    e['DateColumnList'] = allConfigValues['DateColumnList'];
    e['DimensionsList'] = allConfigValues['DimensionsList'];
    e['MetricColumnList'] = allConfigValues['MetricColumnList'];
    // if ( data.hasOwnProperty('Features') && ( data.hasOwnProperty('FeaturesCombination') )) {
    e['FeaturesList'] = allConfigValues.hasOwnProperty('Features') ? allConfigValues['Features'] : [];
    e['FeaturesCombinationList'] = allConfigValues.hasOwnProperty('FeatureCombinations') ? allConfigValues['FeatureCombinations'] : [];
    // }


    this.VolumetricConfigInput.Dimensions = new Set(e['DimensionsList']);
    this.viewConfigDetails = e;
    if (this.viewConfigDetails.FeaturesCombinationList.length > 0) {
      this.deselectedMetricConfigCount = this.viewConfigDetails.FeaturesCombinationList.length;
    }
    this.VolumetricConfigInput.Frequency = [];
    if (savedConfigValues) {
      this.savedConfigValues = savedConfigValues
      this.prepopulateValues(this.savedConfigValues);
    } else {
      if (allConfigValues.hasOwnProperty('FilterValues')) {
        // const FilterValues = "{\"Status\":{\"Closed\":\"True\",\"Cancelled\":\"False\",\"Resolved\":\"False\"},\"Priority\":{\"Medium\":\"False\",\"High\":\"False\",\"Low\":\"False\"},\"CreatedBy\":{\"integration@Hess\":\"False\",\"archana.w.singh\":\"False\",\"integration@HessIO\":\"False\"},\"ReportedSource\":{\"Email\":\"False\",\"Phone\":\"False\",\"Auto-Generated Event\":\"False\"},\"ConfigurationItem\":{\"Hess Widget\":\"False\",\"hacssginpmp02\":\"False\",\"minsnswcr02_1\":\"False\",\"gdbssdcrwdp01\":\"False\"},\"ConfigurationItemClass\":{\"cmdb_ci_win_server\":\"False\",\"cmdb_ci_server\":\"False\",\"cmdb_ci_ip_switch\":\"False\"},\"ClosureCode\":{\"Solved (Permanently)\":\"False\",\"Solved Remotely (Permanently)\":\"False\",\"Closed/Resolved by Caller\":\"False\"},\"Impact\":{\"Medium\":\"False\",\"Low\":\"False\"},\"Related Incidents\":{\"INC20517383\":\"False\",\"INC21182482\":\"False\"},\"StatusReason\":{\"6\":\"False\",\"7\":\"False\",\"8\":\"False\"},\"Reopen Count\":{\"0\":\"False\"},\"ResolutionType\":{\"Configuration and Process\":\"False\",\"Network\":\"False\"},\"ConfigurationItemStatus\":{\"1.0\":\"False\",\"7.0\":\"False\"},\"Urgency\":{\"Medium\":\"False\",\"Critical\":\"False\",\"Low\":\"False\"},\"ReassignmentCount\":{\"0\":\"False\",\"1\":\"False\"}}"
        this.addFilter(allConfigValues['FilterValues']);
      }
    }
  }

  prepopulateValues(savedConfigValues) {
    // this.savedConfigValues = savedConfigValues
    if (this.ieParams.hasOwnProperty('analysis') && this.ieParams.analysis === 'multiple') {
      const dataInfoSaved = savedConfigValues.filter(m => m.InferenceConfigType === 'VolumetricAnalysis');
      this.VolumetricConfigInput.DateColumn = dataInfoSaved[0].DateColumn;
      this.VolumetricConfigInput.TrendForecast = dataInfoSaved[0].TrendForecast;
      this.VolumetricConfigInput.Frequency = dataInfoSaved[0].Frequency;
      this.VolumetricConfigInput.Dimensions = new Set(dataInfoSaved[0].Dimensions);

      if (this.VolumetricConfigInput.DateColumn === "" &&
        this.VolumetricConfigInput.TrendForecast === "") {
        // this.ieParams.analysis = 'multiple';
      }
    }
    const measureInfoSaved = savedConfigValues.filter(m => m.InferenceConfigType === 'MeasureAnalysis')
    this.MetricConfigInput.DateColumn = measureInfoSaved[0].DateColumn;
    this.MetricConfigInput.MetricColumn = measureInfoSaved[0].MetricColumn;
    this.MetricConfigInput.Features = new Set(measureInfoSaved[0].Features);
    this.MetricConfigInput.FeatureCombinations = measureInfoSaved[0].FeatureCombinations;
    this.deselectedMetricConfigCount = measureInfoSaved[0].FeatureCombinations.length;
    if (measureInfoSaved[0].DeselectedFeatureCombinations.length > 0) {
      this.ifSavedDeselectedCombination = true;
    }

    if (measureInfoSaved[0].hasOwnProperty('FilterValues')) {
      this.addFilter(measureInfoSaved[0]['FilterValues']);
    }
    this.isApplyDisabled = true;
  }

  /* -----------------Navigate back to IE Model list ------------------------*/
  gotoModelList() {
    if (this.ieConfigService.params) {
      this.router.navigate(['inferenceEngine'],
        {
          queryParams: this.ieConfigService.params
        });
    }
  }

  /* ----------------- Select box from feature of date inflow ------------------------*/
  onSelectedAttributeDateMeasure(columnName, event) {
    //  console.log(event);
    if (event.target.checked) {
      this.VolumetricConfigInput.Dimensions.add(columnName);
    } else {
      this.VolumetricConfigInput.Dimensions.delete(columnName);
    }
    // console.log(this.VolumetricConfigInput)
  }

  /* ----------------- Selected Box from Features of measure  ------------------------*/
  onSelectedAttributeMeasureFeature(feature, event) {
    if (event.target.checked) {
      this.MetricConfigInput.Features.add(feature);
    } else {
      this.MetricConfigInput.Features.delete(feature);
    }
  }

  /* ----------------- Selected Box from Feature Combinations of measure  ------------------------*/
  onSelectedAttributeMeasureFeatureCombinations(featureName, event) {
    let featureTodelete = featureName;
    let tempDeselectedFeature = this.deselectedMetricConfigInput;

    this.deselectedMetricConfigInput.push(featureName);

    let key; let listofFeatureCombination = [];
    for (var i = 0; i < this.deselectedMetricConfigInput.length; i++) {
      for (key in this.deselectedMetricConfigInput[i]) {
        if (key == "FeatureName") {
          listofFeatureCombination.push(this.deselectedMetricConfigInput[i][key]);
        }
      }
    }

    let hasDuplicate = listofFeatureCombination.some((val, i) => listofFeatureCombination.indexOf(val) !== i);

    if (hasDuplicate) {
      // let removeIndex = tempDeselectedFeature.map((item) => item['FeatureName']).indexOf(featureTodelete.FeatureName);
      // tempDeselectedFeature.splice(removeIndex, 1);
      // this.deselectedMetricConfigInput = tempDeselectedFeature;

      tempDeselectedFeature.forEach((element) => {
        if (listofFeatureCombination.includes(featureTodelete.FeatureName)) {
          let removeIndex = tempDeselectedFeature.map((item) => item['FeatureName']).indexOf(featureTodelete.FeatureName);
          if (removeIndex > -1) {
            tempDeselectedFeature.splice(removeIndex, 1);
            this.deselectedMetricConfigInput = tempDeselectedFeature;
          }
        }
      })
    }


    if (event.target.checked) {
      let hasMetDuplicate = this.MetricConfigInput.FeatureCombinations.some((val, i) => this.MetricConfigInput.FeatureCombinations.indexOf(val.FeatureName) !== i);
      if (!hasMetDuplicate) {
        this.MetricConfigInput.FeatureCombinations.push(featureName);
      }
    } else {
      const indexToBeDeleted = this.checkFC(featureName);
      if (indexToBeDeleted > -1) {
        this.MetricConfigInput.FeatureCombinations.splice(indexToBeDeleted, 1);
      }
    }

    const indexVal = this.viewConfigDetails.FeaturesCombinationList.findIndex(element => (element.FeatureName === featureName.FeatureName));
    if (indexVal > -1) { } else {
      this.viewConfigDetails.FeaturesCombinationList.push(featureTodelete);
    }

    if (this.ifSavedDeselectedCombination) {
      if (this.viewConfigDetails.FeaturesCombinationList.length !== this.MetricConfigInput.FeatureCombinations.length) {
        if (this.MetricConfigInput.FeatureCombinations.length >= 0 && this.deselectedMetricConfigInput.length === 1) {
          if (this.deselectedMetricConfigCount !== 0) {
            this.deselectedMetricConfigInput = [];
          }
          this.deselectedMetricConfigCount = this.MetricConfigInput.FeatureCombinations.length;
        }
      } else {
        this.deselectedMetricConfigCount = this.MetricConfigInput.FeatureCombinations.length - this.deselectedMetricConfigInput.length;
      }

      if (this.deselectedMetricConfigInput.length > 0 && this.MetricConfigInput.FeatureCombinations.length === 1) {
        this.deselectedMetricConfigCount = this.deselectedMetricConfigInput.length;
        if (this.deselectedMetricConfigCount === this.viewConfigDetails.FeaturesCombinationList.length) {
          this.MetricConfigInput.FeatureCombinations = this.viewConfigDetails.FeaturesCombinationList;
          //this.ifSavedDeselectedCombination = false;
        }
      }
    } else {
      if (this.viewConfigDetails.FeaturesCombinationList.length === this.MetricConfigInput.FeatureCombinations.length) {
        if (this.viewConfigDetails.FeaturesCombinationList.length >= this.deselectedMetricConfigInput.length) {
          if ((this.deselectedMetricConfigCount === this.viewConfigDetails.FeaturesCombinationList.length - 1) && (this.deselectedMetricConfigInput.length === 1)) {
            this.deselectedMetricConfigCount = this.viewConfigDetails.FeaturesCombinationList.length;
          } else {
            this.deselectedMetricConfigCount = this.viewConfigDetails.FeaturesCombinationList.length - this.deselectedMetricConfigInput.length;
          }
          if (this.deselectedMetricConfigCount === this.viewConfigDetails.FeaturesCombinationList.length) {
            this.MetricConfigInput.FeatureCombinations = this.viewConfigDetails.FeaturesCombinationList;
          }
        }
      } else if (this.deselectedMetricConfigInput.length > 0 && this.MetricConfigInput.FeatureCombinations.length === 1) {
        this.deselectedMetricConfigCount = this.deselectedMetricConfigInput.length;
        if (this.deselectedMetricConfigCount === this.viewConfigDetails.FeaturesCombinationList.length) {
          this.MetricConfigInput.FeatureCombinations = this.viewConfigDetails.FeaturesCombinationList;
        }
      }

      if (this.viewConfigDetails.FeaturesCombinationList.length !== this.MetricConfigInput.FeatureCombinations.length) {
        // if (this.MetricConfigInput.FeatureCombinations.length >= 0 && this.deselectedMetricConfigInput.length === 1) {
        // if (this.deselectedMetricConfigCount !== 0) {
        //   this.deselectedMetricConfigInput = [];
        // }
        this.deselectedMetricConfigCount = this.MetricConfigInput.FeatureCombinations.length;
        // } else 
        if (this.deselectedMetricConfigInput.length > 0 && this.MetricConfigInput.FeatureCombinations.length === 1) {
          this.deselectedMetricConfigCount = this.deselectedMetricConfigInput.length;
        }
      } else {
        this.deselectedMetricConfigCount = this.MetricConfigInput.FeatureCombinations.length - this.deselectedMetricConfigInput.length;
      }
    }
  }

  checkFC(featureName) {
    return this.MetricConfigInput.FeatureCombinations.findIndex(element => (element.FeatureName === featureName.FeatureName));
  }

  /* -----------------Show Save Config Name Modal  ------------------------*/
  showSaveConfigNameModalFunction() {
    const metricCheck = this.metricLevelValidation();
    if (this.metricLevelValidation() && this.dataInfoLevelValidation()) {
      if (this.ieParams.hasOwnProperty('InferenceConfigId') && this.ieParams.InferenceConfigId) {
        this.createConfig(this.ieParams['InferenceConfigName']);
      } else {
        this.showSaveCongfigModal = true
      }
    } else {
      let message = "";
      if (this.ieParams.analysis === 'multiple') {
        if (this.VolumetricConfigInput.DateColumn === "" || this.VolumetricConfigInput.TrendForecast === "" ||
          this.VolumetricConfigInput.Dimensions.size === 0) {
          message = "Please select mandatory fields in Data Inflow Analysis";
        } else if (this.VolumetricConfigInput.TrendForecast === 'yes' && this.VolumetricConfigInput.Frequency.length === 0) {
          message = "Please select mandatory fields in Data Inflow Analysis"
        }
        if (message) {
          this.ns.error(message);
        }
      }

      let message1 = "";
      if (this.metricLevelValidation() === false && (this.MetricConfigInput.MetricColumn === "")) {
        message1 = 'Please select mandatory fields in Measure Analysis ';
      } else if (this.MetricConfigInput.Features.size === 0 && this.viewConfigDetails.FeaturesList.length === 0) {
        message1 = 'Please click on Apply to generate important correlated attributes & correlated attribute combinations.';
      } else if (this.MetricConfigInput.Features.size === 0 && this.viewConfigDetails.FeaturesList.length > 0) {
        message1 = 'Please select mandatory fields in Measure Analysis';
      }

      if (message == "" && message1) {
        this.ns.error(message1);
      }
    }
  }

  /* ----------------- Data Info Selected or Not  ------------------------*/
  dataInfoLevelValidation(): boolean {
    if (this.ieParams.hasOwnProperty('analysis') && this.ieParams.analysis === 'single') {
      return true;
    }
    let allValuesPresentInDataInflow = false;
    if (this.VolumetricConfigInput.DateColumn !== "") {
      if (this.VolumetricConfigInput.TrendForecast !== "") {
        if (this.VolumetricConfigInput.TrendForecast === 'yes' && this.VolumetricConfigInput.Frequency.length > 0 && this.VolumetricConfigInput.Dimensions.size > 0) {
          allValuesPresentInDataInflow = true;
        } else if (this.VolumetricConfigInput.TrendForecast === 'no' && this.VolumetricConfigInput.Dimensions.size > 0) {
          allValuesPresentInDataInflow = true;
        }
      }
    }
    return allValuesPresentInDataInflow;
  }


  /* ----------------- Metric Selected or Not  ------------------------*/
  metricLevelValidation(): boolean {
    let allValuesPresentInMetric = false;
    if (this.MetricConfigInput.MetricColumn !== "") {
      if (this.MetricConfigInput.Features.size > 0) {
        allValuesPresentInMetric = true;
      }
    }
    return allValuesPresentInMetric;
  }

  /* ----------------- Create Request Payload & Call   ------------------------*/
  createConfig(name) {
    if (this.coreUtilsService.isSpecialCharacter(name) === 0) {
      return 0;
    }

    if (name === "") {
      this.ns.error('Enter Config name');
      return 0;
    }

    let key; let listofFeatureCombination = [];
    for (var i = 0; i < this.deselectedMetricConfigInput.length; i++) {
      for (key in this.deselectedMetricConfigInput[i]) {
        if (key == "FeatureName") {
          // console.log(key + "->" + this.deselectedMetricConfigInput[i][key]);
          listofFeatureCombination.push(this.deselectedMetricConfigInput[i][key]);
        }
      }
    }

    let tempFeatureComb = this.selectedFeatureCombinationForSave.slice(0);
    tempFeatureComb.forEach((element) => {
      if (listofFeatureCombination.includes(element['FeatureName'])) {
        let removeIndex = this.selectedFeatureCombinationForSave.map((item) => item['FeatureName']).indexOf(element['FeatureName']);
        this.selectedFeatureCombinationForSave.splice(removeIndex, 1);
      }
    });
    // console.log('test Name----', this.selectedFeatureCombinationForSave, tempFeatureComb);

    this.showSaveCongfigModal = false;
    // this.postSaveConfig();
    this.payloadForSaveConfigAPI.ConfigName = name;
    let payloadNewReference = JSON.parse(JSON.stringify(this.payloadForSaveConfigAPI));
    delete payloadNewReference['VolumetricConfigInput']['Dimensions'];
    delete payloadNewReference['MetricConfigInput']['Features'];
    delete payloadNewReference['MetricConfigInput']['FeatureCombinations'];
    payloadNewReference['VolumetricConfigInput']['Dimensions'] = Array.from(this.payloadForSaveConfigAPI.VolumetricConfigInput.Dimensions);
    payloadNewReference['MetricConfigInput']['Features'] = Array.from(this.payloadForSaveConfigAPI.MetricConfigInput.Features);
    if (this.selectedFeatureCombinationForSave.length !== 0) {
      payloadNewReference['MetricConfigInput']['FeatureCombinations'] = this.selectedFeatureCombinationForSave;
    } else {
      payloadNewReference['MetricConfigInput']['FeatureCombinations'] = Array.from(this.payloadForSaveConfigAPI.MetricConfigInput.FeatureCombinations);
    }
    payloadNewReference['InferenceConfigId'] = (this.ieParams.InferenceConfigId) ? this.ieParams.InferenceConfigId : '';


    payloadNewReference['VolumetricConfigInput']['DeselectedDimensions'] = this.coreUtilsService.compareTwoObjectDifference(this.viewConfigDetails.DimensionsList,
      payloadNewReference['VolumetricConfigInput']['Dimensions']);
    payloadNewReference['MetricConfigInput']['DeselectedFeatures'] = this.coreUtilsService.compareTwoObjectDifference(this.viewConfigDetails.FeaturesList,
      payloadNewReference['MetricConfigInput']['Features']);
    // payloadNewReference['MetricConfigInput']['DeselectedFeatureCombinations'] = this.coreUtilsService.compareTwoObjectDifference(this.viewConfigDetails.FeaturesCombinationList,
    payloadNewReference['MetricConfigInput']['DeselectedFeatureCombinations'] = this.deselectedMetricConfigInput;

    payloadNewReference['MetricConfigInput']['FilterValues'] = this.filterValues;

    if (this.ieParams.hasOwnProperty('analysis') && this.ieParams.analysis === 'single') {
      delete payloadNewReference['VolumetricConfigInput'];
    }
    this.postSaveConfig(payloadNewReference);
    // console.log(payloadNewReference);
  }

  /* -----------------SaveConfig Call  ------------------------*/
  postSaveConfig(requestPayload) {
    this._appUtilsService.loadingStarted();
    this.ieConfigService.postSaveConfig(requestPayload, null).subscribe(
      (data) => {
        if (data.Status == 'P') {
          this.retrypostSaveConfig(requestPayload);
        }
        if (data.Status == 'C') {
          this._appUtilsService.loadingImmediateEnded();
          this.ns.success((this.ieParams.InferenceConfigId) ? 'Configuration saved successfully' : 'Configuration created successfully');
          this.gotoModelList();
        }
        if (data.Status == 'E') {
          this._appUtilsService.loadingImmediateEnded();
          this.ns.error(data['Message']);
        }

      },
      (error) => {
        this._appUtilsService.loadingImmediateEnded();
        this.ns.error(error.error)
      }
    )
  }

  /* -----------------Retry SaveConfigAPI   ------------------------*/
  retrypostSaveConfig(requestPayload) {
    this.timerSubscripton = timer(5000).subscribe(() => this.postSaveConfig(requestPayload));
    return this.timerSubscripton;
  }

  /* -----------------TriggerFeatureCombination && GetFeatureCombination Call  ------------------------*/
  getAttributes() {
    // TriggerFeatureCombination
    // POST ::api/TriggerFeatureCombination?correlationId=correlationidNew3&userId=mywizardsystemdataadmin@mwphoenix.onmicrosoft.com
    // GET::api/GetFeatureCombination?correlationId=correlationidNew3&metricColumn=SLA Resolution&dateColumn=Creation Date Time

    if (this.MetricConfigInput.MetricColumn === "") {
      this.ns.warning('Please select value')
    } else {
      this._appUtilsService.loadingStarted();
      this.ieConfigService.postTriggerFeatureCombination({
        'Metric': this.MetricConfigInput.MetricColumn, 'date': this.MetricConfigInput.DateColumn
        , 'FilterValues': this.filterValues
      }
        , { 'correlationId': this.payloadForSaveConfigAPI.CorrelationId, 'userId': this.payloadForSaveConfigAPI.UserId, 'inferenceConfigId': this.ieParams.InferenceConfigId }
      ).subscribe(
        (data) => {
          if (data) {
            if (data.Status === 'P' || data.Status === 'N' || data.Status === 'O') { // "Status": "C", //N, O, P,C 
              this.retryAttributes();
            }
            if (data.Status === 'C') {

              this.ieConfigService.getGetFeatureCombination({
                'correlationId': this.payloadForSaveConfigAPI.CorrelationId, 'requestId': data['RequestId']
              }).subscribe(
                (data) => {
                  if (data) {
                    this.viewConfigDetails['FeaturesList'] = data.Features;
                    this.MetricConfigInput.Features = new Set(data.Features);
                    const c = data.FeatureCombinations;
                    let v = [];
                    for (const index1 in c) {
                      if (c[index1].ConnectedFeatures) {
                        v.push(c[index1].ConnectedFeatures);
                      }
                    }
                    this.ns.success('Correlated Attributes and Correlated Attribute combinations generated successfully for Measure Analysis');
                    this.isApplyDisabled = true;
                    this.viewConfigDetails['FeaturesCombinationList'] = JSON.parse(JSON.stringify(c));
                    this.MetricConfigInput.FeatureCombinations = JSON.parse(JSON.stringify(c));
                    this.deselectedMetricConfigCount = this.viewConfigDetails['FeaturesCombinationList'].length;
                    this._appUtilsService.loadingImmediateEnded();
                  }
                },
                (error) => {
                  this.ns.error(error.error);
                  this._appUtilsService.loadingImmediateEnded();
                }
              )
            }

            if (data.Status === 'E') {
              this.ns.error(data.Message);
              this._appUtilsService.loadingImmediateEnded();
            }
          } else {
            this._appUtilsService.loadingImmediateEnded();
          }
        },
        (error) => {
          this.ns.error(error.error);
          this._appUtilsService.loadingImmediateEnded();
        }
      )
    }
  }

  /* -------------------   Retry TriggerFeatureCombination Call  ------------------------*/
  retryAttributes() {
    this.timerSubscripton = timer(2000).subscribe(() => this.getAttributes());
    return this.timerSubscripton;
  }

  /* -------------------   Unsubscribe Retry Call  ------------------------*/
  unsubscribe() {
    if (!this.coreUtilsService.isNil(this.timerSubscripton)) {
      this.timerSubscripton.unsubscribe();
    }
  }

  /* ------------------- Header Related actions ------------------------*/
  previous() {
    this.router.navigate(['choosefocusarea']);
  }

  toggleNavBar() {
    this.isnavBarToggle = !this.isnavBarToggle;
  }


  performAction() {
    if (this.VolumetricConfigInput.Dimensions.size == this.viewConfigDetails.DimensionsList.length) {
      this.VolumetricConfigInput.Dimensions = new Set([]);
    } else {
      this.VolumetricConfigInput.Dimensions = new Set(this.viewConfigDetails.DimensionsList)
    }
  }

  performAction1() {
    if (this.MetricConfigInput.Features.size == this.viewConfigDetails.FeaturesList.length) {
      this.MetricConfigInput.Features = new Set([]);
    } else {
      this.MetricConfigInput.Features = new Set(this.viewConfigDetails.FeaturesList)
    }
  }

  performAction2() {
    this.deselectedMetricConfigInput = [];
    if (this.ifSavedDeselectedCombination) {
      // this.deselectedMetricConfigInput = [];
      // if (this.deselectedMetricConfigCount === this.viewConfigDetails.FeaturesCombinationList.length) {
      if (this.MetricConfigInput.FeatureCombinations.length == this.viewConfigDetails.FeaturesCombinationList.length) {
        this.MetricConfigInput.FeatureCombinations = [];
        this.deselectedMetricConfigCount = this.deselectedMetricConfigInput.length;
        // }
      } else {
        // this.MetricConfigInput.FeatureCombinations = [];
        this.MetricConfigInput.FeatureCombinations = this.viewConfigDetails.FeaturesCombinationList;
        this.deselectedMetricConfigCount = this.viewConfigDetails.FeaturesCombinationList.length;
      }
    } else {
      // this.deselectedMetricConfigInput = [];
      // if (this.deselectedMetricConfigCount === this.viewConfigDetails.FeaturesCombinationList.length) {
      if (this.MetricConfigInput.FeatureCombinations.length == this.viewConfigDetails.FeaturesCombinationList.length) {
        this.MetricConfigInput.FeatureCombinations = [];
        this.deselectedMetricConfigCount = this.deselectedMetricConfigInput.length;
        // }
      } else {
        // this.MetricConfigInput.FeatureCombinations = [];
        this.MetricConfigInput.FeatureCombinations = this.viewConfigDetails.FeaturesCombinationList;
        this.deselectedMetricConfigCount = this.viewConfigDetails.FeaturesCombinationList.length;
      }
    }
  }

  changeMeasure(s4) {
    this.MetricConfigInput['MetricColumn'] = s4.value;
    this.MetricConfigInput.Features = new Set([]);
    this.MetricConfigInput.FeatureCombinations = [];
    this.viewConfigDetails.FeaturesList = [];
    this.viewConfigDetails.FeaturesCombinationList = [];
    this.isApplyDisabled = false;
  }

  changeDateMeasure(s5) {
    this.MetricConfigInput['DateColumn'] = s5.value;
    this.MetricConfigInput.Features = new Set([]);
    this.MetricConfigInput.FeatureCombinations = [];
    this.viewConfigDetails.FeaturesList = [];
    this.viewConfigDetails.FeaturesCombinationList = [];
    this.isApplyDisabled = false;
  }

  addFilter(data) {
    const parsedData = JSON.parse(data);
    this.filterValues = parsedData;
    for (const i in parsedData) {
      if (parsedData.hasOwnProperty(i)) {
        this.nameOfFilterType.push(i);
        for (const j in parsedData[i]) {

          if (parsedData[i].hasOwnProperty(j)) {
            if (parsedData[i][j] === '') {
              delete parsedData[i][j];
            }
            if (j === 'ChangeRequest' || j === 'PChangeRequest') {
              delete parsedData[i][j];
            }
          }

        }
        this.valuesOfFilterType[i] = Object.keys(parsedData[i]);
        const x = this.getFilteredDataByValue(parsedData[i], 'True');
      }

    }
  }

  // Will return a array by filtering 'object' with 'value'.
  getFilteredDataByValue(object: any, value: string) {
    const copyObject = object;
    if (!copyObject) {
      return '';
    }

    delete copyObject.ChangeRequest;
    delete copyObject.PChangeRequest;
    const result = Object.keys(copyObject).filter(key => copyObject[key] === value);
    return result;
  }

  setnameOfFilter(data) {
    //  console.log('Name of Filter',data);
  }

  setValueOfFilter(value) {
    // console.log('Value of Filter',value);
    const filterName = value.parent;
    const filtersSelected: Array<string> = value.child;
    // console.log(this.filterValues[filterName]);
    this.makeFilterValuesFalse(filterName);
    if (filtersSelected.length > 0) {
      for (const value in this.filterValues[filterName]) {
        if (filtersSelected.filter(e => e === value).length) {
          this.filterValues[filterName][value] = 'True';
        } else {
          this.filterValues[filterName][value] = 'False';
        }
      }
    }
    // console.log(this.filterValues);
    this.isApplyDisabled = false;
  }

  makeFilterValuesFalse(filterName) {
    for (const value in this.filterValues[filterName]) {
      this.filterValues[filterName][value] = 'False';
    }
  }


  showData() {
    this._appUtilsService.loadingStarted();
    this.ieConfigService.viewIEData({ 'correlationId': this.ieParams.correlationId, 'DecimalPlaces': this.decimalPoint }).subscribe(data => {
      if (data.length) {
        const tableData = data;
        const showDataColumnsList = Object.keys(data[0]);
        this._appUtilsService.loadingEnded();
        this.dialogService.open(ShowDataComponent, {
          data: {
            'tableData': tableData,
            'columnList': showDataColumnsList,
            // 'problemTypeFlag': problemTypeFlag
          }
        });
      } else {
        this.ns.error('No data found');
        this._appUtilsService.loadingEnded();
      }
    }, error => {
      this._appUtilsService.loadingEnded();
      this.ns.error(error.error);
    });
  }

  viewSampleConfig() {
    this.sampleConfig = true;
  }

  toggleNavBarLabels() {
    this.isNavBarLabelsToggle = !this.isNavBarLabelsToggle;
    return false;
  }

  remainingDataAttribute(value) {
    if (value.length === 0) {
      this.isApplyDisabled = true;
    } else {
      this.isApplyDisabled = false;
    }
  }

  removeDataAttribute(value) {
    const filterName = value.parent;
    const filtersSelected: Array<string> = value.child;
    // console.log(this.filterValues[filterName]);
    this.makeFilterValuesFalse(filterName);
  }
}
