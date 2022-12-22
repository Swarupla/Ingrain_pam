import { Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute, Params } from '@angular/router';
import { ApiCore } from '../../services/api-core.service';
import { ApiService } from 'src/app/_services/api.service';
import { LoaderState } from '../../services/loader-state.service';
import { EnvironmentService } from 'src/app/_services/EnvironmentService';
import { AuthenticationService } from 'src/app/msal-authentication/msal-authentication.service';

@Component({
  selector: 'app-generic-landing',
  templateUrl: './generic-landing.component.html',
  styleUrls: ['./generic-landing.component.scss']
})
export class GenericLandingComponent implements OnInit {

  public requestCompletedCheck = false;
  constructor(public router: Router, public apiCore: ApiCore, private ingrainApiService: ApiService,
    private activatedRoute: ActivatedRoute, private loader: LoaderState, private environmentService : EnvironmentService, private msalAuthentication: AuthenticationService) { }

  ngOnInit() {
    if (this.environmentService.environment.authProvider.toLowerCase() === 'AzureAD'.toLowerCase()) {
      if (!this.msalAuthentication.msalService.instance.getAllAccounts().length) {
          this.msalAuthentication.login();
      } else {
    //this.environmentService.isHeaderLoaded().subscribe(flag => {
      this.activatedRoute.queryParams.subscribe((params: Params) => {
        sessionStorage.setItem('VDSProblemType', 'Generic');
        if (params.hasOwnProperty('CategoryType') && params.hasOwnProperty('RequestType')) {
          sessionStorage.setItem('VDSUseCaseName', params.RequestType);
          sessionStorage.setItem('VDSCategoryType', params.CategoryType);

          this.apiCore.setCoreParams();
          if (this.apiCore.paramData.vdsUsecaseName === '') {
            this.requestCompletedCheck = true;
            this.router.navigate(['generic/upload'], { queryParams: params });
          } else {
            this.loader.start();

            this.ingrainApiService.get('GetTemplateData', {
              'ClientUID': this.apiCore.paramData.clientUID,
              'DeliveryConstructUID': this.apiCore.paramData.deliveryConstructUId,
              'UserId': this.apiCore.getUserId(),
              'UseCaseName': this.apiCore.paramData.vdsUsecaseName,
              'ProblemType': this.apiCore.paramData.problemType,
              'TemplateID': null
            }).subscribe(
              (data) => {
                this.requestCompletedCheck = true;
                if (data && data.hasOwnProperty('IsTemplates') && data.IsTemplates) {
                  this.loader.stop();
                  this.apiCore.paramData.selectInputId = data.TemplateInfo.TemplateID;
                  this.apiCore.paramData.UseCaseID = data.TemplateInfo.UseCaseID;
                  // If SimulationExist navigate to Output directly
                  if (!data.IsSimulationExist) {
                    this.router.navigate(['generic/Input'], { queryParams: params });
                  } else {
                    this.apiCore.paramData.selectSimulationId = data.SimulationID;
                    this.router.navigate(['generic/Output'], { queryParams: params });
                  }
                } else {
                  this.loader.stop();
                  this.router.navigate(['generic/upload'], { queryParams: params });
                }
              },
              (error) => { this.loader.stop(); }
            );
          }
        }
      });
  }
} else {
  this.activatedRoute.queryParams.subscribe((params: Params) => {
    sessionStorage.setItem('VDSProblemType', 'Generic');
    if (params.hasOwnProperty('CategoryType') && params.hasOwnProperty('RequestType')) {
      sessionStorage.setItem('VDSUseCaseName', params.RequestType);
      sessionStorage.setItem('VDSCategoryType', params.CategoryType);

      this.apiCore.setCoreParams();
      if (this.apiCore.paramData.vdsUsecaseName === '') {
        this.requestCompletedCheck = true;
        this.router.navigate(['generic/upload'], { queryParams: params });
      } else {
        this.loader.start();

        this.ingrainApiService.get('GetTemplateData', {
          'ClientUID': this.apiCore.paramData.clientUID,
          'DeliveryConstructUID': this.apiCore.paramData.deliveryConstructUId,
          'UserId': this.apiCore.getUserId(),
          'UseCaseName': this.apiCore.paramData.vdsUsecaseName,
          'ProblemType': this.apiCore.paramData.problemType,
          'TemplateID': null
        }).subscribe(
          (data) => {
            this.requestCompletedCheck = true;
            if (data && data.hasOwnProperty('IsTemplates') && data.IsTemplates) {
              this.loader.stop();
              this.apiCore.paramData.selectInputId = data.TemplateInfo.TemplateID;
              this.apiCore.paramData.UseCaseID = data.TemplateInfo.UseCaseID;
              // If SimulationExist navigate to Output directly
              if (!data.IsSimulationExist) {
                this.router.navigate(['generic/Input'], { queryParams: params });
              } else {
                this.apiCore.paramData.selectSimulationId = data.SimulationID;
                this.router.navigate(['generic/Output'], { queryParams: params });
              }
            } else {
              this.loader.stop();
              this.router.navigate(['generic/upload'], { queryParams: params });
            }
          },
          (error) => { this.loader.stop(); }
        );
      }
    }
  });
}
}

  navigateToVDS() {
    window.location.href = this.ingrainApiService.vdsURL + '?clientUId=' + this.apiCore.paramData.clientUID + '&accountUId=&deliveryConstructUId=' + this.apiCore.paramData.deliveryConstructUId + '';
  }
}
