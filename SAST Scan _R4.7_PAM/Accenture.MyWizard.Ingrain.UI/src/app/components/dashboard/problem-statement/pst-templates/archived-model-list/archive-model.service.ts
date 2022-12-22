import { Injectable } from '@angular/core';
import { ApiService } from 'src/app/_services/api.service';

@Injectable({
  providedIn: 'root'
})
export class ArchiveModelService {

  constructor(private api: ApiService,) { }

  getArchivalModelList(userId : string, DeliveryConstructUID : string, ClientUId : string) {
    return this.api.get('GetArchivedRecords', {userId : userId, DeliveryConstructUID : DeliveryConstructUID, ClientUId : ClientUId });
  }

  retrivePurgedModel(correlationId) {
    return this.api.get('RetrieveArchiveModel', { correlationId: correlationId })
  }
}
