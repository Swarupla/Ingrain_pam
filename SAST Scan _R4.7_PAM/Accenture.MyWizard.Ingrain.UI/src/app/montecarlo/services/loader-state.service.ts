import { Injectable } from '@angular/core';
import { AppUtilsService } from 'src/app/_services/app-utils.service';


@Injectable({
  providedIn: 'root'
})
export class LoaderState {
 
  constructor(private ingrainAppLoader: AppUtilsService) { 

  }

  start() {
   this.ingrainAppLoader.loadingStarted();
  }

  stop() {
  this.ingrainAppLoader.loadingEnded();
  }

  immediatestop() {
    this.ingrainAppLoader.loadingImmediateEnded();
    }

}
