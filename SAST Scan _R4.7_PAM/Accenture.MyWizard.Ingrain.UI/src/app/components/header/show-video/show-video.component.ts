import { Component, OnInit } from '@angular/core';
import { ApiService } from 'src/app/_services/api.service';
import { ClientDeliveryStructureService } from 'src/app/_services/client-delivery-structure.service';

@Component({
  selector: 'app-show-video',
  templateUrl: './show-video.component.html',
  styleUrls: ['./show-video.component.scss']
})
export class ShowVideoComponent implements OnInit {
  videoresponse;
  constructor( private cds: ClientDeliveryStructureService, private _apiService: ApiService) { }

  ngOnInit() {
    this.getVideos();
  }

  getVideos() {
    // https://devtest-mywizardapi-si.accenture.com/core/v1/videoplayer?
    // featureName=VADataScientist&container=mp4&appserviceUid=00010150-0000-0000-0000-000000000000
    const myWizardWebConsoleUrl = this._apiService.phoenixApiBaseURL;
    const videourl = `/v1/VideoPlayer?featureName=Ingrain&container=mp4&appserviceuid=00040560-0000-0000-0000-000000000000`;
    this.videoresponse = myWizardWebConsoleUrl + videourl;
  }

}
