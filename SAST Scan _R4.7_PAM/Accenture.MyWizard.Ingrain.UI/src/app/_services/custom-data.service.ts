import { Injectable } from '@angular/core';
import { ApiService } from 'src/app/_services/api.service';
import { AppUtilsService } from 'src/app/_services/app-utils.service';

@Injectable({
  providedIn: 'root',
})

export class CustomDataService {

  constructor(private apiService: ApiService, private apputilService: AppUtilsService) { }

  //Add-Query custom methods region starts.
  getQueryResponse(dialogConfigData, query,serviceLevel) {

    this.apputilService.loadingStarted();
    let formData = new FormData();

    let params = {
      userId: dialogConfigData.userId,
      clientId: dialogConfigData.clientUID,
      deliveryId: dialogConfigData.deliveryConstructUID,
      category: dialogConfigData.deliveryTypeName,
      serviceLevel : serviceLevel
    }
    formData.append('CustomDataPull', query);
    return this.apiService.post('TestQueryResponse', formData, params);
  }
  //Add-Query custom methods region Ends.
}
