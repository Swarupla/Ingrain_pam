import { Injectable } from '@angular/core';
import { ApiService } from 'src/app/_services/api.service';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';
import { tap } from 'rxjs/operators';
import { throwError } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class FmVisualizationServiceService {

  constructor(private api: ApiService, private coreUtilsService: CoreUtilsService) { }

  getFMVisualizationDetails(clientId, dcId, userId, category) {
    return this.api.get('GetFMVisualizationDetails', { ClientUID: clientId, DCUID: dcId, UserID: userId, Category: category });
  }

  fmFileUpload(fileData, params) {
    const frmData = new FormData();
    frmData.append('UserId', params.UserId);
    frmData.append('ClientUID', params.ClientUID);
    frmData.append('DCUID', params.DCUID);
    frmData.append('Category', params.Category);
    frmData.append('IsRefresh', params.IsRefresh);
    frmData.append('CorrelationId', params.CorrelationId);
    if (fileData.length > 0) {
        frmData.append('excelfile', fileData[0]);
    }
    if (fileData.length) {
      const pattern = /[/\\?%*:|"<>]/g;
      const specialCharaterCheck = pattern.test(fileData[0].name);
      const fileExtensionCheck = fileData[0].name.split('.');
      if (specialCharaterCheck || fileExtensionCheck.length > 2) {
        return throwError({error: 'Invalid file name'});
      }
    }
    // NA defect fix - For Upload progress message
    const fileuploadedDetails = '';
    return this.api.postFile('FMFileUpload', fileuploadedDetails, frmData, params).pipe(
      tap(data => {
        const uploadedData = this.coreUtilsService.isNil(data.body) ? data : data.body[0];
      }),
    );
  }

  fmModelTrainingStatus(correlationId, fmCorrelationId, userId) {
    return this.api.get('FMModelTrainingStatus', { correlationId: correlationId, FMCorrelationId: fmCorrelationId, userId: userId });
  }

  getFMVisualizationPrediction(correlationId, uniqueId) {
    return this.api.get('GetFMVisualizationPrediction', { correlationId: correlationId, uniqId: uniqueId });
  }

  fmModelsDelete(correlationId, fmCorrelationId) {
    return this.api.get('FMModelsDelete', { correlationId: correlationId, fmCorrelationId: fmCorrelationId });
  }

  getTemplateData(useCaseId) {
    return this.api.get('DownloadTemplate', { 'correlationId': useCaseId });
  }
}
