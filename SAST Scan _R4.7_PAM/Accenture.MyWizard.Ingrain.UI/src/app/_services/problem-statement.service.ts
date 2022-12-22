import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { tap, isEmpty } from 'rxjs/operators';
import { LocalStorageService } from './local-storage.service';
import { CoreUtilsService } from './core-utils.service';
import { from, Observable, throwError } from 'rxjs';
// import { forEach } from '@angular/router/src/utils/collection';
import { EnvironmentService } from 'src/app/_services/EnvironmentService';
import { HttpClient, HttpHeaders, HttpResponse } from '@angular/common/http';
import { CustomDataTypes } from '../_enums/custom-data-types.enum';

@Injectable({
    providedIn: 'root'
})
export class ProblemStatementService {
    tColumn: any;
    uniqueIdentifier: any;
    timeSeriesColumn: any;
    getColumnsdata: any;
    _body: any;
    public isPredefinedTemplate = 'False';
    filedata: any;
    uploadedfileName: any;
    correlationIdMP;
    public isMappedFromTemplate = 'False';
    FileChunk = [];
    filename;
    filenamelist = [];
    modelname;
    TotalParts;
    PartCount;

    constructor(private api: ApiService, private ls: LocalStorageService, private coreUtilsService: CoreUtilsService,
        private envService: EnvironmentService, private http: HttpClient) { }

    getAppModelsTrainingStatus(clientid, dcid, userid) {
        return this.api.get('GetAppModelsTrainingStatus', { clientid: clientid, dcid: dcid, userid: userid });
    }

    getPrivateModelTraning(clientid, dcid, userid) {
        this._body = '';
        return this.api.post('PrivateModelTraning', this._body, { ClientID: clientid, DCID: dcid, UserId: userid });
    }

    getPrivateCascadeModelTraining(clientid, dcid, userid) {
        this._body = '';
        return this.api.post('PrivateCascadeModelTraining', this._body, { ClientID: clientid, DCID: dcid, UserId: userid });
    }

    postModelReTraning(payload) {
        return this.api.post('ModelReTraning', payload);
    }

    getDefaultModelsTrainingStatus(clientid, dcid, userid) {
        return this.api.get('GetDefaultModelsTrainingStatus', { clientid: clientid, dcid: dcid, userid: userid });
    }

    getColumns(correlationId, isExistingTemplate, isNewModel) {
        return this.api.get('GetColumns', { correlationId: correlationId, IsTemplate: isExistingTemplate, newModel: isNewModel });
    }

    getColumnsForMyModels(correlationId) {

        return this.api.get('GetColumns', { correlationId: correlationId, IsTemplate: false, newModel: false });
    }

    getViewUploadedData(correlationId, decimalPoint) {
        return this.api.get('ViewUploadedData', { correlationId: correlationId, DecimalPlaces: decimalPoint });
    }

    uploadFiles(fileData, params) {
        this.filedata = JSON.parse(JSON.stringify(fileData));
        const frmData = new FormData();
        const fileName = {};
        if (fileData[0].parentFileName !== undefined) {
            fileName['name'] = fileData[0].parentFileName;
        } else {
            fileName['name'] = fileData[0].source;
        }

        this.filedata.shift();

        for (let i = 0; i < this.filedata.length; i++) {
            // frmData.append('excelfile', fileData[i]);
            if (i === 0) {
                frmData.append('pad', JSON.stringify(this.filedata[i].pad));
            }
            if (i === 1) {
                frmData.append('InstaMl', JSON.stringify(this.filedata[i].InstaMl));
            }
            if (i === 2) {
                frmData.append('metrics', JSON.stringify(this.filedata[i].metrics));
            }
            if (i === 4) {
                if (this.coreUtilsService.isEmptyObject(this.filedata[i].EntitiesName)) {
                    frmData.append('EntitiesName', JSON.stringify(this.filedata[i].EntitiesName));
                } else {
                    frmData.append('EntitiesName', this.filedata[i].EntitiesName);
                }
            }
            if (i === 5) {
                if (this.coreUtilsService.isEmptyObject(this.filedata[i].MetricNames)) {
                    frmData.append('MetricNames', JSON.stringify(this.filedata[i].MetricNames));
                } else {
                    frmData.append('MetricNames', this.filedata[i].MetricNames);
                }
            }
            if (i === 6) {
                frmData.append('Custom', JSON.stringify(this.filedata[i].Custom));
            }
            if (i === 7) {
                if (params.source === CustomDataTypes.API) {
                    frmData.append('CustomDataPull', JSON.stringify(this.filedata[i].CustomDataPull));
                }
                if (params.source === CustomDataTypes.Query) {
                    frmData.append('CustomDataPull', this.filedata[i].CustomDataPull);
                    frmData.append('DateColumn', this.filedata[i].DateColumn);
                }
            }
        }

        if (fileData[0]["DatasetUId"] !== undefined) {
            frmData.append('DataSetUId', fileData[0]["DatasetUId"]);
        }

        let fileIndex = 3;
        if ((sessionStorage.getItem('Environment') === 'PAM' || sessionStorage.getItem('Environment') === 'FDS') && (sessionStorage.getItem('RequestType') === 'AM' || sessionStorage.getItem('RequestType') === 'IO')) {
            fileIndex = 3;

        }

        // Permenant fix for hasOwnProperty issue
        for (const index in fileData) {
            if (fileData) {
                if (fileData[index].hasOwnProperty('fileUpload')) {
                    fileIndex = Number(index);
                }
            }
        }

        //  let fileIndexAfterCheckingPosition = fileData.length - fileIndex;
        const fileIndexAfterCheckingPosition = fileIndex;

        if (fileData[fileIndexAfterCheckingPosition].fileUpload.hasOwnProperty('filepath')) {
            const files = fileData[fileIndexAfterCheckingPosition].fileUpload.filepath;
            if (files.length > 0) {
                for (let j = 0; j < files.length; j++) {
                    frmData.append('excelfile', files[j]);
                }
            }
        }
        // NA defect fix - For Upload progress message
        let fileuploadedDetails;
        if (fileData[fileIndexAfterCheckingPosition].fileUpload.hasOwnProperty('filepath')) {
            fileuploadedDetails = fileData[fileIndexAfterCheckingPosition].fileUpload.filepath[0];
        } else {
            fileuploadedDetails = fileName;
        }
        if (fileuploadedDetails?.name) {
            const pattern = /[/\\?%*:|"<>]/g;
            const specialCharaterCheck = pattern.test(fileuploadedDetails.name);
            const fileExtensionCheck = fileuploadedDetails.name.split('.');
            if (specialCharaterCheck || fileExtensionCheck.length > 2) {
                return throwError({ error: { Category: { Message: 'Invalid file name' } } });
            }
        }
        return this.api.postFile('FileUpload', fileuploadedDetails, frmData, params).pipe(
            tap(data => {
                const uploadedData = this.coreUtilsService.isNil(data.body) ? data : data.body[0];
                this.ls.setLocalStorageData('correlationId', uploadedData);
            }),
        );
    }

    uploadSimilarityFiles(fileData, params) {
        const frmData = new FormData();
        const fileName = {};
        let fileuploadedDetails;
        if (fileData[0].parentFileName !== undefined) {
            fileName['name'] = fileData[0].parentFileName;
        } else {
            fileName['name'] = fileData[0].source;
        }
        // fileData.shift();
        if (fileData[0].source === 'Entity') {
            frmData.append('pad', JSON.stringify(fileData[1].pad));
            frmData.append('EntitiesName', fileData[2].EntitiesName);
            if (fileData[3].MaxDataPull) {
                frmData.append('MaxDataPull', fileData[3].MaxDataPull);
            } else {
                frmData.append('MaxDataPull', null);
            }
            frmData.append('IsCarryOutRetraining', fileData[3].IsCarryOutRetraining);
            frmData.append('IsOffline', fileData[3].IsOffline);
            frmData.append('IsOnline', fileData[3].IsOnline);
            frmData.append('Prediction', JSON.stringify(fileData[3].Prediction));
            frmData.append('Retraining', JSON.stringify(fileData[3].Retraining));
            frmData.append('Training', JSON.stringify(fileData[3].Training));
            if (fileData[3] != undefined && fileData[3].hasOwnProperty('CorrelationId')) {
                frmData.append('CorrelationId', fileData[3].CorrelationId);
            } else if (fileData[4] != undefined && fileData[4].hasOwnProperty('CorrelationId')) {
                frmData.append('CorrelationId', fileData[4].CorrelationId);
            }
            if (params.retrainFlag === true) {
                frmData.append('CorrelationId', fileData[0].CorrelationId);
            }
        } else if (fileData[0].source === 'Custom') {
            frmData.append('pad', JSON.stringify(fileData[1].pad));
            frmData.append('EntitiesName', JSON.stringify(fileData[2].EntitiesName));
            frmData.append('InstaMl', JSON.stringify(fileData[4].InstaMl));
            frmData.append('metrics', JSON.stringify(fileData[5].metrics));
            frmData.append('MetricNames', JSON.stringify(fileData[6].MetricNames));
            frmData.append('Custom', JSON.stringify(fileData[7].Custom));
            if (params.retrainFlag === true) {
                frmData.append('CorrelationId', fileData[0].CorrelationId);
            }
        } else if (fileData[0].source === 'File') {
            if (fileData[1].fileUpload.hasOwnProperty('filepath')) {
                const files = fileData[1].fileUpload.filepath;
                if (files.length > 0) {
                    for (let j = 0; j < files.length; j++) {
                        frmData.append('excelfile', files[j]);
                    }
                }
            }

            frmData.append('pad', JSON.stringify(fileData[2].pad));
            frmData.append('EntitiesName', JSON.stringify(fileData[3].EntitiesName));
            // frmData.append('MaxDataPull', JSON.stringify(fileData[3].MaxDataPull));
            if (fileData[4] != undefined && fileData[4].hasOwnProperty('CorrelationId'))
                frmData.append('CorrelationId', fileData[4].CorrelationId);
        }
        else if (fileData[0].source === 'DataSet') {
            frmData.append('pad', '{}');
            frmData.append('InstaMl', '{}');
            frmData.append('metrics', '{}');
            frmData.append('EntitiesName', '{}');
            frmData.append('MetricNames', '{}');
            // frmData.append('MaxDataPull','{}');
            if (params.retrainFlag === true) {
                frmData.append('DataSetUId', fileData[3].DataSetUId);
                frmData.append('CorrelationId', fileData[4].CorrelationId);
            } else {
                frmData.append('DataSetUId', fileData[3].DataSetUId);
            }
        } else if (fileData[0].source === 'Custom') {
            frmData.append('Custom', JSON.stringify(fileData[3].Custom));
        } else if (fileData[0].source === CustomDataTypes.API) {

            frmData.append('excelfile', '{}');
            frmData.append('pad', '{}');
            frmData.append('EntitiesName', '{}');
            frmData.append('CustomDataPull', JSON.stringify(fileData[1].CustomDataPull));
            if (params.retrainFlag === true) {
                frmData.append('CorrelationId', fileData[0].CorrelationId);
            }
        } else if (fileData[0].source === CustomDataTypes.Query) {
            frmData.append('CustomDataPull', fileData[1].CustomDataPull);
            frmData.append('DateColumn', fileData[1].DateColumn);
            if (params.retrainFlag === true) {
                frmData.append('CorrelationId', fileData[0].CorrelationId);
            }
        }

        // NA defect fix - For Upload progress message
        if (fileData[1].hasOwnProperty('fileUpload') && fileData[1].fileUpload.hasOwnProperty('filepath')) {
            fileuploadedDetails = fileData[1].fileUpload.filepath[0];
        } else {
            fileuploadedDetails = fileName;
        }
        let apiName = 'AIServiceIngestData';
        if (params.ServiceId === '72c38b39-c9fe-4fa6-97f5-6c3adc6ba355') { // For Clustering
            apiName = 'ClusterServiceIngestData';
        }

        if (params.ServiceId === '042468f4-db5b-403f-8fbc-e5378077449e') { // For Word Cloud
            apiName = 'ClusterServiceIngestData';
            params.pageInfo = "wordcloud";
        }
        if (fileuploadedDetails?.name) {
            const pattern = /[/\\?%*:|"<>]/g;
            const specialCharaterCheck = pattern.test(fileuploadedDetails.name);
            const fileExtensionCheck = fileuploadedDetails.name.split('.');
            if (specialCharaterCheck || fileExtensionCheck.length > 2) {
                return throwError({ error: 'Invalid file name' });
            }
        }
        return this.api.postFile(apiName, fileuploadedDetails, frmData, params).pipe(
            tap(data => {
                const uploadedData = this.coreUtilsService.isNil(data.body) ? data : data.body[0];
                this.ls.setLocalStorageData('correlationId', uploadedData);
            }),
        );
    }

    getAIServiceIngestStatus(correlationId, pageInfo, selectedServiceId) {
        let apiName = 'AIServiceIngestStatus';
        if (selectedServiceId === '72c38b39-c9fe-4fa6-97f5-6c3adc6ba355') { // For Clustering
            apiName = 'ClusteringServiceIngestStatus';
        }
        return this.api.get(apiName, { correlationId: correlationId, pageInfo: pageInfo }).pipe(
            tap(data => {
                const uploadedData = this.coreUtilsService.isNil(data.body) ? data : data.body[0];
                this.ls.setLocalStorageData('correlationId', uploadedData);
            }),
        );
    }

    postSelectedColumnNamesModelTraining(payload) {
        const frmData = new FormData();
        /* for (let i = 0; i < fileData.length; i++) {
          frmData.append('excelfile', fileData[i]);
        } */
        Object.entries(payload).forEach(
            ([key, value]) => {
                if (key !== 'file') {
                    if (key === 'CustomParam') {
                        frmData.append(key, JSON.stringify(payload[key]));
                    } else if (key === 'selectedColumns') {
                        frmData.append(key, JSON.stringify(payload[key]));
                    } else {
                        frmData.append(key, payload[key]);
                    }
                }
                // else {
                //   frmData.append(key, fileData[0]);
                // }
            }
        );
        if (payload.Threshold_TopnRecords !== undefined || payload.Threshold_TopnRecords !== null) {
            return this.api.post('InvokeAICoreServiceSingleBulkMultipleWithFile', frmData);
        } else {
            return this.api.post('InvokeAICoreServiceWithFile', frmData);
        }
    }

    getSimRetrainModelDetails(correlationId) {
        return this.api.get('RetrainModelDetails', { correlationId: correlationId });
    }

    getModels(Templates, userId, category, dateFilter, deliveryConstructUID, clientUId) {
        return this.api.get('GetTemplateModels',
            {
                'Templates': Templates, 'userId': userId, 'category': category,
                'dateFilter': dateFilter, 'DeliveryConstructUID': deliveryConstructUID, 'ClientUId': clientUId
            });
    }
    getPublicTemplatess(Templates, userId, category, dateFilter, deliveryConstructUID, clientUId) {
        return this.api.get('GetTemplateModels',
            {
                'Templates': Templates, 'userId': userId, 'category': category,
                'dateFilter': dateFilter, 'DeliveryConstructUID': deliveryConstructUID, 'ClientUId': clientUId
            });
    }

    clone(correlationId, name) {
        const userId = sessionStorage.getItem('userId');
        const clientId = sessionStorage.getItem('clientID');
        const dcId = sessionStorage.getItem('dcID');
        return this.api.get('CloneData', { correlationId, modelName: name, userId: userId, clientUId: clientId, deliveryConstructUID: dcId });
    }

    invokePython(AvailableColumns, TargetColumn, InputColumns, correlationId,
        problemStatement, problemType, timeSeriesCheckbox, uniqueIdentifier, userid, IsCustomColumnSelected?) {

        const p_CorrelationId = localStorage.getItem('oldCorrelationID') !== 'null' ? localStorage.getItem('oldCorrelationID') : null;
        const data = {
            'AvailableColumns': JSON.stringify(Array.from(AvailableColumns)),
            'TargetColumn': TargetColumn,
            'TargetUniqueIdentifier': uniqueIdentifier,
            'InputColumns': JSON.stringify(Array.from(InputColumns)),
            CorrelationId: correlationId,
            CreatedByUser: userid,
            'ProblemStatement': problemStatement,
            'ProblemType': problemType,
            'TimeSeries': timeSeriesCheckbox,
            'ParentCorrelationId': p_CorrelationId,
            'IsDataTransformationRetained': false
        };
        data['IsCustomColumnSelected'] = (IsCustomColumnSelected) ? 'True' : 'False';
        return this.api.post('PostColumns', data);
    }

    setTargetColumn(targetcolumn) {
        this.tColumn = targetcolumn;
        this.ls.setLocalStorageData('targetcolumn', this.tColumn);
    }

    getTargetColumn() {
        this.tColumn = this.tColumn ? this.tColumn : this.ls.getTargetColumn();
        return this.tColumn;
    }
    setUniqueIdentifier(uniqueIdentifier) {
        this.uniqueIdentifier = uniqueIdentifier;
        this.ls.setLocalStorageData('uniqueIdentifier', this.uniqueIdentifier);
    }
    setTimeSeriesColumn(timeSeriesColumn) {
        this.timeSeriesColumn = timeSeriesColumn;
        this.ls.setLocalStorageData('timeSeriesColumn', this.timeSeriesColumn);
    }

    getUniqueIdentifier() {
        this.uniqueIdentifier = this.uniqueIdentifier ? this.uniqueIdentifier : this.ls.getUniqueIdentifier();
        return this.uniqueIdentifier;
    }
    getTimeSeriesColumn() {
        this.timeSeriesColumn = this.timeSeriesColumn ? this.timeSeriesColumn : this.ls.getTimeSeriesColumn();
        return this.timeSeriesColumn;
    }

    dataSourceFileUpload(uploadedFileName, params) {

        this.uploadedfileName = JSON.parse(JSON.stringify(uploadedFileName));
        const frmData = new FormData();
        const fileName = {};
        fileName['name'] = uploadedFileName[0].parentFileName;

        this.uploadedfileName.shift();

        for (let i = 0; i < this.uploadedfileName.length; i++) {
            // frmData.append('excelfile', fileData[i]);
            if (i === 0) {
                frmData.append('pad', JSON.stringify(this.uploadedfileName[i].pad));
            }
            if (i === 1) {
                frmData.append('InstaMl', JSON.stringify(this.uploadedfileName[i].InstaMl));
            }
            if (i === 2) {
                frmData.append('metrics', JSON.stringify(this.uploadedfileName[i].metrics));
            }
            if (i === 4) {
                if (this.coreUtilsService.isEmptyObject(this.uploadedfileName[i].EntitiesName)) {
                    frmData.append('EntitiesName', JSON.stringify(this.uploadedfileName[i].EntitiesName));
                } else {
                    frmData.append('EntitiesName', this.uploadedfileName[i].EntitiesName);
                }
            }
            if (i === 5) {
                if (this.coreUtilsService.isEmptyObject(this.uploadedfileName[i].MetricNames)) {
                    frmData.append('MetricNames', JSON.stringify(this.uploadedfileName[i].MetricNames));
                } else {
                    frmData.append('MetricNames', this.uploadedfileName[i].MetricNames);
                }
            }
            if (i === 6) {
                frmData.append('Custom', JSON.stringify(this.uploadedfileName[i].Custom));
            }
            if (i === 7) {
                if (params.source === CustomDataTypes.API) {
                    frmData.append('CustomDataPull', JSON.stringify(this.uploadedfileName[i].CustomDataPull));
                }
                if (params.source === CustomDataTypes.Query) {
                    frmData.append('CustomDataPull', this.uploadedfileName[i].CustomDataPull);
                    frmData.append('DateColumn', this.uploadedfileName[i].DateColumn);
                }
            }
        }
        let fileIndex = 3;
        if ((sessionStorage.getItem('Environment') === 'PAM' || sessionStorage.getItem('Environment') === 'FDS') && (sessionStorage.getItem('RequestType') === 'AM' || sessionStorage.getItem('RequestType') === 'IO')) {
            fileIndex = 3;
        }


        if (uploadedFileName[0]["DatasetUId"] !== undefined) {
            frmData.append('DataSetUId', uploadedFileName[0]["DatasetUId"]);
        }

        // Permenant fix for hasOwnProperty issue
        for (const index in uploadedFileName) {
            if (uploadedFileName) {
                if (uploadedFileName[index].hasOwnProperty('fileUpload')) {
                    fileIndex = Number(index);
                }
            }
        }

        //  let fileIndexAfterCheckingPosition = uploadedFileName.length - fileIndex;
        const fileIndexAfterCheckingPosition = fileIndex;

        if (uploadedFileName[fileIndexAfterCheckingPosition].fileUpload.hasOwnProperty('filepath')) {
            const files = uploadedFileName[fileIndexAfterCheckingPosition].fileUpload.filepath;
            if (files.length > 0) {
                for (let j = 0; j < files.length; j++) {
                    frmData.append('excelfile', files[j]);
                }
            }
        }
        /* const frmData = new FormData();
          for (let i = 0; i < uploadedFileName.length; i++) {
           frmData.append('excelfile', uploadedFileName[i]);
         } */
        // NA defect fix - For Upload progress message
        let fileuploadedDetails;
        if (uploadedFileName[fileIndexAfterCheckingPosition].fileUpload.hasOwnProperty('filepath')) {
            fileuploadedDetails = uploadedFileName[fileIndexAfterCheckingPosition].fileUpload.filepath[0];
        } else {
            fileuploadedDetails = fileName;
        }
        return this.api.postFile('DataSourceFileUpload', fileuploadedDetails, frmData, params);
    }

    deleteModelByCorrelationId(correlationId) {
        return this.api.get('FlushModel', { 'correlationId': correlationId });
    }

    getExistingModelName(modelname) {
        return this.api.get('GetExistingModelName', { modelName: modelname.toString().trim() });
    }

    /** Api added for mapping of columns */
    uploadMappingColumns(payload, params, fileUploadPayload, inferenceEngine?) {
        const frmData = new FormData();
        frmData.append('mappingPayload', JSON.stringify(payload));

        for (const innerindex in fileUploadPayload) {
            if (fileUploadPayload) {
                if (fileUploadPayload[innerindex].hasOwnProperty('EntitiesName')) {
                    if (this.coreUtilsService.isEmptyObject(fileUploadPayload[Number(innerindex)].EntitiesName)) {
                        frmData.append('EntitiesName', JSON.stringify(fileUploadPayload[Number(innerindex)].EntitiesName));
                    } else {
                        frmData.append('EntitiesName', fileUploadPayload[Number(innerindex)].EntitiesName);
                    }
                }

                if (fileUploadPayload[innerindex].hasOwnProperty('MetricNames')) {
                    if (this.coreUtilsService.isEmptyObject(fileUploadPayload[Number(innerindex)].MetricNames)) {
                        frmData.append('MetricNames', JSON.stringify(fileUploadPayload[Number(innerindex)].MetricNames));
                    } else {
                        frmData.append('MetricNames', fileUploadPayload[Number(innerindex)].MetricNames);
                    }
                }
            }
        }


        let index = 3;
        if ((sessionStorage.getItem('Environment') === 'PAM' || sessionStorage.getItem('Environment') === 'FDS') && (sessionStorage.getItem('RequestType') === 'AM' || sessionStorage.getItem('RequestType') === 'IO')) {
            index = 3;
        }


        // Permenant fix for hasOwnProperty issue
        for (const index2 in fileUploadPayload) {
            if (fileUploadPayload) {
                if (fileUploadPayload[index2].hasOwnProperty('fileUpload')) {
                    index = Number(index2);
                }
            }
        }

        //  let fileIndexAfterCheckingPosition = fileData.length - fileIndex;
        const fileIndexAfterCheckingPosition = index;

        if (fileUploadPayload[fileIndexAfterCheckingPosition].fileUpload.hasOwnProperty('filepath')) {
            const files = fileUploadPayload[fileIndexAfterCheckingPosition].fileUpload.filepath;
            if (files.length > 0) {
                for (let j = 0; j < files.length; j++) {
                    frmData.append('excelfile', files[j]);
                }
            }
        }

        // Inference Engine :+: Multiple Upload API
        if (inferenceEngine) { return this.api.post('IEUploadMappingColumns', frmData, params); }
        return this.api.post('UploadMappingColumns', frmData, params);
    }

    storeData(datas) {
        this.getColumnsdata = datas;
    }
    getMetricsData(clientUId, deliveryConstructUId, UserId) {
        return this.api.get('GetMetricData', { clientUId: clientUId, 'DeliveryConstructUId': deliveryConstructUId, 'userId': UserId });
    }

    //  adding dropdown in publishmodel
    getDeployAppsDetails(correlationId, clientUId, deliveryConstructUID) {
        let environment = sessionStorage.getItem('Environment');
        if (environment === null) {
            environment = sessionStorage.getItem('fromSource');
            if (!this.coreUtilsService.isNil(environment)) {
                environment = environment.toUpperCase();
            }
        }
        return this.api.get('GetAllAppDetails', {
            clientUId: clientUId,
            deliveryConstructUID: deliveryConstructUID,
            CorrelationId: correlationId,
            Environment: environment
        });
    }

    saveNewApp(data: any) {
        return this.api.post('AddNewApp', data);
    }

    appeditable(correlationId) {
        return this.api.get('IsAppEditable', { CorrelationId: correlationId });
    }
    // End
    getAllAIServices() {
        return this.api.get('GetAllAIServices', {});
    }
    invokeAICoreServiceWithFile(fileData, params) {
        const frmData = new FormData();
        /* for (let i = 0; i < fileData.length; i++) {
          frmData.append('excelfile', fileData[i]);
        } */
        Object.entries(params).forEach(
            ([key, value]) => {
                if (key !== 'file') {
                    if (key === 'CustomParam') {
                        frmData.append(key, JSON.stringify(params[key]));
                    } else {
                        frmData.append(key, params[key]);
                    }
                } else {
                    frmData.append(key, fileData[0]);
                }
            }
        );
        return this.api.post('InvokeAICoreServiceWithFile', frmData);
    }
    getAICoreModels(params) {
        return this.api.get('GetAICoreModels', params);
    }
    evaluate(body, params, serviceId, decimalPoint?) {
        if (serviceId === 'a433dcb8-84af-4842-9fb3-74ddbb2d4beb' || serviceId === 'e6f8243b-e00f-4d05-b16c-8c6a449b5a4c') {
            // const frmData = new FormData();
            // Object.entries(params).forEach(
            //   ([key, value]) => {
            //       if (key === 'Data') {
            //         frmData.append(key, JSON.stringify(params[key]));
            //       } else {
            //         frmData.append(key, params[key]);
            //       }
            //   }
            // );
            return this.api.post('EvaluateModel', params);
        } else if (serviceId === '93df37dc-cc72-4105-9ad2-fd08509bc823') {
            params['DecimalPlaces'] = decimalPoint;
            return this.api.post('EvaluateSingleBulkMultipleModel', params);
        } else {
            params['DecimalPlaces'] = decimalPoint;
            return this.api.post('Evaluate', body, params);
        }
    }

    invokeAICoreService(params) {
        return this.api.post('InvokeAICoreService', params);
    }

    getTextSummary(params) {
        const frmData = new FormData();
        frmData.append('ClientID', params.ClientID);
        frmData.append('DeliveryConstructID', params.DeliveryConstructID);
        frmData.append('Payload', JSON.stringify(params.Payload));
        frmData.append('ServiceID', params.ServiceID);
        frmData.append('UserID', params.UserID);
        return this.api.post('GetTextSummary', frmData);
    }

    getTextSummaryStatus(correlationId) {
        return this.api.get('GetTextSummaryStatus', { correlationid: correlationId });
    }

    getSAMultipleBulkPredictionStatus(correlationId, uniqueID) {
        return this.api.post('GetSAMultipleBulkPredictionStatus', { CorrelationId: correlationId, UniqueId: uniqueID });
    }

    getTextSummaryResult(correlationId) {
        return this.api.get('GetTextSummaryResult', { correlationId: correlationId });
    }

    invokeLangTranslationService(params) {
        return this.api.post('InvokeLangTranslationService', params);
    }

    ClusteringIngestData(fileData, params) {
        const frmData = new FormData();
        Object.entries(params).forEach(
            ([key, value]) => {
                if (key !== 'file') {
                    if (key === 'ParamArgs' || key === 'ProblemType' || key === 'SelectedModels' || key === 'selectedColumns' ||
                        key === 'StopWords' || key === 'Ngram') {
                        frmData.append(key, JSON.stringify(params[key]));
                    } else {
                        frmData.append(key, params[key]);
                    }
                }/*  else { 
          frmData.append(key, fileData[0]);
        } */
            }
        );
        return this.api.post('ClusteringIngestData', frmData);
    }

    getCusteringModels(params) {
        return this.api.get('GetCusteringModels', params);
    }

    ClusteringEvaluate(body, params) {
        return this.api.post('ClusteringEvaluate', body, params);
    }

    saveClusterMapData(body) {
        return this.api.post('UpdateMappingData', body);
    }

    GetDefaultEntityName(correlationId) {
        return this.api.get('GetDefaultEntityName', { correlationid: correlationId });
    }
    getDynamicEntity(clientUId, deliveryConstructUId, UserEmail) {
        return this.api.get('GetDynamicEntity', {
            clientUId: clientUId, 'DeliveryConstructUId': deliveryConstructUId
            , 'UserEmail': UserEmail
        });
    }

    fetchAICoreUseCases(serviceId) {
        return this.api.get('FetchUsecase', { 'serviceId': serviceId });
    }

    trainModelDeveloperPrediction(params) {
        return this.api.get('DeveloperPredictionTrain', params);
    }

    publishUseCase(params) {
        return this.api.post('SaveUsecase', params);
    }

    clusteringViewData(correlationId, modelType) {
        return this.api.get('ClusteringViewData', { correlationId: correlationId, modelType: modelType });
    }

    clusteringDownloadMappedData(params) {
        return this.api.post('DownloadMappedData', params);
    }

    downloadMappedDataStatus(correlationId, modelType, pageInfo) {
        return this.api.get('DownloadMappedDataStatus', { correlationId: correlationId, modelType: modelType, pageInfo: pageInfo });
    }

    trainAIModelsFromUseCase(params, fileData) {
        const frmData = new FormData();

        if (fileData[0].source === 'Entity') {
            frmData.append('pad', JSON.stringify(fileData[1].pad));
            frmData.append('EntitiesName', fileData[2].EntitiesName);
            // if (fileData[3] != undefined && fileData[3].hasOwnProperty('CorrelationId'))
            //   frmData.append('CorrelationId', fileData[3].CorrelationId);
        } else if (fileData[0].source === 'File') {
            if (fileData[1].fileUpload.hasOwnProperty('filepath')) {
                const files = fileData[1].fileUpload.filepath;
                if (files.length > 0) {
                    for (let j = 0; j < files.length; j++) {
                        frmData.append('excelfile', files[j]);
                    }
                }
            }

            frmData.append('pad', JSON.stringify(fileData[2].pad));
            frmData.append('EntitiesName', JSON.stringify(fileData[3].EntitiesName));
            // if (fileData[4] != undefined && fileData[4].hasOwnProperty('CorrelationId'))
            //   frmData.append('CorrelationId', fileData[4].CorrelationId);
        } else if (fileData[0].source === CustomDataTypes.Query) {
            frmData.append('CustomDataPull', fileData[0].CustomDataPull);
            frmData.append('DateColumn', fileData[0].DateColumn);
        } else if (fileData[0].source === CustomDataTypes.API) {
            frmData.append('excelfile', '{}');
            frmData.append('pad', '{}');
            frmData.append('EntitiesName', '{}');
            frmData.append('CustomDataPull', JSON.stringify(fileData[0].CustomDataPull));
        }


        frmData.append('ClientId', params['clientId']);
        frmData.append('DeliveryConstructId', params['deliveryConstructId']);
        frmData.append('ServiceId', params['serviceId']);
        frmData.append('ApplicationId', params['applicationId']);
        frmData.append('UsecaseId', params['usecaseId']);
        frmData.append('ModelName', params['modelName']);
        frmData.append('UserId', params['userId']);
        frmData.append('DataSource', params['DataSource']);
        frmData.append('DataSourceDetails', params['DataSourceDetails']);
        frmData.append('Language', params['Language']);
        frmData.append('DataSetUId', fileData[3]?.DataSetUId);

        return this.api.post('AIModelTraining', frmData);
    }

    deleteAIServiceModel(correlationId) {
        return this.api.get('DeleteAIModel', { 'correlationId': correlationId });
    }

    deleteAIServicePublishUseCase(useCaseId) {
        return this.api.get('DeleteAIUsecase', { 'usecaseId': useCaseId });
    }

    getTemplateData(correlationId) {
        return this.api.get('DownloadTemplate', { 'correlationId': correlationId });
    }

    /** Marketplace API calls :start */
    getMarketPlaceRegisteredUser(userId): Promise<any> {
        return this.api.get('GetMarketPlaceUserInfo', { userId: userId }).toPromise();
    }

    insertCertificationFlag(configureFlag) {
        return this.api.post('ConfigureCertificationFlag', '', { 'flag': configureFlag });
    }

    getConfiguredCertificationFlag() {
        return this.api.get('GetCertificationFlag', '');
    }

    getMarketpPlaceTrialUsers(userId) {
        return this.api.get('MarketPlaceTrialUserData', { userId: userId });
    }
    getMPCorrelationid() {
        return this.correlationIdMP;
    }
    /** Marketplace API calls :end */

    // Delete Apps in publish model	
    appDelete(applicationId) {
        return this.api.get('AppDelete', { applicationId: applicationId });
    }

    getWordCloudGeneration(body) {
        return this.api.post('GenerateWordCloud', body);
    }

    uploadFileschunks(fileData, params) {
        this.filedata = JSON.parse(JSON.stringify(fileData));
        const frmData = new FormData();
        const fileName = {};
        var progress = sessionStorage.getItem('progress');
        if (progress === 'Completed' || progress === null) {
            this.FileChunk = [];
        }

        // the file object itself that we will work with
        var files = fileData[1].fileUpload.filepath;

        if (this.FileChunk.length === 0) {
            var file = files[0].size;
            var BufferChunkSize = file;

            // 10485760 
            if (BufferChunkSize > 5242880) {
                BufferChunkSize = 5242880;
            }
        }
        var FileStreamPos = 0;
        // set the initial chunk length  
        var EndPos = BufferChunkSize;
        var Size = file;
        //  }
        // add to the FileChunk array until we get to the end of the file
        //  if (params.datasetid === undefined) {
        while (FileStreamPos < Size) {
            // "slice" the file from the starting position/offset, to  the required length
            this.FileChunk.push(files[0].slice(FileStreamPos, EndPos));
            FileStreamPos = EndPos; // jump by the amount read  
            EndPos = FileStreamPos + BufferChunkSize; // set next chunk length  
            this.filename = files[0].name;
            this.TotalParts = this.FileChunk.length;
            this.PartCount = 0;
        }
        // }


        var chunk;
        if (this.TotalParts > 0) {
            while (chunk = this.FileChunk.shift()) {
                sessionStorage.setItem('progress', 'Inprocess');
                this.PartCount++;
                // file name   
                var FilePartName = this.filename + ".part_" + this.PartCount + "." + this.TotalParts;

                // send the file  
                let fileIndex = 1;

                if (this.FileChunk.length === 0) {
                    sessionStorage.setItem('progress', 'Completed');
                }

                if (fileData[fileData.length - fileIndex].fileUpload.hasOwnProperty('filepath')) {
                    frmData.append('DataSetUId', params.DataSetUId);
                    frmData.append('DatasetName', params.DatasetName);
                    frmData.append('IsPrivate', params.IsPrivate);
                    frmData.append('ClientUId', params.ClientUId);
                    frmData.append('DeliveryConstructUId', params.DeliveryConstructUId);
                    frmData.append('UserId', params.UserId);
                    frmData.append('Category', params.Category);
                    frmData.append('EncryptionRequired', params.EncryptionRequired);
                    frmData.append('SourceName', params.SourceName);
                    frmData.append('excelfile', chunk, FilePartName);
                    if (files.length) {
                        const pattern = /[/\\?%*:|"<>]/g;
                        const specialCharaterCheck = pattern.test(files[0].name);
                        const fileExtensionCheck = files[0].name.split('.');
                        if (specialCharaterCheck || fileExtensionCheck.length > 2) {
                            return throwError({ error: { Category: { Message: 'Invalid file name' } } });
                        }
                    }
                    //  console.log(frmData);
                    return this.api.postFile('UploadDataSet', '', frmData, '').pipe(
                        tap(data => {
                            const uploadedData = this.coreUtilsService.isNil(data.body) ? data : data.body[0];
                            //this.ls.setLocalStorageData('correlationId', uploadedData);
                        }),
                    );
                }
            }
        }
    }

    getDatasetdetails(clientId, deliveryConstructId, userId, tilecategory) {
        if (tilecategory === 'API') {
            tilecategory = 'ExternalAPI';
        }
        return this.api.get('GetDataSets', { clientUId: clientId, deliveryConstructUId: deliveryConstructId, userId: userId, sourcename: tilecategory });
    }

    downloaddatasource(DataSetUId) {
        return this.http.get(this.envService.environment.ingrainAPIURL + "" + 'DownloadDataSetFile?dataSetUId=' + DataSetUId);
    }

    deletedataset(dataSetUId, userId) {
        return this.api.get('DeleteDataSet', { dataSetUId: dataSetUId, userId: userId })
    }

    deletewordcloud(correlationId) {
        return this.api.get('DeleteWordCloud', { correlationId: correlationId });
    }

    showdatasetdetais(dataSetUId, decimalPoint) {
        return this.api.get('ViewDataSet', { dataSetUId: dataSetUId, DecimalPlaces: decimalPoint });
    }

    showdatasetdropdown(clientUId, deliveryConstructUId, userId) {
        return this.api.get('GetCompletedDataSets', { clientUID: clientUId, deliveryConstructUId: deliveryConstructUId, userId: userId });
    }
    sendapipayload(params) {
        let AzureCredentials;
        // if (params.startdate === undefined && params.enddate === undefined) {
        //   params.startdate = '';
        //   params.enddate = '';
        // }

        const frmData = new FormData();
        if (params.clientid !== undefined && params.clientsecret !== undefined && params.resource !== undefined && params.grantype !== undefined) {
            AzureCredentials = {
                'client_id': params.clientid,
                'client_secret': params.clientsecret,
                'resource': params.resource,
                'grant_type': params.grantype
            };
        } else {
            AzureCredentials = {
                'client_id': '',
                'client_secret': '',
                'resource': '',
                'grant_type': ''
            };
        }

        var bodypayload = {
            'DataSetName': params.DatasetName,
            'ClientUId': params.ClientUId,
            'DeliveryConstructUId': params.DeliveryConstructUId,
            'Category': '',
            'IsPrivate': params.IsPrivate,
            'EnableIncrementalFetch': params.EnableIncrementalFetch,
            'HttpMethod': params.HttpMethod,
            'Url': params.Url,
            'AuthType': params.authttype,
            'Token': params.token,
            'Headers': params.Keyvalue,
            'Body': params.Body,
            'AzureUrl': params.tokenurl,
            'AzureCredentials': AzureCredentials,
            'StartDate': params.startdate,
            'EndDate': params.enddate,
            'UserId': params.userid,
            'EncryptionRequired': params.Encryption
        };

        // frmData.append('DataSetName', params.DatasetName);
        // frmData.append('ClientUId', params.ClientUId);
        // frmData.append('DeliveryConstructUId', params.DeliveryConstructUId);
        // frmData.append('Category', '');
        // frmData.append('IsPrivate', params.IsPrivate);
        // frmData.append('EnableIncrementalFetch',params.EnableIncrementalFetch);
        // frmData.append('HttpMethod', params.HttpMethod);
        // frmData.append('Url', params.Url);
        // frmData.append('AuthType', params.authttype);
        // frmData.append('Token', params.token); 
        // frmData.append('Headers', JSON.stringify(params.Keyvalue));
        // frmData.append('Body', params.Body.trim());
        // frmData.append('AzureUrl', params.tokenurl);
        // frmData.append('AzureCredentials', JSON.stringify(AzureCredentials));
        // frmData.append('StartDate', params.startdate);
        // frmData.append('EndDate', params.enddate);
        // frmData.append('UserId', params.userid);
        return this.api.post('UploadExternalAPIDataSet', bodypayload, {});

    }

    getFMModelsStatus(clientid, dcid, userid) {
        return this.api.get('GetFMModelsStatus', { clientid: clientid, dcid: dcid, userid: userid });
    }


    getVisulalisationDataStatus(correlationId, selectedModel) {
        // correlationId=a82730bd-10c5-4f64-89b7-c78b090ae6ac&modelType=KMeans&pageInfo=Clustering_Visualization
        return this.api.get('VisulalisationDataStatus',
            { correlationId: correlationId, modelType: selectedModel, pageInfo: 'Clustering_Visualization' });
    }


    getVisualizationData(bodypayload) {
        return this.api.post('VisualizationData', bodypayload, {});
    }

    getUserNotificationMessagesData() {
        return this.api.getPheonix('/v1/UserNotificationMessages', {});
    }

    getBulkPrediction(bodypayload) {
        return this.api.post('GetSimilarityPredictions', bodypayload, {});
    }

    getWindowServiceStatus() {
        return this.api.get('GetWindowServiceStatus', {});
    }

    GetSimilarityRecordCount(correlationId) {
        return this.api.get('GetSimilarityRecordCount', { correlationid: correlationId });
    }

    GetClusteringRecordCount(correlationId, uploadType, DataSetUId) {
        return this.api.get('GetClusteringRecordCount', { correlationid: correlationId, UploadType: uploadType, DataSetUId: DataSetUId });
    }

}
