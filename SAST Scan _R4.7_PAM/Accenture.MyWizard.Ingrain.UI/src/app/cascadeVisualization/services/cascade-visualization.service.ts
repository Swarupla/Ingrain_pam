import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { throwError } from 'rxjs';
import { tap } from 'rxjs/operators';
import { ApiService } from 'src/app/_services/api.service';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';
import { EnvironmentService } from 'src/app/_services/EnvironmentService';
import { LocalStorageService } from 'src/app/_services/local-storage.service';

interface ICascadeVisData {
  clientUID: string;
  deliveryConstructUId: string;
  categoryType: string;
  problemType: string;
  dataForDownload: any;
  cascadedId: string;
  uniqueId: string;
  modelName: string;
  isOnlyFileupload: any;
  isBoth: any;
  isonlySingleEntity: any;
  dataUploadValidationMessage: string;
  isMultipleEntities: any;
}

@Injectable({
  providedIn: 'root'
})
export class CascadeVisualizationService {
  filedata: any;
  public cascadeVisData = {} as ICascadeVisData;
  constructor(private api: ApiService, private ls: LocalStorageService, private coreUtilsService: CoreUtilsService,
    private envService: EnvironmentService, private http: HttpClient) { }

    getCascadeVisData() {
      return this.cascadeVisData;
    }

  getCascadeInfluencers(cascadedId) {
    return this.api.get('GetCascadeInfluencers', { cascadedId: cascadedId });
  }

  uploadData(fileData, params) {
    const frmData = new FormData();

    frmData.append('UserId', params.UserId);
    frmData.append('IsFileUpload', params.IsFileUpload);
    frmData.append('CascadedId', params.CascadedId);
    frmData.append('ModelName', params.ModelName);
    frmData.append('ClientUID', params.ClientUID);
    frmData.append('DCUID', params.DCUID);

      if (fileData.length > 0) {
          frmData.append('excelfile', fileData[0]);
      }
    // NA defect fix - For Upload progress message
    if (fileData.length) {
      const pattern = /[/\\?%*:|"<>]/g;
      const specialCharaterCheck = pattern.test(fileData[0].name);
      const fileExtensionCheck = fileData[0].name.split('.');
      if (specialCharaterCheck || fileExtensionCheck.length > 2) {
        return throwError({error: 'Invalid file name'});
      }
    }
    const fileuploadedDetails = '';
    return this.api.postFile('UploadData', fileuploadedDetails, frmData, params).pipe(
      tap(data => {
        const uploadedData = this.coreUtilsService.isNil(data.body) ? data : data.body[0];
      }),
    );
  }

  getCascadeVisualization(cascadedId, uniqueId) {
    return this.api.get('GetCascadeVisualization', { CascadedId: cascadedId, UniqueId: uniqueId });
  }

  showCascadeData(cascadedId, uniqueId) {
    return this.api.get('ShowCascadeData', { CascadedId: cascadedId, UniqueId: uniqueId });
  }
}
