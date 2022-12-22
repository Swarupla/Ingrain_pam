import { Injectable } from '@angular/core';
import { ApiService } from './api.service';

@Injectable({
  providedIn: 'root'
})
export class CascadeModelsService {

  constructor(private api: ApiService) { }

  getCascadingModels(clientUid, dcUid, userId, category, cascadedId) {
    return this.api.get('GetCascadingModels', {
      clientUid: clientUid,
      dcUid: dcUid,
      userId: userId,
      category: category,
      cascadedId: cascadedId });
  }

  saveCascadeModels(payload) {
    return this.api.post('SaveCascadeModels', payload);
  }

  getCascadeModelMapping(cascadedId) {
    return this.api.get('GetCascadeModelMapping', {cascadedId: cascadedId});
  }

  updateCascadeMapping(payload) {
    return this.api.post('UpdateCascadeMapping', payload);
  }

  getCascadeDeployedModel(cascadedId) {
    return this.api.get('GetCascadeDeployedModel', { cascadedId: cascadedId });
  }

  getCustomCascadeModels(clientUid, dcUid, userId, category) {
    return this.api.get('GetCustomCascadeModels', {
      userId: userId,
      clientUid: clientUid,
      dcUid: dcUid,
      category: category });
  }

  getCascadeIdDetails(sourceCorid, targetCorid, cascadeId, UniqIdName, UniqDatatype, TargetColumn) {
    return this.api.get('GetCascadeIdDetails', {
      sourceCorid: sourceCorid,
      targetCorid: targetCorid,
      cascadeId: cascadeId,
      UniqIdName: UniqIdName,
      UniqDatatype: UniqDatatype,
      TargetColumn: TargetColumn });
  }

  saveCustomCascadeModels(payload) {
    return this.api.post('SaveCustomCascadeModels', payload);
  }

  getCustomCascadeDetails(cascadeId) {
    return this.api.get('GetCustomCascadeDetails', {cascadeId: cascadeId });
  }
}
