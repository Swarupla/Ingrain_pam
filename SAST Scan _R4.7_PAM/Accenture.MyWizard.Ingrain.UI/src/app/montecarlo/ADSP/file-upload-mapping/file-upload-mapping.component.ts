import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AlertService } from '../../services/alert-service.service';
import { UploadApiService } from '../../services/upload-api.service';
import { PayloadHelper } from '../../services/payload-helper.service';
import { LoaderState } from '../../services/loader-state.service';
import { ApiCore } from '../../services/api-core.service';


@Component({
  selector: 'app-file-upload-mapping',
  templateUrl: './file-upload-mapping.component.html',
  styleUrls: ['./file-upload-mapping.component.scss']
})
export class FileUploadMappingComponent implements OnInit {

  private file = [];
  public template = '';
  private phases = ['Plan', 'Analyze', 'Design', 'Detailed Technical Design', 'Build', 'Component Test', 'Assembly Test', 'Product Test'];
  private selectedPhases = {};
  private selectedPhasesCount = 0;
  private selectedPhasesName = [];
  constructor(private router: Router, private fileApi: UploadApiService,
    private payloadGenerate: PayloadHelper, private loader: LoaderState,
    private message: AlertService, private apiCore: ApiCore) { }

  ngOnInit() {
    this.template = 'FileUpload';

  }



  private setTemplate(name) {
    if ( name === 'MappingPhases') { this.selectedPhases = {}; }
    this.template = name;
  }

  private setFileData(data) {
    this.file = data;
  }
  private checkedPhase(name, inputRef) {
    this.selectedPhasesCount = 0;
    this.selectedPhasesName = [];
    this.selectedPhases[name] = inputRef.checked;
    for (const key in this.selectedPhases) {
      if (this.selectedPhases.hasOwnProperty(key)) {
        if (this.selectedPhases[key] === true) {
          this.selectedPhasesName.push(key);
          this.selectedPhasesCount++;
        }
      }
    }
  }

  private selectAllPhases(){
    this.selectedPhases = { 'Plan': true, 'Analyze': true, 'Design': true, 'Detailed Technical Design': true, 'Build': true, 'Component Test': true, 'Assembly Test': true, 'Product Test': true};
    this.selectedPhasesCount = this.phases.length;
    this.selectedPhasesName = this.phases;
  }

  private clearAllPhases() {
    this.selectedPhases = {};
    this.selectedPhasesCount = 0;
    this.selectedPhasesName = [];
  }
  
  private navigateToInput() {

    const InputColumns = this.selectedPhasesName;
    const requestPayload = this.payloadGenerate.getFileUploadPayload();
    const fileparams = {
      'InputColumns': JSON.stringify(InputColumns),
      'file': this.file[0]
    };
    Object.assign(requestPayload, fileparams);

    this.loader.start();
    this.fileApi.uploadFile('TemplateUpload', this.file, requestPayload).subscribe(
      (data) => {
        // console.log(data);
        if (data.IsUploaded === true) {
          this.loader.stop();
          if (data.hasOwnProperty('TemplateInfo')) {
            // this.selectedPhasesName = data.TemplateInfo.InputColumns;
            // this.apiCore.paramData.InputSelection = data.TemplateInfo.InputSelection;
            if (data.TemplateInfo.hasOwnProperty('blankData') && data.TemplateInfo.blankData) {   this.message.warning('Blank/Null values are replaced by 0'); }
          } else {
            // this.selectedPhasesName = data.InputColumns;
            // this.apiCore.paramData.InputSelection = data.InputSelection;
            if (data.hasOwnProperty('blankData') && data.blankData) {   this.message.warning('Blank/Null values are replaced by 0'); }
          }
          this.apiCore.paramData.selectInputId = data.TemplateId;
          this.message.success('File Uploaded Successfully');
          this.router.navigate(['RiskReleasePredictor/Input']);
        }
        if (data.IsUploaded === false) {
          this.loader.stop();
          this.message.error(data.ErrorMessage);
          this.template = 'FileUpload';
        }
      },
      (error) => {
        this.loader.stop();
        this.template = 'FileUpload';
        const errorMessage = error?.error ? error?.error : 'Something went wrong.';
        this.message.error(errorMessage);
      }
    );

  }

  private downloadTemplate() {
    const path = '../../../../assets/MonteCarlo/template/Template.xlsx';
    window.open(path, 'self');
  }

}
