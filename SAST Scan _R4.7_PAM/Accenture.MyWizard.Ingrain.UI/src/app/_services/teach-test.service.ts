import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { tap } from 'rxjs/operators';
import { Subject, BehaviorSubject, throwError } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class TeachTestService {

  whatIfAnalysisData: {} = {};
  private whatIfAnalysisSubject: Subject<{}> = new BehaviorSubject(this.whatIfAnalysisData);
  public EmittedWhatIfAnalysis$ = this.whatIfAnalysisSubject.asObservable();

  constructor(private api: ApiService) { }



  SetWhatIfAnalysisData(data) {
    Object.assign(this.whatIfAnalysisData, data);
    this.emitWhatIfAnalysis(data);
  }
  // get teach and Test Data
  getTeachTestData(correlationId, modelType, timeSeriesSteps, modelName) {
    if (modelType === 'TimeSeries') {
      return this.api.get('GetTeachTestDataforTS',
      { correlationId: correlationId, ModelType: modelType, TimeSeriesSteps: timeSeriesSteps, modelName: modelName });
    } else {
      return this.api.get('GetTeachTestData', { correlationId: correlationId, modelName: modelName });
    }
  }

  getWhaIfAnalysisData() {
    return this.whatIfAnalysisData;
  }
  emitWhatIfAnalysis(change) {
    Object.assign(this.whatIfAnalysisData, change);
    this.whatIfAnalysisSubject.next(this.whatIfAnalysisData);
  }

  getTeachAndTestScenario(correlationId, wfId, isTimeSeries, scenario) {
    return this.api.get('GetTeachModels', {
      correlationId: correlationId,
      WFId: wfId,
      IstimeSeries: isTimeSeries,
      scenario: scenario
    });
  }

  // update test Data
  uploadTeachAndtestFiles(fileData, params) {
    const frmData = new FormData();
    for (let i = 0; i < fileData.length; i++) {
      frmData.append('excelfile', fileData[i]);
    }
    if (fileData.length) {
      const pattern = /[/\\?%*:|"<>]/g;
      const specialCharaterCheck = pattern.test(fileData[0].name);
      const fileExtensionCheck = fileData[0].name.split('.');
      if (specialCharaterCheck || fileExtensionCheck.length > 2) {
        return throwError({error: 'Invalid file name'});
      }
    }
    return this.api.postFile('UploadTestData', fileData[0], frmData, params).pipe(
      tap(data => data
      ),
    );
  }

  // run Test and Run Bulktest
  postWhatIfAnalysisFeatures(correlationId, userId, features, modelName, isBulkData, modelType, WFId?: string) {
    let data;
    if (modelType === 'TimeSeries') {
      data = {
        'CorrelationId': correlationId,
        'createdByUser': userId,
        'Steps': features,
        'model': modelName,
        'bulkData': isBulkData,
        'WFId': WFId
      };
    } else {
      data = {
        'CorrelationId': correlationId,
        'createdByUser': userId,
        'Features': features,
        'model': modelName,
        'bulkData': isBulkData,
        'WFId': WFId
      };
    }
    return this.api.post('RunTest', data);
  }

  // Save test Cases
  saveTestResults(testScenarioName, correlationId, wfId, scenario) {
    const data = {
      'CorrelationId': correlationId,
      'WFId': wfId,
      // "Temp": "False",
      'SenarioName': testScenarioName,
      'scenario' : scenario
    };
    return this.api.post('SaveTestResults', data);
  }
  GetHyperTunedDataByVersion(hypertunedata) {
    return this.api.get('GetHyperTunedDataByVersion',
    { CorrelationId: hypertunedata.CorrelationId, HyperTuneId: hypertunedata.HTId, VersionName: hypertunedata.VersionName });
  }
  savePrescriptiveAnalytics(requestPayload) {
    return this.api.post('PrescriptiveAnalytics', requestPayload);
  }
  deletePrescriptiveAnalytics(requestPayload) {
    return this.api.delete('DeletePrescriptiveAnalytics', requestPayload);
  }
}
