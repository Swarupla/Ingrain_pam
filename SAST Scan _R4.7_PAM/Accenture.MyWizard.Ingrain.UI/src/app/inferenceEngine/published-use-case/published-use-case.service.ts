import { Injectable } from '@angular/core';
import { ApiService } from 'src/app/_services/api.service';
import { tap } from 'rxjs/operators';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';
import { LocalStorageService } from 'src/app/_services/local-storage.service';


@Injectable({
  providedIn: 'root'
})
export class PublishedUseCaseService {

  constructor(private api: ApiService, private coreUtilsService: CoreUtilsService, private ls: LocalStorageService) { }

 
  // GET:::api/DeleteUseCase?useCaseId=eb54e08d-373b-4341-8408-c6cca080379d
  deleteUseCase(useCaseId) {
   return this.api.get('DeleteIEUseCase',{ 'useCaseId': useCaseId });
  }

  // api/GetUseCaseList
  getUseCaseList() {
    return this.api.get('GetUseCaseList', {});
  }

  // /api/CreateUseCase
  createUseCase(body) {
   return this.api.post('CreateUseCase', body);
  }

  trainUseCase(payload) {
    return this.api.post('TrainUsecase', payload )
  }
}

