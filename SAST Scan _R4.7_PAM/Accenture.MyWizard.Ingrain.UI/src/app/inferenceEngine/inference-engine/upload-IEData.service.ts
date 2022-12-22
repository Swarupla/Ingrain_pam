import { Injectable } from '@angular/core';
import { ApiService } from 'src/app/_services/api.service';
import { tap } from 'rxjs/operators';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';
import { LocalStorageService } from 'src/app/_services/local-storage.service';
import { throwError } from 'rxjs';


@Injectable({
  providedIn: 'root'
})
export class UploadIEDataService {

  constructor(private api: ApiService, private coreUtilsService: CoreUtilsService, private ls: LocalStorageService) { }
  filedata: any;
  /**
   * 
   * @param fileData 
   * @param params 
   * @returns Response file upload status
   */
  ingestIEData(fileData: any, params: any, source, FileEntityName?) {
    this.filedata = JSON.parse(JSON.stringify(fileData));
    // this.filedata = fileData;
    const frmData = new FormData();
    const fileName = {};
    if (fileData[0].parentFileName !== undefined) {
      fileName['name'] = fileData[0].parentFileName;
    } else {
      fileName['name'] = fileData[0].source;
    }

    for (const index in fileData) {
      if (fileData) {
        if (fileData[index].hasOwnProperty('pad')) {
          frmData.append('pad', JSON.stringify(this.filedata[Number(index)].pad));
        }
        if (fileData[index].hasOwnProperty('InstaMl')) {
          frmData.append('InstaMl', JSON.stringify(this.filedata[Number(index)].InstaMl));
        }
        if (fileData[index].hasOwnProperty('metrics')) {
          frmData.append('metrics', JSON.stringify(this.filedata[Number(index)].metrics));
        }

        if (fileData[index].hasOwnProperty('EntitiesName')) {
          if (this.coreUtilsService.isEmptyObject(this.filedata[Number(index)].EntitiesName)) {
            frmData.append('EntitiesName', JSON.stringify(this.filedata[Number(index)].EntitiesName));
          } else {
            frmData.append('EntitiesName', this.filedata[Number(index)].EntitiesName);
          }
        }

        if (fileData[index].hasOwnProperty('MetricNames')) {
          if (this.coreUtilsService.isEmptyObject(this.filedata[Number(index)].MetricNames)) {
            frmData.append('MetricNames', JSON.stringify(this.filedata[Number(index)].MetricNames));
          } else {
            frmData.append('MetricNames', this.filedata[Number(index)].MetricNames);
          }
        }
        if (fileData[index].hasOwnProperty('Custom')) {
          frmData.append('Custom', JSON.stringify(this.filedata[Number(index)].Custom));
        }
      }
    }

    if (fileData[0]["DatasetUId"] !== undefined) {
      frmData.append('DataSetUId', fileData[0]["DatasetUId"]);
    }

    let fileIndex = 3;
    if (sessionStorage.getItem('Environment') === 'FDS' && (sessionStorage.getItem('RequestType') === 'AM' || sessionStorage.getItem('RequestType') === 'IO')) {
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

    if (source === 'File') {
      frmData.append('FileEntityName', FileEntityName);
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
        return throwError({error: { Category : { Message : 'Invalid file name'}}});
      }
    }
    return this.api.postFile('IEIngestData', fileuploadedDetails, frmData, params).pipe(
      tap(data => {
        const uploadedData = this.coreUtilsService.isNil(data.body) ? data : data.body[0];
        this.ls.setLocalStorageData('correlationId', uploadedData);
      }),
    );

  }


  /**
   * 
   * @param correlationId 
   * @param regenerate 
   * @returns AutoGenerateInferences Status
   */
  autoGenerateInferences(correlationId: string, regenerate: boolean) {
    // correlationId=18e714ca-e137-4065-93b6-5fbd2329f655&regenerate=false
    return this.api.get('AutoGenerateInferences', { 'correlationId': correlationId, 'regenerate': regenerate });
  }

  /**
   * 
   * @param clientId 
   * @param dCId 
   * @param userId 
   * @returns IEModel list
   */
  getIEModel(clientId: string, dCId: string, userId: string) {
    const Environment = (this.coreUtilsService.isNil(sessionStorage.getItem('Environment')) == false) ? sessionStorage.getItem('Environment') : sessionStorage.getItem('env');
    const RequestType = sessionStorage.getItem('RequestType');
    if (Environment === 'PAM' || Environment === 'FDS') {
      return this.api.get('GetIEModel', { 'clientId': clientId, 'dCId': dCId, 'userId': userId, 'FunctionalArea': RequestType });
    } else {
      return this.api.get('GetIEModel', { 'clientId': clientId, 'dCId': dCId, 'userId': userId });
    }
  }



  checkIEModel(params) {
    // CheckIEModelName
    return this.api.get('CheckIEModelName', params);
  }

}
