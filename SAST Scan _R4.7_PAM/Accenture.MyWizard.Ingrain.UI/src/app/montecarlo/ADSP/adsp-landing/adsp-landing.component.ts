import { Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute, Params } from '@angular/router';
import { ApiCore } from '../../services/api-core.service';
import { ApiService } from 'src/app/_services/api.service';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { LoaderState } from '../../services/loader-state.service';
import { UploadApiService } from '../../services/upload-api.service';
import { AlertService } from '../../services/alert-service.service';
import { timer } from 'rxjs';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';
import { EnvironmentService } from 'src/app/_services/EnvironmentService';

@Component({
  selector: 'app-adsp-landing',
  templateUrl: './adsp-landing.component.html',
  styleUrls: ['./adsp-landing.component.scss']
})
export class AdspLandingComponent implements OnInit {

  // private isInputVersionExist: Boolean = true;
  public phases = ['Plan', 'Analyze', 'Design', 'Detailed Technical Design', 'Build', 'Component Test', 'Assembly Test', 'Product Test'];
  private timerSubscription;
  public inProgressPercentage = '0';
  public pythonProgress;
  public requestCompletedCheck = false;
  constructor(public router: Router, private activatedRoute: ActivatedRoute,
    public apiCore: ApiCore, private message: AlertService,
    private ingrainApiService: ApiService,
    private loader: LoaderState, private fileApi: UploadApiService,
    private coreUtilService: CoreUtilsService, private environmentService : EnvironmentService
  ) { }

  ngOnInit() {
    this.environmentService.isHeaderLoaded().subscribe(flag => {
      this.activatedRoute.queryParams.subscribe((params: Params) => {
        sessionStorage.setItem('VDSProblemType', 'ADSP');
        sessionStorage.setItem('VDSUseCaseName', 'ADSP');
        if (params.hasOwnProperty('CategoryType') && params.hasOwnProperty('RequestType')) {
          sessionStorage.setItem('VDSCategoryType', params.CategoryType);

          this.apiCore.setCoreParams();
          this.loader.start();
          const input = [""];
          this.RRPPullDataFromPheonix(params);
        }
      });
    });
  
 }

 RRPPullDataFromPheonix(params) {
  this.fileApi.uploadFile('RRPUpload', null , {
    'ClientUID': this.apiCore.paramData.clientUID,
    'DeliveryConstructUID': this.apiCore.paramData.deliveryConstructUId,
    'DeliveryTypeID': this.apiCore.paramData.deliveryConstructUId,
    'CreatedByUser': this.apiCore.getUserId(),
    'UseCaseName': 'ADSP',
    'UseCaseDescription': 'ADSP',
    'ProblemType': 'RRP',
    'TemplateID': null,
    'DeliveryTypeName' : this.apiCore.paramData.vdsCategoryType,
    'TargetColumn': 'Defect',
    'InsertBase': 'False',
    'InputColumns': '[]'
  }).subscribe(
    (result) => {
      if ( result.Status === "P" || result.Status === "BsonNull") { // "Status" : "P",
        this.loader.start();
        this.pythonProgress = result.Status;
        this.inProgressPercentage = result.Progress;
        if ( result.Status === "BsonNull") {
          this.inProgressPercentage = '0';
        }
        this.retryRRPDataPullFromPheonix(params);
      } 
      // else if ( result.Status === "C" && result.Message === "Completed" ) {
      //     // Then call get template
      //     this.apiCore.paramData.selectInputId = result.TemplateId;
      //     this.apiCore.paramData.UseCaseID = result.UseCaseID;
      //     if ( !result.IsSimulationExist) {
      //     this.router.navigate(['RiskReleasePredictor/Input'], { queryParams: params});
      //     } else {
      //     this.apiCore.paramData.selectSimulationId = result.SimulationID;
      //     this.router.navigate(['RiskReleasePredictor/Output'], { queryParams: params});
      //     }
      //     this.requestCompletedCheck = true;    
      // }
       else if ( result.Status === "C") {
        // Then automatically call the run simulation
        // Date format yyyy-mm-dd 
        // select current release
        // call run simulation 
        this.loader.start();
        this.ingrainApiService.get('GetTemplateData', {
          'ClientUID': this.apiCore.paramData.clientUID,
          'DeliveryConstructUID': this.apiCore.paramData.deliveryConstructUId,
          'UserId': this.apiCore.getUserId(),
          'UseCaseName': 'ADSP',
          'ProblemType': 'ADSP',
          'TemplateID': null
        }).subscribe(
          (data) => {
            if (data && data.hasOwnProperty('IsTemplates')) {
              this.loader.immediatestop();
              if (!data.IsTemplates) {
                this.requestCompletedCheck = true;
                this.router.navigate(['RiskReleasePredictor/upload'] , { queryParams: params});
                this.message.warning("Insufficient data, hence simulation cannot be generated. Kindly update the data and run the simulation again.");
              } else {
                this.apiCore.paramData.selectInputId = data.TemplateInfo.TemplateID;
                this.apiCore.paramData.UseCaseID = data.TemplateInfo.UseCaseID;
                // If SimulationExist navigate to Output directly
                if ( !data.IsSimulationExist) {
                this.router.navigate(['RiskReleasePredictor/Input'], { queryParams: params});
                this.requestCompletedCheck = true;
                } else {
                  if (data.hasOwnProperty('TemplateInfo')) {  
                    this.apiCore.paramData.InputSelection = data.TemplateInfo.InputSelection;
                  } else {
                    this.apiCore.paramData.InputSelection = data.InputSelection;
                  }
                  this.setInputSelectionRRP(data, params); 
            
                }
              }
            }
          },
          (error) => {
            this.loader.immediatestop();
          }
        );
      } else if ( result.Status === "E") {
          this.loader.immediatestop();
          this.message.warning(result.Message);
          // this.router.navigate(['RiskReleasePredictor/upload'] , { queryParams: params});
          this.unsubscribe();
          // this.requestCompletedCheck = true;
          this.loader.start();
          this.ingrainApiService.get('GetTemplateData', {
            'ClientUID': this.apiCore.paramData.clientUID,
            'DeliveryConstructUID': this.apiCore.paramData.deliveryConstructUId,
            'UserId': this.apiCore.getUserId(),
            'UseCaseName': 'ADSP',
            'ProblemType': 'ADSP',
            'TemplateID': null
          }).subscribe(
            (data) => {
              if (data && data.hasOwnProperty('IsTemplates')) {
                this.loader.immediatestop();
                if (!data.IsTemplates) {
                  this.requestCompletedCheck = true;
                  this.router.navigate(['RiskReleasePredictor/upload'] , { queryParams: params});
                  this.message.warning("Insufficient data, hence simulation cannot be generated. Kindly update the data and run the simulation again.");
                } else {
                  this.apiCore.paramData.selectInputId = data.TemplateInfo.TemplateID;
                  this.apiCore.paramData.UseCaseID = data.TemplateInfo.UseCaseID;
                  // If SimulationExist navigate to Output directly

                  if ( data.TemplateVersions.length === 1 && data.TemplateVersions[0].Version === 'Base_Version' ) {
                    this.requestCompletedCheck = true;
                    this.router.navigate(['RiskReleasePredictor/upload'] , { queryParams: params});
                    this.message.warning("Insufficient data, hence simulation cannot be generated. Kindly update the data and run the simulation again.");
                  } else if ( !data.IsSimulationExist) {
                  this.router.navigate(['RiskReleasePredictor/Input'], { queryParams: params});
                  this.requestCompletedCheck = true;
                  } else {
                    if (data.hasOwnProperty('TemplateInfo')) {  
                      this.apiCore.paramData.InputSelection = data.TemplateInfo.InputSelection;
                    } else {
                      this.apiCore.paramData.InputSelection = data.InputSelection;
                    }
                    this.setInputSelectionRRP(data, params); 
              
                  }
                }
              }
            },
            (error) => {
              this.loader.immediatestop();
            }
          );
      }
    }
  );
 }

 
 retryRRPDataPullFromPheonix(_data) {
  this.timerSubscription = timer(10000).subscribe(() =>
    this.RRPPullDataFromPheonix(_data));
  return this.timerSubscription;
}

unsubscribe() {
  if (!this.coreUtilService.isNil(this.timerSubscription)) {
    this.timerSubscription.unsubscribe();
  }
  this.loader.stop();
}


 setInputSelectionRRP(data, params) { 

  for (let i = 0; i < this.phases.length; i++) {
    if ( this.apiCore.paramData.InputSelection.hasOwnProperty(this.phases[i])) {
    this.apiCore.paramData.InputSelection[this.phases[i]] = this.apiCore.paramData.InputSelection[this.phases[i]];
    } else {
    this.apiCore.paramData.InputSelection[this.phases[i]] = 'NA';
    }
  }

  this.apiCore.paramData.unSelectedPhases = {};
   for (const selectedPhases in this.apiCore.paramData.InputSelection) {
    if (this.apiCore.paramData.InputSelection[selectedPhases] === 'NA' || this.apiCore.paramData.InputSelection[selectedPhases] === 'False' )
     this.apiCore.paramData.unSelectedPhases[selectedPhases] = 'False';
   }
   this.apiCore.paramData.selectSimulationId = data.SimulationID;
   this.router.navigate(['RiskReleasePredictor/Output'], { queryParams: params});
   this.requestCompletedCheck = true;
}


 navigateToVDS() {
 window.location.href = this.ingrainApiService.vdsURL + '?clientUId=' + this.apiCore.paramData.clientUID +'&accountUId=&deliveryConstructUId=' + this.apiCore.paramData.deliveryConstructUId + '';
 }
}
