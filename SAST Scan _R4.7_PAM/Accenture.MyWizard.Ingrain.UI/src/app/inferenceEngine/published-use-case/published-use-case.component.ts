import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { FormGroup, FormBuilder } from '@angular/forms';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { PublishedUseCaseService } from './published-use-case.service';
export class PublishClass {
  UseCaseId?: string = '';
  UseCaseName?: string = '';
  UseCaseDescription?: string = '';
  CorrelationId?: string = '';
  ApplicationId?: string = '';
  ApplicationName?: string = '';
  SourceName?: string = '';
  Entity?: "";
  InferenceConfigurationsDetails?= [];
  CreatedBy?: string = '';
  CreatedOn?: string = '';
  ModifiedBy?: string = '';
  ModifiedOn?: string = '';
  constructor(values = {}) {
    Object.keys(this).forEach(key => {
      if (values && values.hasOwnProperty(key))
        this[key] = values[key];
    });
  }
}

export class InferenceConfigurationsDetails {
  ConfigName: "entity conf 2"
  constructor(values = {}) {
    Object.keys(this).forEach(key => {
      if (values && values.hasOwnProperty(key))
        this[key] = values[key];
    });
  }
}

@Component({
  selector: 'app-published-use-case',
  templateUrl: './published-use-case.component.html',
  styleUrls: ['./published-use-case.component.css']
})
export class PublishedUseCaseComponent implements OnInit {
  @Input() paramData;
  @Output() publishedUseCasesList = new EventEmitter();
  
  publishedUseCases: Array<PublishClass> = [];
  public dateControl: FormGroup;
  startDate = new Date();
  selectedEntity: PublishClass = {};
  selectedusecaseSampleInputIndex: number;
  radioButtonselected: number;
  isSampleInputPopupClosed: any = false;
  usecaseSampleInputshow: boolean = false;
  infoUseCase = [];
  userId: string;
  constructor(private ps: PublishedUseCaseService, private ns: NotificationService, private ap: AppUtilsService,
    private formBuilder: FormBuilder) {
    this.dateControl = this.formBuilder.group({
      startDateControl: [''],
      endDateControl: [''],
    });
  }

  ngOnInit(): void {
    this.getUseCaseList();
  }

  getUseCaseList() {
    this.ap.loadingStarted();
    this.ps.getUseCaseList().subscribe(
      (data) => {
        this.ap.loadingEnded();
        this.publishedUseCases = data;
        this.publishedUseCasesList.emit(this.publishedUseCases);
        this.userId = this.ap.getCookies().UserId;
      },
      (error) => {
        this.ap.loadingEnded();
        this.ns.error(error.error);
        this.publishedUseCases = [];
      }
    )
  }

  deleteUseCaseModel(useCases: PublishClass) {
    this.ap.loadingStarted();
    this.ps.deleteUseCase(useCases.UseCaseId).subscribe(
      (data) => {
        this.ns.success("Deleted Successfully");
        this.ap.loadingEnded();
        this.getUseCaseList();
      },
      (error) => {
        this.ns.error(error.error);
        this.ap.loadingEnded();
      }
    );
  }

  selectedUseCase(usecase: PublishClass) {
    this.selectedEntity = usecase;

    this.dateControl.get('startDateControl').setValue(this.startDate);
    this.dateControl.get('endDateControl').setValue(this.startDate);
  }


  trainUseCase() {
    this.userId = this.ap.getCookies().UserId;
    const startDate = this.dateControl.get('startDateControl').value.toLocaleDateString();
    const endDate = this.dateControl.get('endDateControl').value.toLocaleDateString();
    //  https://devut-ai-mywizardapi-si.aiam-dh.com/ingrain/api/TrainUseCase
    this.ap.loadingStarted();
    const REQUEST = {
      "ClientUId": this.paramData.clientUID,
      "DeliveryConstructUId": this.paramData.deliveryConstructUId,
      "UseCaseId": this.selectedEntity.UseCaseId,
      "DataSource": 'Entity',
      "DataSourceDetails": {
        "StartDate": startDate,
        "EndDate": endDate
      },
      "ApplicationId": this.selectedEntity.ApplicationId,
      "UserId": this.userId
    }


    this.ps.trainUseCase(REQUEST).subscribe(
      (data) => {
        this.ns.success('Usecase trained successfully');
        this.ap.loadingEnded();
      },
      (error) => {
        this.ns.error(error.error);
        this.ap.loadingEnded();
      }
    )
  }

  openUsecaseSampleInputWindow(index: number, usecaseDtl: PublishClass) {
    this.selectedusecaseSampleInputIndex = index;

    this.createInfoStructure(usecaseDtl);
    if (this.isSampleInputPopupClosed) {
    } else {
      this.usecaseSampleInputshow = true;
    }
    this.isSampleInputPopupClosed = false;
  }

  closeUsecaseSampleInputWindow(index: number) {
    this.selectedusecaseSampleInputIndex = index;
    this.usecaseSampleInputshow = false;
    return this.usecaseSampleInputshow;
  }

  createInfoStructure(usecaseDtl: PublishClass) {

    const InferenceConfigDetails = usecaseDtl.InferenceConfigurationsDetails;
    const infoUseCase = JSON.parse(JSON.stringify(InferenceConfigDetails));
    
    infoUseCase.forEach(element => {
      delete element.InferenceConfigId;
      delete element['MetricConfig']['FeatureCombinations'];
      delete element['MetricConfig']['Features'];
      delete element['CreatedBy'];
      delete element['CreatedOn'];
      delete element['ModifiedBy'];
      delete element['ModifiedOn'];
      delete element['UseCaseDescription'];
      delete element['UseCaseName'];
      delete element['ApplicationName'];

      if (element['VolumetricConfig'] && element['VolumetricConfig']['Dimensions']) {
        delete element['VolumetricConfig']['Dimensions'];
      } else {
        delete element['VolumetricConfig'];
      }
    });

    this.infoUseCase = infoUseCase;
  }
}
