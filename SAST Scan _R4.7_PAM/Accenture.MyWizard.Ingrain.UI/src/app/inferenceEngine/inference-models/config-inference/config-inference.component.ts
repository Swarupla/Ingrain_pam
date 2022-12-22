import { Component, Input, OnInit, EventEmitter, Output, AfterViewInit } from '@angular/core';
import { Router } from '@angular/router';
import { timer } from 'rxjs';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { IeConfigurationService } from '../../ie-configuration/ie-configuration.service';
import { InfoTextService } from '../../info-text/info-text.service';
import { InferenceModelsService } from '../inference-models.service';
import { ApiService } from 'src/app/_services/api.service';

export class PublishInference {
    ApplicationId?: string = '';
    CorrelationId?: string = '';
    InferenceConfigId?: string = '';
    InferenceConfigType?: string = '';
    InferenceConfigSubTypes?: Array<string> = [];
    CreatedBy?: string = ''
    constructor(values = {}) {
        Object.keys(this).forEach(key => {
            if (values && values.hasOwnProperty(key))
                this[key] = values[key];
        });
    }
}


@Component({
    selector: 'app-config-inference',
    templateUrl: './config-inference.component.html',
    styleUrls: ['./config-inference.component.css']
})
export class ConfigInferenceComponent implements OnInit, AfterViewInit {

    @Input() modelCreatedwith;
    @Input() manualConfigGeneratedIE;
    @Input() publishInferencePayload: PublishInference[];;
    @Output() refreshIE = new EventEmitter<any>();
    measureActive;
    measureIconInfo;
    dateTypeIconInfo;
    correlationId;
    InferenceConfigTypeForDataInflow = 'VolumetricAnalysis';
    InferenceConfigTypeForMeasureflow = "MeasureAnalysis"

    configDetail = 'InferenceConfigDetails';
    configResutls = 'InferenceResults';
    outputInflowDataToBeShown;
    outputInflowMeasureToBeShown;
    createdConfigName;
    selectedConfigId;
    firstConfigId;
    dataInfoSelectedForConfig;
    modelName: any;
    configIdGenerateInferences;
    timerSubscription;
    publishModalShow;
    redirectionModal;
    applicationList;
    applicationIdSelected;

    measureaccordian = {
        'acc0': false,
        'acc1': false,
        'expandAll': false
    }

    dataInflowaccordian = {
        'accOutput0': false,
        'accOutput1': false,
        'accOutput2': false,
        'expandAll': false
    }
    isModelCreatedUsingEntity: boolean;
    environment;
    isProcessDisabled: boolean = false;

    constructor(private service: InfoTextService, private inferenceModelService: InferenceModelsService,
        private notificationService: NotificationService, private router: Router, private _appUtilsService: AppUtilsService,
        private coreUtilsService: CoreUtilsService, private ieConfigService: IeConfigurationService, private _apiService: ApiService) {
        this.measureIconInfo = this.service.measureIconInfo;
        this.dateTypeIconInfo = this.service.dateTypeIconInfo;
    }

    ngOnInit(): void {
        this.environment = sessionStorage.getItem('Environment');
        if (this.modelCreatedwith[0].Entity !== null) {
            this.isModelCreatedUsingEntity = false;
        } else {
            this.isModelCreatedUsingEntity = true;
        }
        this.setConfigVariables();
        this.manualConfigGeneratedIE.forEach(result => {
            this._appUtilsService.loadingStarted();
            let s = {
                'InferenceName': result.InferenceName, 'InferenceConfigId': result.InferenceConfigId, 'DeleteRequired': true,
                'InferenceConfigType': result.InferenceConfigType
            };

            if (result.InferenceConfigType === this.InferenceConfigTypeForDataInflow) {

                const v = result[this.configDetail]; // ConfigDetails means Input block
                if (v.DateColumn != "") {
                    s.DeleteRequired = false;
                    this.dataInfoSelectedForConfig = v.DateColumn;
                    this.selectedConfigId = result.InferenceConfigId;
                    this.firstConfigId = result.InferenceConfigId;
                }
                if (result[this.configResutls].length) {
                    const w = result[this.configResutls]; // ConfigResult means Accordian view
                    const f = this.outputInflowDataToBeShown
                    // f['VolumetricInferences'] = w[0].VolumetricInferences;
                    f.push({ 'VolumetricInferences': w[0]?.VolumetricInferences, 'InferenceConfigId': result.InferenceConfigId, 'InferenceName': result.InferenceName });
                }

            }

            if (result.InferenceConfigType === this.InferenceConfigTypeForMeasureflow) {
                const v = result[this.configDetail]; // ConfigDetails means Input block
                if (result[this.configResutls].length) {
                    const w = result[this.configResutls]; // ConfigResult means Accordian view
                    const f = this.outputInflowMeasureToBeShown
                    f.push({ 'MeasureAnalysisInferences': w[0].MeasureAnalysisInferences, 'InferenceConfigId': result.InferenceConfigId, 'InferenceName': result.InferenceName, });
                    // f['MeasureAnalysisInferences'] = w[0].MeasureAnalysisInferences;
                    // f['InferenceConfigId'] = result.InferenceConfigId;
                }
                if (this.firstConfigId === result.InferenceConfigId) {
                    s.DeleteRequired = false;
                }
            }

            this.createdConfigName.push(s);
            this._appUtilsService.loadingEnded();
        })
        const removeFirstConfig = this.createdConfigName.splice(this.createdConfigName.findIndex(
            x => x.InferenceConfigId === this.firstConfigId && x.InferenceConfigType === this.InferenceConfigTypeForMeasureflow)
            , 1);
        this.createdConfigName = [...removeFirstConfig, ...this.createdConfigName];
    }

    ngAfterViewInit() {

        const className = 'example-element-row' + this.manualConfigGeneratedIE[0].CorrelationId;
        document.getElementsByClassName(className)[0].scrollIntoView();
    }

    /* ------------------- Initialize Property actions ------------------------*/
    setConfigVariables() {
        this.outputInflowDataToBeShown = []
        this.outputInflowMeasureToBeShown = []
        this.measureActive = true;
        this.correlationId = this.manualConfigGeneratedIE[0].CorrelationId;
        this.modelName = this.manualConfigGeneratedIE[0].ModelName;
        this.createdConfigName = [];
        this.dataInfoSelectedForConfig = '';
        this.configIdGenerateInferences = [];
        this.selectedConfigId = '';
        this.firstConfigId = '';
        this.publishModalShow = false;
        this.applicationList = [];
        this.applicationIdSelected = '';
        this.publishInferencePayload = [];
        this.redirectionModal = false;
        // this.onApplicationName(this.ieConfigService.application.ApplicationID);
    }


    /* ------------------- Dropdown actions ------------------------*/
    deleteIEConfig(configId) {
        // DeleteIEModel?correlationId=26a82223-3372-4ab3-80a0-c1a286da9401 
        this.inferenceModelService.deleteIEConfig(configId).subscribe(
            (data) => {
                // "Status":"C",
                if (data['Status'] === 'C') {
                    this.notificationService.success('Deleted succesfully.')
                    this.refreshModelList();
                }
            },
            (error) => {
                this.notificationService.error(error.error)
            }
        )
    }

    editIEConfig(configId, name) {
        this.router.navigate(['ieConfigurationDetails'],
            {
                queryParams: {
                    'correlationId': this.correlationId,
                    'autoGenerated': true,
                    'modelName': this.modelName,
                    'InferenceConfigId': configId,
                    'InferenceConfigName': name,
                    'analysis': (configId === this.firstConfigId) ? 'multiple' : 'single'
                }
            });
    }

    /* -----------------Navigate back to IE Model list ------------------------*/
    refreshModelList() {
        this.refreshIE.emit('Refresh');
    }

    /*---------------- Button actions *********************/
    configurationView() {
        this.router.navigate(['ieConfigurationDetails'],
            {
                queryParams: {
                    'correlationId': this.correlationId,
                    'autoGenerated': true,
                    'modelName': this.modelName,
                    'analysis': 'single'
                }
            });
    }

    /*---------------Generate Inference Button Clicked Actions- ------------*/
    initiateGenerateInference() {
        // correlationId=8257c968-56ef-4eb3-bec9-5925ef2c6d06&inferenceConfigId=535d1cc2-0c37-4038-8bad-1f684be0311c&userId=mywizardsystemdataadmin@mwphoenix.onmicrosoft.com
        const queryParams = {
            'correlationId': this.correlationId, 'inferenceConfigId': '',
            'userId': this._appUtilsService.getCookies().UserId, 'isNewRequest': true
        };
        this.configIdGenerateInferences.forEach(
            (configId, index) => {
                queryParams['inferenceConfigId'] = configId;
                this.generateInferencesOneByOne(queryParams, (index === this.configIdGenerateInferences.length - 1));
            }
        )
    }

    /*---------------Generate Inference API - ------------*/
    generateInferencesOneByOne(queryParams, isLast) {
        // isNewRequest
        this._appUtilsService.loadingStarted();
        this.inferenceModelService.generateInferences(queryParams).subscribe(
            (data) => {
                if (data && data['Status'] !== 'C' && data['Status'] !== 'E') {
                    queryParams['isNewRequest'] = false;
                    this.retryGenerateInference(queryParams, isLast, 'onebyone');
                }

                if (data && data['Status'] === 'C') {
                    if (isLast) {
                        this.checkProgressOfGeneratedIE();
                        // this._appUtilsService.loadingImmediateEnded();
                        // this.notificationService.success('Inference Generated Successfully');
                        // this.refreshModelList();
                    }
                }

                if (data && data['Status'] === 'E') {
                    if (isLast) {
                        this._appUtilsService.loadingImmediateEnded();
                        this.notificationService.error(data['Message']);
                        this.unsubscribe();
                    }
                }

            },
            (error) => {
                this.notificationService.error(error.error)
            }
        )
    }


    /* -----------------Retry Generate Inference   ------------------------*/
    retryGenerateInference(requestPayload, isLast, source) {
        if (source === 'onebyone') {
            this.timerSubscription = timer(6000).subscribe(() => this.generateInferencesOneByOne(requestPayload, isLast));
        } else if (source === 'all') {
            this.timerSubscription = timer(6000).subscribe(() => this.checkProgressOfGeneratedIE());
        }
        return this.timerSubscription;
    }

    /* -------------------   Unsubscribe Retry Call  ------------------------*/
    unsubscribe() {
        if (!this.coreUtilsService.isNil(this.timerSubscription)) {
            this.timerSubscription.unsubscribe();
        }
    }

    checkProgressOfGeneratedIE() {
        let manualConfigInProgress = [];
        let manualConfigCompleted = [];
        const queryParams = {
            'correlationId': this.correlationId, 'inferenceConfigId': '',
            'userId': this._appUtilsService.getCookies().UserId, 'isNewRequest': true
        };
        this.inferenceModelService.getModelInferences(this.correlationId).subscribe(
            (data) => {
                if (data && data.length > 0) {
                    data.forEach(entry => {
                        if (entry.InferenceSourceType === 'Manual' && entry.Status === 'P') {
                            manualConfigInProgress.push(entry.InferenceConfigId); // Development inprogress
                        }
                        if (entry.InferenceSourceType === 'Manual' && entry.Status === 'C') {
                            if (this.configIdGenerateInferences.includes(entry.InferenceConfigId)) {
                                if (manualConfigCompleted.indexOf(entry.InferenceConfigId) == -1) {
                                    manualConfigCompleted.push(entry.InferenceConfigId);
                                }
                                //  manualConfigCompleted.push(entry.InferenceConfigId);
                            }
                        }
                    });

                    if (manualConfigInProgress.length > 0) {
                        this.retryGenerateInference(queryParams, true, 'all');
                    } else if (this.configIdGenerateInferences.length === manualConfigCompleted.length) {
                        this._appUtilsService.loadingImmediateEnded();
                        this.notificationService.success('Inference Generated Successfully');
                        this.refreshModelList();
                    }
                }
            }, (error) => {
                this.notificationService.error(error.error);
                this._appUtilsService.loadingEnded();
            }
        )
    }


    /*---------------To Show Generated Inferencenes : Development In-Progress - ------------*/
    showApplied(configIdClicked): void {
        this.selectedConfigId = configIdClicked;
    }


    onSelectedConfig(configId, event) {
        if (event.target.checked) {
            this.configIdGenerateInferences.push(configId);
        } else {
            const indexToBeDeleted = this.checkIndex(configId);
            if (indexToBeDeleted > -1) {
                this.configIdGenerateInferences.splice(indexToBeDeleted, 1);
            }
        }
    }

    checkIndex(configId) {
        return this.configIdGenerateInferences.findIndex(element => (element === configId));
    }


    /*----------------------- Button Click Publish Inference -------------*/
    publishInferenceStart() {
        this.isProcessDisabled = (this.modelCreatedwith[0].SourceName == "File") ? true : false;
        this.publishModalShow = true;
        this.applicationList = [this.ieConfigService.application];
        //  this.onApplicationName(this.ieConfigService.application.ApplicationID);
    }


    selectedForPublish(InferenceConfigId, InferenceConfigSubTypes, InferenceConfigType, event) {
        let indexOfElement;
        if (InferenceConfigType === this.InferenceConfigTypeForMeasureflow) {
            indexOfElement = this.findIfKeyNamePresent('InferenceConfigId', InferenceConfigId, this.InferenceConfigTypeForMeasureflow)
        } else if (InferenceConfigType === this.InferenceConfigTypeForDataInflow) {
            indexOfElement = this.findIfKeyNamePresent('InferenceConfigType', this.InferenceConfigTypeForDataInflow);
        }
        if (indexOfElement > -1) {
            this.editPublishInferencePayload(indexOfElement, InferenceConfigSubTypes, event.target.checked);
        } else {
            this.createPublishInference(InferenceConfigId, InferenceConfigSubTypes, InferenceConfigType)
        }
    }


    findIfKeyNamePresent(keyName, value, optional?) {
        if (optional) {
            return this.publishInferencePayload.findIndex(element => (element[keyName] === value && element.InferenceConfigType === optional));
        } else {
            return this.publishInferencePayload.findIndex(element => (element[keyName] === value));
        }
    }

    editPublishInferencePayload(indexOfElement, InferenceConfigSubTypes, toDelete?) {
        if (toDelete == false) {
            this.removeConfigFromPublishInferencePayload(indexOfElement, InferenceConfigSubTypes);
        } else {
            this.publishInferencePayload[indexOfElement].InferenceConfigSubTypes.push(InferenceConfigSubTypes);
            if (this.publishInferencePayload[indexOfElement].InferenceConfigSubTypes.length === 2) {
                if (this.publishInferencePayload[indexOfElement].InferenceConfigSubTypes[0] === 'Outlier Analysis') {
                    this.publishInferencePayload[indexOfElement].InferenceConfigSubTypes = [];
                    this.publishInferencePayload[indexOfElement].InferenceConfigSubTypes.push('Measure Analysis', 'Outlier Analysis');
                }
            }
        }
    }

    removeConfigFromPublishInferencePayload(indexToBeDeleted, InferenceConfigSubTypes) {
        const subTypeRef = this.publishInferencePayload[indexToBeDeleted].InferenceConfigSubTypes;
        if (subTypeRef.length > 1) {
            const subTypeIndex = subTypeRef.findIndex((type) => (type === InferenceConfigSubTypes))
            subTypeRef.splice(subTypeIndex, 1);
        } else if (subTypeRef.length === 1) {
            this.publishInferencePayload.splice(indexToBeDeleted, 1);
        }
    }

    createPublishInference(InferenceConfigId, InferenceConfigSubTypes, InferenceConfigType) {
        const publishInference = new PublishInference({
            InferenceConfigId,
            'InferenceConfigSubTypes': [InferenceConfigSubTypes],
            InferenceConfigType,
            'CorrelationId': this.correlationId,
            "CreatedBy": this._appUtilsService.getCookies().UserId
        })
        this.publishInferencePayload.push(publishInference);
    }

    // onApplicationName(name) {
    //   this.applicationIdSelected = name;
    //   // correlationId=eb54e08d-373b-4341-8408-c6cca080379d&applicationId=&inferenceconfigId=
    //    this._appUtilsService.loadingStarted();
    //    this.inferenceModelService.getPublishedInferences({ 'correlationId': this.correlationId , 'applicationId': this.applicationIdSelected,
    //    'inferenceconfigId': ''}).subscribe(
    //      (data)=> {
    //        if ( data.length != 0) {
    //          this.publishInferencePayload = data;
    //        } else {

    //        }
    //       this._appUtilsService.loadingEnded();
    //      },
    //      (error)=>{
    //       this._appUtilsService.loadingEnded();
    //      }
    //    )
    // }

    checkedCondition(InferenceConfigId, InferenceConfigSubTypes, InferenceConfigType) {
        let indexOfElement;
        if (InferenceConfigType === this.InferenceConfigTypeForMeasureflow) {
            indexOfElement = this.findIfKeyNamePresent('InferenceConfigId', InferenceConfigId, this.InferenceConfigTypeForMeasureflow)
        } else if (InferenceConfigType === this.InferenceConfigTypeForDataInflow) {
            indexOfElement = this.findIfKeyNamePresent('InferenceConfigType', this.InferenceConfigTypeForDataInflow);
        }
        if (indexOfElement > -1) {
            const subTypePresentOrNot = this.publishInferencePayload[indexOfElement]['InferenceConfigSubTypes'].findIndex((element) => element === InferenceConfigSubTypes);
            if (subTypePresentOrNot > -1) {
                return true;
            } else {
                return false;
            }
        } else {
            return false;
        }
    }

    publishInferenceSubmit() {
        if (this.publishInferencePayload.length > 0) {
            this.publishInferencePayload.forEach((element) => element.ApplicationId = this.ieConfigService.application.ApplicationID);
            this._appUtilsService.loadingStarted();
            this.inferenceModelService.publishInference(this.publishInferencePayload).subscribe(
                (data) => {
                    this.notificationService.success('Successfully published');
                    this.publishModalShow = false;
                    this.redirectionModal = true;
                    this._appUtilsService.loadingEnded();
                },
                (error) => {
                    this.publishModalShow = false;
                    this._appUtilsService.loadingEnded();
                    this.notificationService.error(error.error);
                }
            )
        }
        else {
            this.notificationService.error('Select Application, Inferences to Publish');
        }
    }

    getChipsvalueForMeasure(InferenceConfigId) {
        let totalInferences = '0';
        let selectedorPublishedInferences = '0'

        if (this.outputInflowMeasureToBeShown && this.outputInflowMeasureToBeShown.length > 0) {
            const getMeasureIndex = this.outputInflowMeasureToBeShown.findIndex(element =>
                (element.InferenceConfigId === InferenceConfigId));
            if (getMeasureIndex > -1) {
                totalInferences = this.outputInflowMeasureToBeShown[getMeasureIndex].MeasureAnalysisInferences.length + '';
            }
        }

        const indexOfElement = this.findIfKeyNamePresent('InferenceConfigId', InferenceConfigId, this.InferenceConfigTypeForMeasureflow)
        if (indexOfElement > -1) {
            selectedorPublishedInferences = this.publishInferencePayload[indexOfElement].InferenceConfigSubTypes.length + '';
        }

        return `${selectedorPublishedInferences} / ${totalInferences}`;
    }

    getChipsvalueForDataInflow() {
        let totalInferences = '0';
        let selectedorPublishedInferences = '0'
        if (this.outputInflowDataToBeShown && this.outputInflowDataToBeShown.length > 0) {
            totalInferences = this.outputInflowDataToBeShown[0].VolumetricInferences.length + '';
        }

        const indexOfElement = this.findIfKeyNamePresent('InferenceConfigType', this.InferenceConfigTypeForDataInflow);
        if (indexOfElement > -1) {
            selectedorPublishedInferences = this.publishInferencePayload[indexOfElement].InferenceConfigSubTypes.length + '';
        }
        // InferenceConfigType: "VolumetricAnalysis"

        return `${selectedorPublishedInferences} / ${totalInferences}`;
    }

    expandMAll() {
        this.measureaccordian = {
            'acc0': false,
            'acc1': false,
            'expandAll': false
        }
    }

    collapseMAll() {
        this.measureaccordian = {
            'acc0': true,
            'acc1': true,
            'expandAll': true
        }
    }

    expandDAll() {
        this.dataInflowaccordian = {
            'accOutput0': false,
            'accOutput1': false,
            'accOutput2': false,
            'expandAll': false
        }
    }

    collapseDAll() {
        this.dataInflowaccordian = {
            'accOutput0': true,
            'accOutput1': true,
            'accOutput2': true,
            'expandAll': true
        }
    }

    navigateToVDS() {
        let url = '';
        let ClientUId = sessionStorage.getItem('ClientUId');
        let EndToEndUId = sessionStorage.getItem('End2EndId');
        // 'ApplicationId='';
        let RequestType = sessionStorage.getItem('RequestType');
        let dcID = sessionStorage.getItem('DeliveryConstructUId');
        let EnterpriseId = sessionStorage.getItem('UserId').split('@')[0];

        this.redirectionModal = false;

        if (this._apiService.FDSbaseURL.indexOf('eu') > -1) {
            url = this._apiService.FDSbaseURL + '/nav/InferenceEngine?ClientUId=' + ClientUId + '&EndToEndUId=' + EndToEndUId +
                '&DCUId=' + dcID + '&RequestType=' + RequestType + '&Type=Inference&EnterpriseId=' + EnterpriseId;
            console.log('FDS redirection URL--', url);
        } else {
            const URL_Suffix = (this.environment === 'PAM') ? this._apiService.FDSbaseURL + '/VDS/nav' : this._apiService.FDSbaseURL;
            url = URL_Suffix + '/InferenceEngine?ClientUId=' + ClientUId + '&EndToEndUId=' + EndToEndUId + '&DCUId=' + dcID + '&RequestType=' + RequestType + '&Type=Inference&EnterpriseId=' + EnterpriseId;
            console.log('FDS redirection URL--', url);
        }
        window.open(url, '_blank');
    }
}
