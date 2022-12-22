import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { AppUtilsService } from './app-utils.service';

@Injectable({
  providedIn: 'root'
})
export class ProcessedDataService {

  constructor(private api: ApiService, private au: AppUtilsService) {}

  getStatus() {
    return this.api.get('ProcessDataForModelling', {
      'correlationId': '40340faa-e666-48bc-81b6-e2e59c825a96',
      'pageInfo': 'PreProcess',
      'userId': this.au.getCookies().UserId
    });
  }
}
