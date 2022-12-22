import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class SimulationHeader {
  private isRunSimulation: BehaviorSubject<boolean> = new BehaviorSubject(true);
  public isRunSimulation$ = this.isRunSimulation.asObservable();
  public disableRunSimulation: boolean;
  public disableSaveAs: boolean;
  public outputSavePayload = {};
  public genericGridData;
  constructor() {

  }

  public createPhases(data, Features) {
    for (const key in data) {
      if (data) {
        const innerData = data[key];
        for (const key1 in innerData) {
          if (key1 !== 'rowHeight') {
            if (key1.includes('_')) {
              const mainphase = key1.split('_')[0];
              const subphase = key1.split('_')[1];
              if (!Features.hasOwnProperty(mainphase)) {
                Features[mainphase] = {};
                this.setPhaseLevelData(Features, mainphase, subphase, innerData, key1);
              } else {
                this.setPhaseLevelData(Features, mainphase, subphase, innerData, key1);
              }
            } else {
              if (!Features.hasOwnProperty(key1)) {
                Features[key1] = [innerData[key1]];
              } else {
                Features[key1].push(innerData[key1]);
              }
            }
          }
        }
      }
    }
    return Features;
  }
  private setPhaseLevelData(Features, mainphase, subphase, innerData, key1) {
    if (!Features[mainphase].hasOwnProperty(subphase)) {
      if (!this.isDateFields(subphase)) {
        Features[mainphase][subphase] = [Number(innerData[key1])];
      } else {
        Features[mainphase][subphase] = [innerData[key1]];
      }
    } else {
      if (!this.isDateFields(subphase)) {
        Features[mainphase][subphase].push(Number(innerData[key1]));
      } else {
        Features[mainphase][subphase].push(innerData[key1]);
      }
    }
  }

  private isDateFields(subphase) {
    return (subphase === 'Release Start Date (dd/mm/yyyy)' ||
      subphase === 'Release End Date (dd/mm/yyyy)');
  }

  public setRunSimulationButton(flag) {
    this.isRunSimulation.next(flag);
    // this.isRunSimulation$.subscribe( (flag1) => {
    //   this.disableRunSimulation = flag1;
    // });
    this.disableRunSimulation = flag;
  }

  public createGenericPhases(data) {
    console.log(data);
    const header = data.column;
    const row = data.row;
    const r = {};
    for (let i = 0; i < row.length; i++) {
      for (let j = 0; j < row[i].length; j++) {
        if (r.hasOwnProperty(header[j])) {
            r[header[j]].push(row[i][j]);
        } else {
            r[header[j]] = [row[i][j]];
        }
      }
    }
    this.genericGridData = JSON.parse(JSON.stringify(r));
    return r;
  }


  public isMandatory() {
    const error = {
      'red': false,
      'invalidRange': false
    }
    for (const key in this.genericGridData) {
      if ( this.genericGridData[key]) {
        const d = this.genericGridData[key];
        for ( const key2 in d) {
          if ( d[key2] === 'invalid') { error.red = true;}
          if ( d[key2] === 'invalidRange') { error.invalidRange = true;}
        }
      }
    }
    return error;
  }

  setOutputSavePayload(payLoadData) {
    this.outputSavePayload = payLoadData;
  }

  getOutputSavePayload() {
    return this.outputSavePayload;
  }

}
