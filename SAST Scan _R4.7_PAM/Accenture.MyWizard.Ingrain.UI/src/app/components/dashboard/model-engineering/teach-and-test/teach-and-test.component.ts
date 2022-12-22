import { Component, OnInit } from '@angular/core';
import { DialogService } from 'src/app/dialog/dialog.service';
import { tap, switchMap } from 'rxjs/operators';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { empty } from 'rxjs';
import { LocalStorageService } from '../../../../_services/local-storage.service';
import { TeachTestService } from '../../../../_services/teach-test.service';
import { ProblemStatementService } from 'src/app/_services/problem-statement.service';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { TemplateNameModalComponent } from 'src/app/components/template-name-modal/template-name-modal.component';
import { Router } from '@angular/router';
import { CookieService } from 'ngx-cookie-service';
import { UsageTrackingService } from 'src/app/_services/usage-tracking.service';
@Component({
  selector: 'app-teach-and-test',
  templateUrl: './teach-and-test.component.html',
  styleUrls: ['./teach-and-test.component.scss'],

})

export class TeachAndTestComponent implements OnInit {

  correlationId: string;
  teachAndTestData: {} = {};
  displayDataSourceLink: boolean;
  targetColumn: any;
  modelName: any;
  dataSourceName: any;
  useCase: any;
  statusForPrescription: boolean;
  showSaveAs: false;


  pageInfo = 'WFIngestData';
  newModelName: any;
  newCorrelationIdAfterCloned: any;
  frequency: string;
  modelType: string;
  hyperDisableForTimeSeries;
  deliveryTypeName;
  selectedModelName;

  clusteringFlag: string;
  readOnly;

  constructor(private teachTestService: TeachTestService,
    private localStorService: LocalStorageService,
    private dialogService: DialogService,
    private appUtilsService: AppUtilsService,
    private _problemStatementService: ProblemStatementService,
    private _router: Router,
    private notificationService: NotificationService, private cookieService: CookieService,
    private uts: UsageTrackingService
  ) { }

  ngOnInit() {
    this.readOnly = sessionStorage.getItem('viewEditAccess');
    this.correlationId = this.localStorService.getCorrelationId();
    this.frequency = this.cookieService.get('Frequency');
    this.modelType = this.cookieService.get('ProblemType');
    this.uts.usageTracking('Model Engineering', 'Teach and Test');
    if (this.modelType === 'TimeSeries') {
      this.hyperDisableForTimeSeries = true;
    }
    // this.selectedModelName = this.cookieService.get('SelectedRecommendedModel');
    this.selectedModelName = localStorage.getItem('SelectedRecommendedModel');
    this.teachTestService.getTeachTestData(this.correlationId, this.modelType, this.frequency, this.selectedModelName).subscribe(data => {
      this.setTeachAndTestData(data);
      this.displayDataSourceLink = true;
    });
  }

  saveAs($event) {
    const openTemplateAfterClosed = this.dialogService.open(TemplateNameModalComponent, {}).afterClosed.pipe(
      switchMap(modelName => {
        this.newModelName = modelName;
        if (modelName !== null && modelName !== '' && modelName !== undefined) {
          return this._problemStatementService.clone(this.correlationId, modelName);
        } else {
          // tslint:disable-next-line: deprecation
          return empty();
        }
      }),
      tap(data => this.newCorrelationIdAfterCloned = data),
      tap(data => {
        if (data) {
          this._router.navigate(['dashboard/modelengineering/TeachAndTest'], {
            queryParams: { modelName: this.newModelName, correlationId: data }
          });
          this.notificationService.success('Cloned Successfully');
        }
      })
    );

    openTemplateAfterClosed.subscribe();
  }

  setTeachAndTestData(teachAndTestData: any) {

    this.modelName = teachAndTestData.ModelName;
    this.dataSourceName = teachAndTestData.DataSource;
    this.useCase = teachAndTestData.BusinessProblem;
    this.deliveryTypeName = teachAndTestData.Category;
    this.clusteringFlag = teachAndTestData.Clustering_Flag;
    this.localStorService.setLocalStorageData('clusteringFlag', this.clusteringFlag);
    this.localStorService.setLocalStorageData('modelName', this.modelName);
    this.localStorService.setLocalStorageData('dataSource', this.dataSourceName);
    this.localStorService.setLocalStorageData('useCase', this.useCase);
  }

  prescriptionOpen(event) {
    this.statusForPrescription = !this.statusForPrescription;
  }
  ignore() {
    this.statusForPrescription = false;
  }

  fixIt() {

  }

  teachAndTestToggle(event) {
    let requiredUrl = 'dashboard/modelengineering/TeachAndTest/WhatIfAnalysis';
    if (event.currentTarget.checked === true) {
      requiredUrl = 'dashboard/modelengineering/TeachAndTest/HyperTuning';
    }
    this._router.navigateByUrl(requiredUrl);
  }

}
