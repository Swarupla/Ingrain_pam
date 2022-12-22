import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class StoreService {

  userData = {};
  private dynamicStyleSubject = new Subject();

  dynamicEntities;
  filteredMatricsData;

  constructor() { }

  getUserData() {
    if (localStorage.getItem('currentUser')) {
      return localStorage.getItem('currentUser')['userId'];
    } else {
      return '';
    }
  }

  setUserData(key, value) {
    this.userData[key] = value;
  }

  sendValueToFooter(value: boolean) {
    this.dynamicStyleSubject.next(value);
  }

  getValue() {
    return this.dynamicStyleSubject.asObservable();
  }

  setEntityData(entityData) {
    this.dynamicEntities = entityData;
  }
  getEntityData() {
    return this.dynamicEntities;
  }
  setMatricFilteredData(filteredMatrics) {
    this.filteredMatricsData = filteredMatrics;
  }
  getMatircsFilteredData() {
    return this.filteredMatricsData;
  }

}
