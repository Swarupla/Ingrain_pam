import { Injectable } from '@angular/core';
import { throwError } from 'rxjs';
import { tap, isEmpty } from 'rxjs/operators';
import { ApiService } from './api.service';
import { CoreUtilsService } from './core-utils.service';
import { LocalStorageService } from './local-storage.service';

@Injectable({
  providedIn: 'root'
})
export class AdProblemStatementService {
  filedata: any;
  targetColumn: any;
  uniqueIdentifier: any;
  timeSeriesColumn: any;
  getColumnsdata: any;
  uploadedfileName: any;
  public isPredefinedTemplate = 'False';
  public isMappedFromTemplate = 'False';

  constructor(private api: ApiService, private coreUtilsService: CoreUtilsService,
    private ls: LocalStorageService) { }

  getExistingModelNameAD(modelname) {
    return this.api.get('GetExistingModelNameAD', { modelName: modelname.toString().trim()});
  }

  uploadFilesAD(fileData, params) {
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
    return this.api.postFile('FileUploadAD',fileuploadedDetails,frmData,params).pipe(
      tap(data => {
        const uploadedData = this.coreUtilsService.isNil(data.body) ? data : data.body[0];
        this.ls.setLocalStorageData('correlationId', uploadedData);
      }),
    );
  }

  /**
   * store  columns data for later usage.
   * @param data
   */
  storeData(data) {
    this.getColumnsdata = data;
  }

  /**
   * 
   * @param targetcolumn 
   */
  setTargetColumn(targetcolumn) {
    this.targetColumn = targetcolumn;
    localStorage.setItem('targetcolumnAD', this.targetColumn);
  }

  /**
   * method to get Target Columns.
   * @returns 
   */
  getTargetColumn() {
    this.targetColumn = this.targetColumn ? this.targetColumn : localStorage.getItem('targetcolumnAD');
    return this.targetColumn;
  }

  /**
   * Method to set Unique Identifier
   * @param uniqueIdentifier 
   */
  setUniqueIdentifier(uniqueIdentifier) {
    this.uniqueIdentifier = uniqueIdentifier;
    localStorage.setItem('uniqueIdentifierAD', this.uniqueIdentifier);
  }

  /**
   * Method to get Unique Identifier
   * @returns 
   */
  getUniqueIdentifier() {
    this.uniqueIdentifier = this.uniqueIdentifier ? this.uniqueIdentifier : localStorage.getItem('uniqueIdentifierAD');
    return this.uniqueIdentifier;
  }
  /**
   * Method to set time series column.
   * @param timeSeriesColumn 
   */
  setTimeSeriesColumn(timeSeriesColumn) {
    this.timeSeriesColumn = timeSeriesColumn;
    localStorage.setItem('timeSeriesColumnAD', this.timeSeriesColumn);
  }

  /**
   *  Method to get time series column.
   * @returns 
   */
  getTimeSeriesColumn() {
    this.timeSeriesColumn = this.timeSeriesColumn ? this.timeSeriesColumn : localStorage.getItem('timeSeriesColumnAD');
    return this.timeSeriesColumn;
  }

  /**
   * Function to get columns.
   * @param correlationId 
   * @param isExistingTemplate 
   * @param isNewModel 
   * @returns 
   */
  getColumnsAD(correlationId, isExistingTemplate, isNewModel) {
    return this.api.get('GetColumnsAD', { correlationId: correlationId, IsTemplate: isExistingTemplate, newModel: isNewModel });
  }

  /**
   * Function to get columns for my models.
   * @param correlationId 
   * @returns 
   */
  getColumnsForMyModels(correlationId) {
    return this.api.get('GetColumnsAD', { correlationId: correlationId, IsTemplate: false, newModel: false });
  }

  /**
   * Method to get uploaded data.
   * @param correlationId 
   * @param decimalPoint 
   * @returns 
   */
  getViewUploadedData(correlationId, decimalPoint) {
    return this.api.get('ViewUploadedDataAD', { correlationId: correlationId, DecimalPlaces: decimalPoint });
  }

  /**
   * 
   */
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
    return this.api.post('PostColumnsAD', data);
  }


  /**
   * method to check the status of Data Clean Up and Data Preprocessing status.
   * @param CorrelationID
   * @param pageInfo
   */
  GetStatusForDEAndDTProcess(correlationId, pageInfo, userId) {
    return this.api.get('GetStatusForDEAndDTProcess', { correlationId: correlationId, pageInfo: pageInfo, userId : userId });
  }

  /**
   * method to clone the model.
   * @param correlationId 
   * @param name 
   * @returns 
   */
  cloneAD(correlationId, name) {
    const userId = sessionStorage.getItem('userId');
    const clientId = sessionStorage.getItem('clientID');
    const dcId = sessionStorage.getItem('dcID');
    return this.api.get('CloneDataAD', { correlationId, modelName: name, userId: userId, clientUId: clientId, deliveryConstructUID: dcId });
  }

  /**
   * method to delete model based on correlationId.
   * @param correlationId 
   * @returns 
   */
  deleteModelByCorrelationId(correlationId) {
    return this.api.get('FlushModelAD', { 'correlationId': correlationId });
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
    return this.api.postFile('DataSourceFileUploadAD', fileuploadedDetails, frmData, params);
  }

  getDeployAppsDetailsAD(correlationId, clientUId, deliveryConstructUID) {
    let environment = sessionStorage.getItem('Environment');
    if (environment === null) {
      environment = sessionStorage.getItem('fromSource');
      if (!this.coreUtilsService.isNil(environment)) {
        environment = environment.toUpperCase();
      }
    }
    return this.api.get('GetAllAppDetailsAD', {
      clientUId: clientUId,
      deliveryConstructUID: deliveryConstructUID,
      CorrelationId: correlationId,
      Environment: environment
    });
  }

  saveNewAppAD(data: any) {
    return this.api.post('AddNewAppAD', data);
  }

  getPublicTemplates(Templates, userId, category, dateFilter, deliveryConstructUID, clientUId) {
    return this.api.get('GetTemplateModelsAD',
      {
        'Templates': Templates, 'userId': userId, 'category': category,
        'dateFilter': dateFilter, 'DeliveryConstructUID': deliveryConstructUID, 'ClientUId': clientUId
      });
  }

  getModelsAD(Templates, userId, category, dateFilter, deliveryConstructUID, clientUId) {
    return this.api.get('GetTemplateModelsAD',
      {
        'Templates': Templates, 'userId': userId, 'category': category,
        'dateFilter': dateFilter, 'DeliveryConstructUID': deliveryConstructUID, 'ClientUId': clientUId
      });
  }

  getDynamicEntity(clientUId, deliveryConstructUId, UserEmail) {
    return this.api.get('GetDynamicEntity', {
      clientUId: clientUId, 'DeliveryConstructUId': deliveryConstructUId
      , 'UserEmail': UserEmail
    });
  }

}
