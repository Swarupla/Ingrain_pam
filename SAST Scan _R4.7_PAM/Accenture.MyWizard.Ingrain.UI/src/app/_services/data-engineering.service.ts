import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { Subscription, Subject, BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class DataEngineeringService {

  private applyFlagSubscription: BehaviorSubject<boolean> = new BehaviorSubject(false);
  public applyFlag$ = this.applyFlagSubscription.asObservable();
  public TextDataPreprocessing = {};
  public deleteByUser;

  constructor(private api: ApiService) { }

  postPreProcessData(dataToServer: {}) {
    return this.api.post('PostPreProcessingData', dataToServer);
  }

  getDataCleanUp(correlationId, userId, pageInfo) {
    return this.api.get('ProcessDataForModelling', { 'correlationId': correlationId, 'userId': userId, 'pageInfo': pageInfo });
  }

  apply(correlationId: string, userId: string, pageInfo: string, data) {
    return this.api.post('ApplyPreProcessingData', data, { correlationId, userId, pageInfo });
  }


  getViewData(correlationId: string, userId: any, pageInfo: any) {
    return this.api.get('GetViewData', { correlationId, userId, pageInfo });
  }

  getPreprocessData(correlationId) {
    return this.api.get('GetPreProcessData', { correlationId });
  }

  getShowData(correlationId, noOfRecord, showAllRecord, problemType, decimalPoint) {
    return this.api.get('GetPreProcessedData', { correlationId, noOfRecord, showAllRecord, problemType, DecimalPlaces: decimalPoint });
  }

  setApplyFlag(applyFlag) {
    this.applyFlagSubscription.next(applyFlag);
  }


  postDataCleanup(selectedDataTypes, selectedcleanupScales, correlationId, DtypeModifiedColumns, ScaleModifiedColumns, userId, 
    pageInfo, DateFormatColumns) {
    const data = {
      'datatypes': selectedDataTypes,
      'scales': selectedcleanupScales,
      'correlationId': correlationId,
      'DtypeModifiedColumns': DtypeModifiedColumns,
      'ScaleModifiedColumns': ScaleModifiedColumns,
      'pageInfo': pageInfo,
      'userId': userId,
      // 'DateFormatColumns': DateFormatColumns // DateFormat is on hold
    };
    return this.api.post('PostCleanedData', data);
  }

  fixColumns(correlationId, featuresNamestobeFixed) {
    const prescriptionComlumnsToRemove = Array.from(featuresNamestobeFixed);
    return this.api.get('RemovePrescriptionColumns',
      { 'correlationId': correlationId, 'prescriptionColumns': prescriptionComlumnsToRemove });
  }

  
  fixColumnsDataTransformation(correlationId) {
    // const prescriptionComlumnsToRemove = Array.from(featuresNamestobeFixed); ?correlationId=c1c576fb-4050-4b48-b0c5-de4fda61905c
    return this.api.post('SmoteTestTechnique?', {} , { correlationId});
  }

  clone(correlationId, name) {
    const userId = sessionStorage.getItem('userId');
    const clientId = sessionStorage.getItem('clientID');
    const dcId = sessionStorage.getItem('dcID');
    return this.api.get('CloneData', { correlationId, modelName: name, userId: userId, clientUId: clientId, deliveryConstructUID: dcId });
  }

  postManualAddFeature(correlationid: string, requestPayload) {
    const formData = new FormData();
    formData.append('ManualAddFeature', JSON.stringify(requestPayload));
    return this.api.post('ManualAddFeature', formData , { correlationid } );
  }

  getUniqueValuesOfEachColumns( correlationId: string) {
    return this.api.get('GetUniqueValues', { correlationId } );
  }

  saveTextDataPreprocessing( data ) {
  //  const deletedByUserVar = Object.keys(this.deleteByUser);
    this.TextDataPreprocessing = Object.assign({}, data);
  //   const DeletedTextColumnByUser = [];
  //   if ( this.deleteByUser && this.deleteByUser.length > 0 ) {
  //     deletedByUserVar.forEach( (element) => {
  //     if ( this.deleteByUser[element] === 'true' ) {
  //       DeletedTextColumnByUser.push(element);
  //     }
  //   });
  //  }

  //  this.TextDataPreprocessing['DeletedTextColumnByUser'] = DeletedTextColumnByUser;

    console.log( 'TextDataPreprocessing ===> ', this.TextDataPreprocessing);
  }
}
