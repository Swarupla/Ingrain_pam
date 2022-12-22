import { Injectable } from '@angular/core';
import { ApiService } from 'src/app/_services/api.service';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';
import { LocalStorageService } from 'src/app/_services/local-storage.service';

@Injectable({
  providedIn: 'root'
})
export class ModelMonitoringService {


  constructor(private api: ApiService, private ls: LocalStorageService, private coreUtilsService: CoreUtilsService) { }

  getModelData() {
    const userId = sessionStorage.getItem('userId');
    const clientid = sessionStorage.getItem('clientID');
    const dcid = sessionStorage.getItem('dcID');
    const correlationId = this.ls.getCorrelationId();
    // clientid='00100000-0000-0000-0000-000000000000';
    // dcid='1e4272c7-d9e6-4df8-b3da-da3d056f35a1';
    // correlationId='94e3ba11-9456-4863-8809-ace3f9e73730';
    return this.api.get('ModelMetrics', { clientid: clientid, dcid: dcid, correlationId: correlationId });
  }

  getTrainedHistory( ) {
    const userId = sessionStorage.getItem('userId');
    const clientid = sessionStorage.getItem('clientID');
    const dcid = sessionStorage.getItem('dcID');
    const correlationId = this.ls.getCorrelationId();
    return this.api.get('TrainedModelHistroy', { clientid: clientid, dcid: dcid, correlationId: correlationId });
}


}
