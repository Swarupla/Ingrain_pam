import { Component, OnInit, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
import { tap } from 'rxjs/operators';
import { DialogService } from 'src/app/dialog/dialog.service';
import { ModalNameDialogComponent } from 'src/app/_reusables/modal-name-dialog/modal-name-dialog.component';
import { FilesMappingModalComponent } from '../../files-mapping-modal/files-mapping-modal.component';
import { AdDeployedModelListComponent } from '../ad-deploy-model/ad-deployed-model-list/ad-deployed-model-list.component';
import { UploadProgressComponent } from '../upload-progress/upload-progress.component';

@Component({
  selector: 'app-ad-dashboard',
  templateUrl: './ad-dashboard.component.html',
  styleUrls: ['./ad-dashboard.component.scss']
})
export class AdDashboardComponent implements OnInit {
  @ViewChild(AdDeployedModelListComponent, { static: false }) deployedModelListComp: AdDeployedModelListComponent;

  modelName: string;

  @ViewChild('searchInput', { static: false }) searchInput: any;

  isnavBarToggle: boolean; // header related
  isNavBarLabelsToggle = false;
  toggle: boolean = true;

  constructor(private router: Router, private dialogService :DialogService, private _bsModalService: BsModalService) { }

  ngOnInit(): void {
  }


  /* -------- header related function ----- */
  previous() {
    this.router.navigate(['choosefocusarea']);
  }

  toggleNavBar() {
    this.isnavBarToggle = !this.isnavBarToggle;
  }

  toggleNavBarLabels() {
    this.isNavBarLabelsToggle = !this.isNavBarLabelsToggle;
    return false;
  }

  refreshModelList() {
    this.deployedModelListComp.getTempate();
  }

  closeSearchBox() {
    this.searchInput = '';
  }
  /* -------- header relaed function ----- */

  /**
   * 
   * @param value 
   * @returns 
   */
  searchADModel(value) {
    return value;
  }

  getUploadedData(data){
    this.showModelEnterDialog(data.finalPayload);
  }

  /* Enter Model Name Dialog */
  showModelEnterDialog(filesData) {

    this._bsModalService.show(ModalNameDialogComponent, { class: 'modal-dialog modal-dialog-centered', backdrop: 'static', initialState: { title: 'Save View', placeholder: 'Enter model Name', isAnomalyService : true } }).content.Data.subscribe(modelName => {
      this.openFileProgressDialog(filesData, modelName);
    });
  }

  /* Show File Upload Process Dialog */
  openFileProgressDialog(filesData, modelName) {
    this.modelName = modelName;
    const totalSourceCount = filesData[0].sourceTotalLength;
    if (totalSourceCount > 1) {
      const openFileProgressAfterClosed = this.dialogService.open(UploadProgressComponent,
        { data: { filesData: filesData, modelName: modelName} }).afterClosed.pipe(
          tap(data => data ? this.openModalMultipleDialog(data.body, modelName, filesData, data.dataForMapping) : '')
        );
      openFileProgressAfterClosed.subscribe();
    } else {
      const openFileProgressAfterClosed = this.dialogService.open(UploadProgressComponent,
        { data: { filesData: filesData, modelName: modelName } }).afterClosed.pipe(
          tap(data => data ? this.navigateToADUseCaseDefinition(data.body[0]) : '')
        );
      openFileProgressAfterClosed.subscribe();
    }
  }

  navigateToADUseCaseDefinition(correlationId) {
    //this.correlationId = correlationId;
    localStorage.setItem('correlationId', correlationId);
    this.router.navigate(['/anomaly-detection/problemstatement/usecasedefinition'],
      {
        queryParams: {
          'modelCategory': '',
          'displayUploadandDataSourceBlock': false,
          'modelName': this.modelName
        }
      });
  }

  /* Multiple File Mapping Dialog */
  openModalMultipleDialog(filesData, modelName, fileUploadData, fileGeneralData) {
    this.modelName = modelName;
    if (filesData.Flag === 'flag3' || filesData.Flag === 'flag4') {
      const openFileMappingTemplateAfterClosed = this.dialogService.open(FilesMappingModalComponent,
        { data: { filesData: filesData, fileUploadData: fileUploadData, fileGeneralData: fileGeneralData } }).afterClosed.pipe(
          tap(data => data ? this.navigateToADUseCaseDefinition(data[0]) : '')
        );
      openFileMappingTemplateAfterClosed.subscribe();
    } else {
      if (filesData.CorrelationId !== undefined) {
        this.navigateToADUseCaseDefinition(filesData.CorrelationId);
      } else {
        if (filesData[0] !== undefined) {
          this.navigateToADUseCaseDefinition(filesData[0]);
        }
      }
    }
  }

}
