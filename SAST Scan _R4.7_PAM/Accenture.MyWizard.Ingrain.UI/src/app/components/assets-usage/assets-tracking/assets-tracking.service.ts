import { Injectable } from '@angular/core';
import { ApiService } from 'src/app/_services/api.service';


@Injectable({
  providedIn: 'root'
})
export class AssetTrackingService {

  constructor(private api: ApiService) { }

  getAssetUsageDashBoard(sdate , edate) {
    // AssetUsageDashBoard?fromDate=2/2/2021&todate=2/8/2021
    const params = { 'fromDate': sdate , 'todate': edate};
    return this.api.get('AssetUsageDashBoard',params)
  }
}
