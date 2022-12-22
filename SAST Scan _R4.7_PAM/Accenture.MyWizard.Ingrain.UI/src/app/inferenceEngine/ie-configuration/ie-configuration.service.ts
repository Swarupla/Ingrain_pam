import { Injectable } from '@angular/core';
import { ApiService } from 'src/app/_services/api.service';
import { tap } from 'rxjs/operators';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';
import { LocalStorageService } from 'src/app/_services/local-storage.service';
import { Observable } from 'rxjs';



@Injectable({
  providedIn: 'root'
})
export class IeConfigurationService {
  params;
  autoSampleGeneratedIE = [];
  application = {
      "ApplicationName": "",
      "ApplicationID": ""
  };
  constructor(private api: ApiService, private coreUtilsService: CoreUtilsService, private ls: LocalStorageService) { }

  // api/GetDateMeasureAttribute?correlationId=04062021-ioincidentdata
  getDateMeasureAttribute(correlationId): Observable<any> {
    return this.api.get('GetDateMeasureAttribute', { 'correlationId': correlationId })
  }

  // api/ViewConfiguration?correlationId=abcdefg&InferenceConfigId=7e9523f0-d3e3-46a3-972a-11111111
  viewConfiguration(params) {
    return this.api.get('ViewConfiguration', params);
  }

  postTriggerFeatureCombination(body, params) {
    // POST ::api/TriggerFeatureCombination?correlationId=correlationidNew3&userId=mywizardsystemdataadmin@mwphoenix.onmicrosoft.com
    //     Body
    // {
    //     "Metric": "SLA Resolution",
    //     "date": "Creation Date Time"
    // }
    return this.api.post('TriggerFeatureCombination', body, params)
  }

  getGetFeatureCombination(data) {
    // GetFeatureCombination
    return this.api.get('GetFeatureCombination', data);
  }


  postSaveConfig(body, params) {
    // api/SaveIEConfig
    return this.api.post('SaveIEConfig', body, params)
  }

  viewIEData(params) {
    // si.accenture.com/ingrain/api/ViewIEData?
    return this.api.get('ViewIEData', params);
  }
}


