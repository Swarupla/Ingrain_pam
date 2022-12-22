import { Component, OnInit, ViewChild, ChangeDetectionStrategy } from '@angular/core';
import { interval, timer, Observable, Subscription, throwError, of, EMPTY } from 'rxjs';
import { DataEngineeringService } from 'src/app/_services/data-engineering.service';
import { catchError, tap, switchMap, finalize } from 'rxjs/operators';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { CoreUtilsService } from '../../../../../_services/core-utils.service';
import { DialogRef } from 'src/app/dialog/dialog-ref';
import { DialogConfig } from 'src/app/dialog/dialog-config';
import { AppUtilsService } from 'src/app/_services/app-utils.service';

@Component({
    selector: 'app-view-data',
    templateUrl: './view-data.component.html',
    styleUrls: ['./view-data.component.scss'],
})
export class ViewDataComponent implements OnInit {

    @ViewChild('helppanelopen', { static: false }) helppanelopen;
    @ViewChild('helppanelclose', { static: false }) helppanelclose;
    @ViewChild('selectedDataType', { static: false }) selectedDataType;


    page = 1;
    status = false;
    cleanupData = false;
    counter = 0;
    count = 0;
    allowedCount: number;
    dataToCleanUp;
    repeatedCalls;
    userId = 'hariom.thakur@accenutre.com';
    correlationId = '';
    pageInfo = 'DataCleanUp';
    pythonProgress;
    pythonProgressStatus;

    processData;
    featureNames;
    featureNameArray = [];

    cleanupDataTypeArray;
    cleanupCorrelation;
    cleanupScale;
    objecttoarraypipe;
    timerSubscripton: Subscription;
    timerSubscriptonForSave: Subscription;
    dataEngineeringSubscription: Subscription;
    showhelppanel = false;
    totalRowsOfFeatureNames: number;
    sizeofScaleObject: number;
    sizeofDatatypesObject: number;
    fixMissingValue = new Set([]);
    fixDataQuality = new Set([]);
    correlation = [];
    cleanedupData: {};
    selectedDataTypesArray: any;
    postselectedDatatypeObject = {};
    featurenameinobject: any;
    defaultDataType;
    corelationForPrescription: Set<[]>;
    idDataTypeForPrescription = new Set([]);
    isDataTypeContainsId: boolean;
    sizeofDatatypesFromDB: number;
    isDataTypeDropDownEmpty: boolean;
    problemStatementData;
    correlationtoRemove: any;
    dataCleanUpObject: any;
    dataCleanUpData: any;
    featureNamesObject: any;
    fixedFeatureNamesObject = new Set([]);
    selectedDataTypesToSave = {};
    selectedcleanupScalesToSave = {};
    newCorrelationIdAfterCloned;

    mappings = {
        featurename: 'Feature Name',
        datatype: 'Datatype',
        correlation: 'Correlation',
        scale: 'Scale',
        outlier: 'Outlier',
        imbalanced: 'ImBalanced',
        skewness: 'Skewness',
        dataqualityscore: 'Data_Quality_Score',
        missingvalues: 'Missing Values',
        q_info: 'Q_Info',
        correlationtoremove: 'CorrelationToRemove'
    };

    isLoading = true;
    featuresNamestobeFixed = new Set([]);
    datatypeForPrescriptionEmpty = new Set([]);
    defaultDataTypes = ['category', 'Text', 'float64', 'int64', 'Id', 'datatime64[ns]'];
    // defaultDataTypes = ['Not Applicable']
    // defaultScales = ['Nominal', 'Ordinal'];
    defaultScales = ['Not Applicable'];
    fixEmptyDatatypeFeatureNames = new Set([]);
    pstModelName: any;
    pstDataSource: any;
    newModelName: {};
    noData: string;
    dataQualityInfo = '';
    pstUseCase: any;
    searchText;
    prescriptionCorrelatedVal;
    highlyCorrelatedKeys = [];
    DtypeModifiedColumns = {};
    ScaleModifiedColumns = {};
    saveSubscription: Subscription;
    nextDisabled: Boolean = true;
    fixitBtnDisabled: Boolean = false;
    countOfUnselectedDataType = 0;
    featurenamecount = 0;
    unselectedDatatypeFeatures = new Set();
    finalcount = 0;
    redDatatype = false;
    viewDataTimerSubscripton: Subscription;
    viewDataSubscription: Subscription;
    ViewDataQuality: {};
    showDataColumnsList: any[];
    tableData: any[];
    decimalPoint: any;

    constructor(private des: DataEngineeringService, private ns: NotificationService,
        private dr: DialogRef, private dc: DialogConfig, private coreUtilsService: CoreUtilsService, private aus: AppUtilsService) {
        this.decimalPoint = sessionStorage.getItem('decimalPoint') ? parseInt(sessionStorage.getItem('decimalPoint')) : 2;
    }

    ngOnInit() {
        this.correlationId = this.dc.data['correlationId'];
        this.userId = this.aus.getCookies().UserId;
        this.viewData();
    }

    viewData() {
        this.isLoading = true;
        this.viewDataSubscription = this.des.getViewData(this.correlationId, this.userId, 'ViewDataQuality').pipe(
            switchMap(
                data => {
                    let tempData = {};
                    if (this.IsJsonString(data)) {
                        const parsedData = JSON.parse(data);
                        tempData = parsedData;
                    } else if (data.constructor === Object) {
                        tempData = data;
                    } else if (data.constructor === String) {
                        this.noData = data + '';
                        this.ns.success(data);
                    }

                    if (tempData.hasOwnProperty('message')) {
                        return this.des.getViewData(this.correlationId, this.userId, 'ViewDataQuality');
                    } else if (tempData.hasOwnProperty('UseCaseDetails')) {
                        return of(tempData);
                    } else {
                        this.noData = 'Format from server is not Recognised';
                        this.ns.error(`Error occurred:
              Due to some backend data process the relevant
              data could not be produced. Please try again while we troubleshoot the error`);

                        return EMPTY;
                    }
                }),
            catchError(error => {
                return EMPTY;
            })
        ).subscribe(da => {

            const status = JSON.parse(da['UseCaseDetails'])['Status'];
            if (status === 'P') {
                this.isLoading = true;
                this.viewDataRetry();
            } else if (status === 'C') {

                if (!this.coreUtilsService.isNil(da) && !this.coreUtilsService.isNil(da['ViewData'])) {
                    this.ViewDataQuality = da['ViewData'];
                }
                // const AttributeNames 
                const AttributeNames = Object.keys(this.ViewDataQuality || {});
                const ColumnList = [];
                const tempTableData = [];
                if (AttributeNames.length > 0) {
                    this.ViewDataQuality = { ...this.ViewDataQuality }; // clone an Object

                    AttributeNames.forEach(x => {
                        this.ViewDataQuality[x]['ATTRIBUTE NAME'] = x;
                        tempTableData.push({ ...this.Flatnner(this.ViewDataQuality[x]) });
                    });
                }
                this.tableData = tempTableData;
                this.tableData.forEach(element => {
                    if (element['Data_Quality_Score']) {
                        element['Data_Quality_Score'] = parseFloat(element['Data_Quality_Score']).toFixed(this.decimalPoint);
                    }
                    if (element['Outlier']) {
                        element['Outlier'] = parseFloat(element['Outlier']).toFixed(this.decimalPoint);
                    }
                    if (element['Unique']) {
                        element['Unique'] = parseFloat(element['Unique']).toFixed(this.decimalPoint);
                    }
                    if (element['Missing Values']) {
                        element['Missing Values'] = parseFloat(element['Missing Values']).toFixed(this.decimalPoint);
                    }
                });
                this.viewDataunsubscribe();
                this.isLoading = false;

            } else if (status === 'E') {
                this.ns.error(JSON.parse(da['UseCaseDetails'])['Message']);
                this.isLoading = false;
            } else {
                this.isLoading = true;
                this.noData = 'Format from server is not Recognised';
                this.ns.error(`Error occurred: Due to
         some backend data process the
          relevant data could not be produced.
           Please try again while we troubleshoot the error`);
                this.viewDataunsubscribe();
                this.isLoading = false;
            }

        });
    }

    Flatnner(obj: {}): {} {

        for (const x in obj) {
            if (typeof obj[x] === 'object') {
                let temp = '';
                const tempObj = {};
                if (Object.keys(obj[x]).length === 0) {
                    obj[x] = '';
                }

                for (const t in obj[x]) {
                    if (obj[x][t] === 'True') {
                        temp = t;
                    } else if (obj[x][t] === 'False') {

                    } else {
                        temp = temp + obj[x][t];
                    }

                }

                obj[x] = temp;
            }
        }
        return obj;
    }

    viewDataunsubscribe() {
        if (!this.coreUtilsService.isNil(this.viewDataTimerSubscripton)) {
            this.viewDataTimerSubscripton.unsubscribe();
        }
        if (!this.coreUtilsService.isNil(this.viewDataSubscription)) {
            this.viewDataSubscription.unsubscribe();
        }
        this.isLoading = false;
    }

    viewDataRetry() {
        this.viewDataTimerSubscripton = timer(10000).subscribe(() => this.viewData());
        return this.viewDataTimerSubscripton;
    }

    IsJsonString(str) {
        try {
            JSON.parse(str);
        } catch (e) {
            return false;
        }
        return true;
    }

    getClassForDataQualityScore(featureName) {
        const dataQualityScore = featureName[this.mappings.dataqualityscore];
        const dataTypes = featureName[this.mappings.datatype];
        if (dataTypes.length === 0) {
            return 'ingrAI-data-quality-failed-by-datatype';
        }
        if (dataQualityScore <= 0) {
            return 'ingrAI-data-quality-NA';
        }
        if (dataQualityScore > 0 && dataQualityScore < 50) {
            return 'ingrAI-data-quality-failed';
        } else if (dataQualityScore >= 50 && dataQualityScore < 80) {
            return 'ingrAI-data-quality-average';
        } else if (dataQualityScore >= 80) {
            return 'ingrAI-data-quality-success';
        }
    }


    openHelper(featureName) {
        this.showhelppanel = true;
        this.dataQualityInfo = this.ViewDataQuality[featureName][this.mappings.q_info];
        // this.helppanelopen.nativeElement.classList.add('show');
    }

    closeHelper() {
        this.showhelppanel = false;
        // this.helppanelopen.nativeElement.classList.remove('show');
    }

    ignore() {
        this.status = false;
    }

    onClose() {
        this.dr.close();
    }

    getUnselectedDatatype(featureName) {
        const dataTypes = Object.keys(this.ViewDataQuality[featureName][this.mappings.datatype]);
        const dt = this.ViewDataQuality[featureName][this.mappings.datatype];
        for (const keys in dt) {
            if (dt[keys].toLowerCase() === 'true') {
                return keys;
            }
        }
    }

    getUniqueValue(featureName) {
        return parseFloat(this.ViewDataQuality[featureName].Unique).toFixed(this.decimalPoint);
    }

    getCorrelation(featureName) {
        const highlyCorrelatedVlues = Object.keys(this.ViewDataQuality[featureName][this.mappings.correlation])
            .map(item => this.ViewDataQuality[featureName][this.mappings.correlation][item]);
        const correlations = highlyCorrelatedVlues.join(', ');
        return correlations.toString();
    }

    getScales(featureName) {
        const scales = this.ViewDataQuality[featureName][this.mappings.scale];
        for (const keys in scales) {
            if (scales[keys] === 'True') {
                console.log(keys)
                return scales.length === 0 ? this.defaultScales : keys;
            }
        }
    }

    getMissingValue(featureName) {
        const MissingValue = parseFloat(this.ViewDataQuality[featureName][this.mappings.missingvalues]).toFixed(this.decimalPoint);
        return MissingValue;
    }

    getOutlier(featureName) {
        return parseFloat(this.ViewDataQuality[featureName][this.mappings.outlier]).toFixed(this.decimalPoint);
    }

    getBalanced(featureName) {
        return this.ViewDataQuality[featureName].Balanced;
    }

    getSKewNess(featureName) {
        return this.ViewDataQuality[featureName][this.mappings.skewness];
    }

    getDataQualityScore(featureName) {
        let dataQualityScore = this.ViewDataQuality[featureName][this.mappings.dataqualityscore];
        dataQualityScore = parseFloat(dataQualityScore).toFixed(this.decimalPoint);

        const dt = this.ViewDataQuality[featureName][this.mappings.datatype];
        let featureDataType; // To get the selected datatype as true
        for (const keys in dt) {
            if ((keys === 'Text' || keys === 'Id' || keys === 'datetime' || keys === 'datetime64[ns]')
                && dt[keys].toLowerCase() === 'true') {
                featureDataType = keys;
            }
        }
        if (featureDataType == 'Id') {
            return 'NA';
        }
        return dataQualityScore;
    }

}
