import { Component, OnInit, Input, Output, EventEmitter, ViewChild, ElementRef, AfterViewInit, TemplateRef } from '@angular/core';
import { ApiService } from 'src/app/_services/api.service';
import { ApiCore } from 'src/app/montecarlo/services/api-core.service';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
import { AlertService } from 'src/app/montecarlo/services/alert-service.service';
import { GridDataService } from 'src/app/montecarlo/ADSP/grid-table/grid-data.service';
import { UploadApiService } from 'src/app/montecarlo/services/upload-api.service';
import { SimulationHeader } from './simulation-header.service';
import { LoaderState } from 'src/app/montecarlo/services/loader-state.service';
import { PayloadHelper } from 'src/app/montecarlo/services/payload-helper.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-simulation-header',
  templateUrl: './simulation-header.component.html',
  styleUrls: ['./simulation-header.component.scss']
})
export class SimulationHeaderComponent implements OnInit, AfterViewInit {
  @ViewChild('SimulationExistModel', { static: true }) SimulationExistModel: TemplateRef<any>;
  @ViewChild('templateNameModal', { static: true }) templateNameModal: TemplateRef<any>;
  @Output() setGridInputTable = new EventEmitter();
  @Output() setSimulatedData = new EventEmitter();
  @Input() flowInfo; // 'input', 'output'
  @Input() typeOfSimulation; // 'generic' , 'notgeneric'
  @Input() templateID;

  public uploadModalRef: BsModalRef | null;
  public saveAsModalRef: BsModalRef | null;
  public templateNameModalRef: BsModalRef | null;
  public simulationExistModelRef: BsModalRef | null;
  public templateNameString = '';
  public uploadModalConfig = {
    ignoreBackdropClick: true,
    class: 'uploadFile-modal'
  };
  public saveAsModalConfig = {
    ignoreBackdropClick: true,
    class: 'saveAs-modal'
  };
  public inputVersionDropDownList = [];
  public selectedInputVersion = { 'TemplateID': '', 'Version': '' };
  public simulatedVersionDropDownList = [];
  public selectedSimulatedVersion = { 'TemplateID': '', 'SimulationID': '', 'SimulationVersion': '' };
  private file = [];
  private template = '';
  public phases = ['Plan', 'Analyze', 'Design', 'Detailed Technical Design', 'Build', 'Component Test', 'Assembly Test', 'Product Test'];
  public selectedPhases = {};
  public selectedPhasesCount = 0;
  public selectedPhasesName = [];
  public config = {
    'flow': ''
  };
  public selected = {
    'target': '',
    'unique': '',
    'templateID': ''
  };
  public editingDropdownType = { 'type': '', 'data': {} };
  public target = [];
  public unique = [];
  public saveOutputDataRequest = {};
  public disableOutput;
  public numberOfFeatures = 0;
  public saveAsInputFeatures = {};
  oldSelectedTarget;
  public outputSaveCheckBox = false;
  outputSimulationTxt = true;

  public baseVersionCheckedBox = false;
  public InsertBase = false;
  public saveNewName;
  public newSimulationVersionName;
  isInitialSelection = true;

  constructor(private ingrainApiService: ApiService, private _modalService: BsModalService, private message: AlertService,
    private gridData: GridDataService, private fileApi: UploadApiService, public validator: SimulationHeader,
    private loader: LoaderState, private payloadGenerate: PayloadHelper, public router: Router,
    public apiCore: ApiCore) { }

  ngOnInit() {
    /*  this.config.flow = this.flowInfo;
    this.templateID = this.apiCore.paramData.selectInputId;
    if (this.config.flow === 'input') {
      this.getTemplateInfoById(this.templateID);
    }
    if (this.config.flow === 'output') {
      if (this.apiCore.paramData.selectSimulationId) {
        this.runSimulation();
      }
    } */
  }

  ngAfterViewInit() {
    this.baseVersionCheckedBox = false;
    this.outputSaveCheckBox = false;
    this.saveNewName = '';
    this.newSimulationVersionName = '';
    this.config.flow = this.flowInfo;
    this.templateID = this.apiCore.paramData.selectInputId;
    if (this.config.flow === 'input') {
      this.getTemplateInfoById(this.templateID);
    }
    if (this.config.flow === 'output') {
      if (this.apiCore.paramData.selectSimulationId) {
        this.runSimulation();
      }
    }
    console.log('data--', this.saveOutputDataRequest);
  }

  loadInput() {
    if (this.config.flow === 'output') {
      this.validator.setRunSimulationButton(false);
      if (this.typeOfSimulation === 'notgeneric') {
        this.router.navigate(['RiskReleasePredictor/Input']);
      } else {
        this.router.navigate(['generic/Input']);
      }
    }
  }

  loadOutput() {
    if (this.config.flow === 'input' && this.disableOutput) {
      if (this.typeOfSimulation === 'notgeneric') {
        this.apiCore.paramData.viewSimulationFlag = 'No';
        this.router.navigate(['RiskReleasePredictor/Output']);
      } else {
        this.router.navigate(['generic/Output']);
      }
    }
  }
  /**
  * Function to get template/version info by templateID
  * @param templateID unique Id of template
  */
  private getTemplateInfoById(templateID) {
    // this.setGridInputTable(GRID_DATA); // STATIC DATA
    this.loader.start();
    const requestPayload = this.payloadGenerate.getClientIdDCIDUserName();
    const params = {
      'TemplateId': templateID,
      'UseCaseName': (this.typeOfSimulation === 'notgeneric') ? 'ADSP' : this.apiCore.paramData.vdsUsecaseName,
      'ProblemType': this.apiCore.paramData.problemType
    };
    Object.assign(requestPayload, params);
    this.ingrainApiService.get('GetTemplateData', requestPayload).subscribe(
      (data) => {
        if (data) {
          // this.gridDataFeatures = null;
          this.loader.stop();
          if (!data.IsTemplates) {
            if (this.typeOfSimulation === 'notgeneric') {
              this.router.navigate(['RiskReleasePredictor/upload']);
            }
          } else {
            this.disableOutput = data.IsSimulationExist;
            this.validator.disableSaveAs = false;
            // this.validator.setRunSimulationButton(data.IsSimulationExist);
            this.apiCore.paramData.viewSimulationFlag = (data.IsSimulationExist) ? 'Yes' : 'No';
            this.apiCore.paramData.selectInputId = data.TemplateInfo.TemplateID;
            this.apiCore.paramData.UseCaseID = data.TemplateInfo.UseCaseID;
            this.apiCore.paramData.vdsUsecaseName = data.TemplateInfo.UseCaseName;
            this.apiCore.paramData.vdsUsecasedesc = data.TemplateInfo.UseCaseDescription;
            let inputSelecton = {};
            let feature;
            if (data.hasOwnProperty('TemplateInfo')) {
              this.selectedPhasesName = data.TemplateInfo.InputColumns;
              feature = data.TemplateInfo;
              this.apiCore.paramData.InputSelection = data.TemplateInfo.InputSelection;
            } else {
              this.selectedPhasesName = data.InputColumns;
              this.apiCore.paramData.InputSelection = data.InputSelection;
              feature = data;
            }
            if (this.selectedPhasesName && this.selectedPhasesName.length > 0) {
              this.selectedPhasesCount = this.selectedPhasesName.length;
            }

            if (feature.Features) {
              this.setInputSelectionRRP();
              this.setInputVersionDropDownValues(data.TemplateVersions, data);
              this.selectedInputVersion = { 'Version': data.TemplateInfo.Version, 'TemplateID': data.TemplateInfo.TemplateID };
              if (data.IsSimulationExist) {
                // this.validator.setRunSimulationButton(true);
                this.selectedSimulatedVersion.SimulationID = this.apiCore.paramData.selectSimulationId = data.SimulationID;
              } else {
                // this.validator.setRunSimulationButton(false);
                this.selectedSimulatedVersion.SimulationID = this.apiCore.paramData.selectSimulationId = data.SimulationID;
                this.loadInput();
              }
            } else {
              this.message.warning(feature.Message);
            }
          }
          // console.log(this.gridDataFeatures);
        }
      },
      (error) => {
        this.loader.stop();
        this.message.error('Something went wrong.');
      }
    );
  }

  /**
  * Function to set input version dropdown
  * @param versionsJSONObject all the version list
  * @param data will have optional grid table data 
  */
  private setInputVersionDropDownValues(versionsJSONObject, data?) {
    const list = [];
    // TemplateID: "04fa10ed-98c1-46f8-8609-3d6b2bfb0413"
    // Version: "Input Version 1"
    for (const key in versionsJSONObject) {
      if (key) {
        /*   list.push({
            'TemplateID': key,
            'Version': versionsJSONObject[key]
          }); */
        list.push({
          'TemplateID': versionsJSONObject[key].TemplateID,
          'Version': versionsJSONObject[key].Version
        });
      }
    }
    this.inputVersionDropDownList = list.reverse();
    if (this.config.flow === 'input' && data) {
      if (data.hasOwnProperty('TemplateInfo')) {
        this.numberOfFeatures = Object.entries(data.TemplateInfo.Features).length;
      } else {
        this.numberOfFeatures = Object.entries(data.Features).length;
      }

      this.setGridInputTable.emit(data);
    } else if (this.config.flow === 'output' && data) {
      if (data.IsSimulationExist) {
        this.runSimulation(data.SimulationID);
      }
    }
  }

  /**
* Function to set simulated version dropdown
* @param versionsJSONObject all the simulated version list
  @param data optional param yet to use
*/
  private setSimulatedVersionDropDownValues(versionsJSONObject, data?) {
    const list = [];
    //   SimulationID: "54d133ce-f45a-4620-b8c2-72a013559009"
    // TemplateID: "04fa10ed-98c1-46f8-8609-3d6b2bfb0413"{ 'TemplateID': '', 'SimulationID': '', 'SimulationVersion': '' }
    // Version: "Simulation Version 1"
    for (const key in versionsJSONObject) {
      if (key) {
        if (this.apiCore.paramData.selectInputId === versionsJSONObject[key].TemplateID) {
          list.push({
            'TemplateID': versionsJSONObject[key].TemplateID,
            'SimulationVersion': versionsJSONObject[key].Version,
            'SimulationID': versionsJSONObject[key].SimulationID
          });
        }
      }
    }
    this.simulatedVersionDropDownList = list;
    // if (data) {
    //   this.setGridInputTable.emit(data);
    // }
  }



  /**
   *
   * @param data selected dropdown data
   * @param typeofdropdown means input or simulated dropdown
   */
  showTemplateNameModal(data, typeofdropdown) {
    // UpdateVersionName
    this.editingDropdownType.type = typeofdropdown;
    this.editingDropdownType.data = data;
    if (typeofdropdown === 'simulateddropdown') {
      this.templateNameString = 'Edit simulation version name';
    }
    if (typeofdropdown === 'inputdropdown') {
      this.templateNameString = 'Edit input version name';
    }
    this.templateNameModalRef = this._modalService.show(this.templateNameModal, this.saveAsModalConfig);
  }

  /* -----------------Input header functionalities -------------------------------------------
  ------------------------------------------------------------------------------------------- */

  uploadFile(uploadFileTemplate) {
    this.template = 'FileUpload';
    this.file = [];
    this.selectedPhases = {};
    this.selectedPhasesCount = 0;
    this.selectedPhasesName = [];
    this.uploadModalRef = this._modalService.show(uploadFileTemplate, this.uploadModalConfig);
  }

  updateVersionName(input) {
    if (input.length === 0) {
      this.message.warning('Please enter name');
      return 0;
    }
    if (this.apiCore.isSpecialCharacter(input) === 0) {
      return 0;
    }
    const comman = this.payloadGenerate.getClientIdDCIDUserName();
    if (this.editingDropdownType.type === 'simulateddropdown') {
      this.selectedSimulatedVersion = {
        'SimulationVersion': this.editingDropdownType.data['SimulationVersion'], 'TemplateID': this.editingDropdownType.data['TemplateID'],
        'SimulationID': this.editingDropdownType.data['SimulationID']
      };
    }
    if (this.editingDropdownType.type === 'inputdropdown') {
      this.selectedInputVersion = {
        'Version': this.editingDropdownType.data['Version'],
        'TemplateID': this.editingDropdownType.data['TemplateID']
      };
    }
    const requestPayload =
      (this.editingDropdownType.type !== 'simulateddropdown') ?
        {
          'TemplateId': this.selectedInputVersion.TemplateID,
          'simulationId': null,
          'UseCaseID': this.apiCore.paramData.UseCaseID,
          'NewName': input
        }
        :
        {
          'TemplateId': this.selectedInputVersion.TemplateID,
          'simulationId': this.selectedSimulatedVersion.SimulationID,
          'UseCaseID': this.apiCore.paramData.UseCaseID,
          'NewName': input
        };
    Object.assign(requestPayload, comman);
    this.templateNameModalRef.hide();
    this.loader.start();
    this.ingrainApiService.get('UpdateVSName', requestPayload).subscribe(
      data => {
        if (data) {
          if (data.constructor === Object) {
            this.message.success('Name changed succesfully.');
            if (this.editingDropdownType.type === 'simulateddropdown') {
              this.setSimulatedVersionDropDownValues(data.SimulationVersions);
              this.selectedSimulatedVersion.SimulationID = data.SimulationID;
              // this.selectedSimulatedVersion.TemplateID = data.TemplateID;
              this.selectedSimulatedVersion.SimulationVersion = data.SimulationVersion;
              this.apiCore.paramData.selectSimulationId = this.selectedSimulatedVersion.SimulationID;
              //  this.runSimulation(this.selectedSimulatedVersion.SimulationID);
            } else {
              this.setInputVersionDropDownValues(data.TemplateVersions);
              this.getTemplateInfoById(this.selectedInputVersion.TemplateID);
            }
            // const selectedId = this.selectedInputVersion.TemplateID;
            // this.selectedInputVersion = { 'VersionName': data[selectedId], 'TemplateId': selectedId };
            this.loader.stop();
          } else if (data.constructor === String) {
            this.message.error(data);
            this.loader.stop();
          }
        } else {
          this.loader.stop();
          this.message.error('Something went wrong.');
        }
      }
    );
  }

  // Show modal save as
  showSaveAsModal(saveAsTemplate) {
    if (this.typeOfSimulation === 'notgeneric') {
      let Features = {};
      const isValid = this.gridData.isRequiredMandatory();
      const isMandatory = this.gridData.isReleaseNameMandatory();
      /*  && !isMandatory.duplicate */
      if (this.flowInfo === 'input') {
        if (isValid !== 'RedCellPresent' && isValid !== 'RedDateCellPresent' && isValid !== 'EndDate<StartDate'
          && isValid !== 'NumberLimitRange' && !isMandatory.empty) {
          const data = this.gridData.rowDataInput;
          Features = this.validator.createPhases(data, Features);
          this.saveAsInputFeatures = Features;
          // Calling UpdateTemplateInfo api
          this.saveAsModalRef = this._modalService.show(saveAsTemplate, this.saveAsModalConfig);
        } else {
          this.errorMessage(isValid);
          /*   if (isMandatory.duplicate) {this.message.error('Release name should be unique.'); } */
          if (isMandatory.empty) { this.message.error('Release name cannot be empty.'); }
        }
      } else if (this.flowInfo === 'output') {
        if (this.simulatedVersionDropDownList.length >= 10) { this.message.error('Maximum 10 output scenarios can be saved.'); } else {
          this.saveAsModalRef = this._modalService.show(saveAsTemplate, this.saveAsModalConfig);
        }
      }
    } else if (this.typeOfSimulation === 'generic') {
      // genericRowData
      let Features = {};
      if (this.flowInfo === 'input') {
        Features = this.validator.createGenericPhases(this.gridData.genericRowData);
        this.saveAsInputFeatures = Features;
        const isValid = this.validator.isMandatory();
        if (isValid.red || isValid.invalidRange) {
          if (isValid.red) { this.message.error('Please enter numeric value.'); }
          if (isValid.invalidRange) { this.message.error(' Value cannot be greater than 99999999.'); }
        } else {
          this.saveAsModalRef = this._modalService.show(saveAsTemplate, this.saveAsModalConfig);
        }
      } else if (this.flowInfo === 'output') {
        if (this.simulatedVersionDropDownList.length >= 10) { this.message.error('Maximum 10 output scenarios can be saved.'); } else {
          this.saveAsModalRef = this._modalService.show(saveAsTemplate, this.saveAsModalConfig);
        }
      }
    }

  }

  errorMessage(isValid) {
    if (isValid === 'RedCellPresent') { this.message.error('Columns cannot be empty. Please enter the values.'); }
    if (isValid === 'RedDateCellPresent') { this.message.error('Enter date in format (dd/mm/yyyy).'); }
    if (isValid === 'EndDate<StartDate') { this.message.error('Release End date should be greater than the Release start date.'); }
    if (isValid === 'NumberLimitRange') { this.message.error(' Value cannot be greater than 99999999.'); }
  }

  // Save as
  saveAs(newName) {
    // CloneTemplateInfo
    if (newName.length === 0) {
      // if (newName === undefined) {
      this.message.warning('Please enter name');
      return 0;
    }
    if (this.apiCore.isSpecialCharacter(newName) === 0) {
      return 0;
    }
    const comman = this.payloadGenerate.getClientIdDCIDUserName();
    let requestPayload = {}
    if (this.flowInfo === 'input') {
      requestPayload = {
        'TemplateID': this.selectedInputVersion.TemplateID,
        'UseCaseID': this.apiCore.paramData.UseCaseID,
        'Features': this.saveAsInputFeatures,
        'UseCaseName': this.apiCore.paramData.vdsUsecaseName,
        'ProblemType': this.apiCore.paramData.problemType,
        'TargetColumn': '',
        'NewName': newName
      }
      requestPayload['TargetColumn'] = (this.typeOfSimulation === 'generic') ? this.apiCore.paramData.targetcolumn : 'Defect';
      Object.assign(requestPayload, comman);
      this.saveAsToDB('CloneTemplateInfo', requestPayload);
    } else {
      const data = this.validator.getOutputSavePayload();
      requestPayload = {
        'TemplateID': this.selectedInputVersion.TemplateID,
        'SimulationID': this.selectedSimulatedVersion.SimulationID,
        'UseCaseID': this.apiCore.paramData.UseCaseID,
        'ProblemType': this.apiCore.paramData.problemType,
        'inputs': data['inputs'],
        'TargetColumn': '',
        'NewName': newName
      }
      requestPayload['TargetColumn'] = (this.typeOfSimulation === 'generic') ? this.apiCore.paramData.targetcolumn : 'Defect';
      if (this.typeOfSimulation !== 'generic') { requestPayload['inputs']['Observation'] = data['Observation'] }
      Object.assign(requestPayload, comman);
      this.saveAsToDB('CloneSimulation', requestPayload);
    };

  }

  saveAsToDB(domainName, requestPayload) {
    this.loader.start();
    // this.baseVersionCheckedBox = false;
    // this.outputSaveCheckBox = false;
    // this.saveNewName = '';
    // this.newSimulationVersionName = '';
    this.ingrainApiService.post(domainName, requestPayload).subscribe(
      (data) => {
        if (data) {
          if (data.constructor === Object) {
            if (data.Status === 'C') {
              this.validator.setRunSimulationButton(false);
              if (this.flowInfo === 'input') {
                this.setInputVersionDropDownValues(data.TemplateData.TemplateVersions, data.TemplateData);
                this.selectedInputVersion = { 'Version': data.TemplateVersion, 'TemplateID': data.TemplateID };
                this.apiCore.paramData.selectInputId = this.selectedInputVersion.TemplateID;
                this.apiCore.paramData.viewSimulationFlag = 'No';
                this.validator.setRunSimulationButton(data.IsSimulationExist);
              } else if (this.flowInfo === 'output') {
                // this.setInputVersionDropDownValues(data.TemplateVersions);
                this.setSimulatedVersionDropDownValues(data.SimulationData.SimulationVersions);
                this.selectedSimulatedVersion.SimulationID = data.SimulationID;
                // this.selectedSimulatedVersion.TemplateID = data.TemplateID;
                this.selectedSimulatedVersion.SimulationVersion = data.SimulationVersion;
                this.apiCore.paramData.selectSimulationId = this.selectedSimulatedVersion.SimulationID;
              }
              this.loader.stop();
              this.message.success('Data Cloned successfully.');
              // this.saveAsModalRef.hide();
            } else if (data.Status === 'E') {
              this.loader.stop();
              this.message.error(data.ErrorMessage);
              // this.saveAsModalRef.hide();
            }
          } else if (data.constructor === String) {
            this.message.error(data);
            this.loader.stop();
          }
        } else {
          this.loader.stop();
          this.message.error(data);
          this.saveAsModalRef.hide();
        }
      }
    );
  }

  saveGridData() {
    if (this.validator.disableRunSimulation === true && this.disableOutput === true) {
      this.simulationExistModelRef = this._modalService.show(this.SimulationExistModel, this.saveAsModalConfig);
    } else {
      this.saveGridDataChecked();
    }
  }
  // Save  // flowInfo --> 'input', 'output' // typeOfSimulation -->'generic' , 'notgeneric'
  saveGridDataChecked() {
    if (this.simulationExistModelRef) { this.simulationExistModelRef.hide(); }
    if (this.typeOfSimulation === 'notgeneric') {
      let Features = {};
      const isValid = this.gridData.isRequiredMandatory();
      const isMandatory = this.gridData.isReleaseNameMandatory();
      /*  && !isMandatory.duplicate */
      if (isValid !== 'RedCellPresent' && isValid !== 'RedDateCellPresent' && isValid !== 'EndDate<StartDate'
        && isValid !== 'NumberLimitRange' && !isMandatory.empty) {
        const data = this.gridData.rowDataInput;
        Features = this.validator.createPhases(data, Features);
        // Calling UpdateTemplateInfo api
        this.updateTemplateInfo(Features, 'Defect');
      } else {
        this.errorMessage(isValid);
        /*   if (isMandatory.duplicate) {this.message.error('Release name should be unique.'); } */
        if (isMandatory.empty) { this.message.error('Release name cannot be empty.'); }
      }
    } else if (this.typeOfSimulation === 'generic') {
      // genericRowData
      let Features = {};
      Features = this.validator.createGenericPhases(this.gridData.genericRowData);
      const isValid = this.validator.isMandatory();
      if (isValid.red || isValid.invalidRange) {
        if (isValid.red) { this.message.error('Please enter numeric value.'); }
        if (isValid.invalidRange) { this.message.error(' Value cannot be greater than 99999999.'); }
      } else {
        this.updateTemplateInfo(Features, this.apiCore.paramData.targetcolumn);
      }
    }
  }

  // Update template Info
  private updateTemplateInfo(Features, targetColumn) {

    const comman = this.payloadGenerate.getClientIdDCIDUserName();
    // input scrreen
    const requestPayload = {
      'TemplateId': this.selectedInputVersion.TemplateID,
      'Version': this.selectedInputVersion.Version,
      'Features': Features,
      'UseCaseName': this.apiCore.paramData.vdsUsecaseName,
      'ProblemType': this.apiCore.paramData.problemType,
      'TargetColumn': targetColumn,
      'SelectedCurrentRelease': this.apiCore.paramData.SelectedCurrentRelease
    };
    Object.assign(requestPayload, comman);
    this.loader.start();
    this.ingrainApiService.post('UpdateTemplateInfo', requestPayload).subscribe(
      (data) => {
        if (data && data.TemplateInfo && data.TemplateVersions) {
          this.selectedInputVersion = { 'Version': data.TemplateInfo.Version, 'TemplateID': data.TemplateInfo.TemplateID };
          this.setInputVersionDropDownValues(data.TemplateVersions, data);
          this.validator.setRunSimulationButton(data.IsSimulationExist);
          if (!data.IsSimulationExist) {
            this.apiCore.paramData.viewSimulationFlag = 'No';
          }
          this.disableOutput = data.IsSimulationExist;
          // this.setGridInputTable.emit(data);
          this.validator.disableSaveAs = false;
          this.loader.stop(); this.message.success('Data updated successfully.');
        } else {
          this.loader.stop(); this.message.error('Something went wrong.');
        }
      }
    );
  }

  // Run Simulation
  public runSimulation(simulatedIdByDropdown?) {
    // const isValid = this.gridData.isRequiredMandatory();
    // const isMandatory = this.gridData.isReleaseNameMandatory();
    // /*  && !isMandatory.duplicate */
    // if ((this.flowInfo === 'input') && (isValid === 'RedCellPresent' || isValid === 'RedDateCellPresent' || isValid === 'EndDate<StartDate'
    //   || isValid === 'NumberLimitRange' || !isMandatory.empty)) {
    //   this.message.error('Please check the resolve error run simulation');
    //   return 0;
    // }
    let phasecount = 0;
    /**
     * commented below code from line number 608 to 6155 for defect 1710766
      R4.1__Devtest - Ingrain [RRP]- User is getting error while changing one simulation version to another simualtion version from Simulation version drop down in RRP output screen

     */
    // let phasecount = 0;
    // for (const key in this.apiCore.paramData.InputSelection) {
    //   if (this.apiCore.paramData.InputSelection[key] === 'True') {
    //     phasecount++;
    //   } 
    // }

    // if ( this.typeOfSimulation === 'notgeneric' && (this.gridData.minPhaseError === 'Yes' || phasecount < 3)) {
    if (this.typeOfSimulation === 'notgeneric' && (this.gridData.minPhaseError === 'Yes')) {
      this.message.error(' Data is insufficient to run the simulation. Minimum 3 phases and 4 past releases required.');
      return 0;
    }
    if (this.validator.disableRunSimulation === true && this.flowInfo === 'input') {
      this.message.error('Please click on Save to proceed');
      return 0;
    }
    this.loader.start();
    if (simulatedIdByDropdown) {
      this.apiCore.paramData.selectSimulationId = simulatedIdByDropdown;
    }
    if (this.numberOfFeatures > 4 && this.typeOfSimulation === 'generic') {
      this.message.success('For what if analysis only top 4 influencers have been selected based on the feature importance.');
    }
    if ((this.config.flow === 'input') && this.typeOfSimulation === 'notgeneric' && this.apiCore.paramData.TeamCheckedBox === 'True'
      && this.apiCore.paramData.viewSimulationFlag === 'No') {
      // this.message.warning('Analysis will happen with overall team size values as there is no phase level details for the team.');
    }
    const dataRem = this.apiCore.paramData.InputSelection;
    for (const key in this.apiCore.paramData.InputSelection) {
      if (this.apiCore.paramData.InputSelection[key] === 'NA') {
        // dataRem[key]
        // dataRem[key] = 'False';
      } else {
        dataRem[key] = this.selectedPhases[key];
      }
    }
    this.ingrainApiService.get('RunSimulation', {
      'TemplateID': (this.config.flow === 'input') ? this.selectedInputVersion.TemplateID : this.apiCore.paramData.selectInputId,
      // tslint:disable-next-line: max-line-length
      'SimulationID': (this.config.flow === 'input') ? '' : this.apiCore.paramData.selectSimulationId,
      'ProblemType': this.apiCore.paramData.problemType,
      'IsPythonCall': (this.config.flow === 'input'), // IsPythonCall True then Run Simulation Input, else Run Simulation Output
      'ClientUID': sessionStorage.getItem('clientID'),
      'DCUID': sessionStorage.getItem('dcID'),
      'UserId': this.apiCore.getUserId(),
      'UseCaseID': this.apiCore.paramData.UseCaseID,
      'UseCaseName': (this.typeOfSimulation === 'notgeneric') ? 'ADSP' : this.apiCore.paramData.vdsUsecaseName,
      'RRPFlag': (this.selectedInputVersion.Version === 'Base_Version') ? true : false, // only for base version 
      'SelectedCurrentRelease': this.apiCore.paramData.SelectedCurrentRelease,
      "InputSelection": (this.typeOfSimulation === 'notgeneric') ? JSON.stringify(dataRem) : '',
    }).subscribe(
      (result) => {
        this.loader.stop();
        if (result.Status === 'C') {
          if (this.config.flow === 'input') { this.message.success('Run Simulation completed.'); }
          this.selectedSimulatedVersion.SimulationID = result.SimulationID;
          this.selectedSimulatedVersion.TemplateID = result.TemplateID;
          this.selectedSimulatedVersion.SimulationVersion = result.SimulationVersion;
          this.selectedInputVersion = { 'Version': result.TemplateVersion, 'TemplateID': result.TemplateID };
          this.apiCore.paramData.selectInputId = this.selectedInputVersion.TemplateID;
          this.apiCore.paramData.selectSimulationId = this.selectedSimulatedVersion.SimulationID;
          this.validator.disableSaveAs = false;
          this.setInputVersionDropDownValues(result.TemplateVersions);
          this.setSimulatedVersionDropDownValues(result.SimulationVersions);
          if (this.config.flow === 'input') {
            if (this.typeOfSimulation === 'notgeneric') {
              this.router.navigate(['RiskReleasePredictor/Output']);
            } else {
              this.router.navigate(['generic/Output']);
            }
          }
          if (this.config.flow === 'output') {
            this.setSimulatedData.emit(result);
          }
          // console.log(result);
        } else {
          this.message.error(result.ErrorMessage);
          this.loader.stop();
        }
      },
      (error) => {
        this.message.error(error.error.ErrorMessage);
        this.loader.stop();
      });
  }


  /* -----------------Input header functionalities -------------------------------------------
  ------------------------------------------------------------------------------------------- */


  /* -----------------Output header functionalities -------------------------------------------
 ------------------------------------------------------------------------------------------- */



  // Update Simulation Info API name : UpdateSimulation
  updateOutputSimulationInfo() {
    // output scrreen  UpdateSimulation
    this.saveOutputDataRequest = this.validator.getOutputSavePayload();
    if (this.saveOutputDataRequest) {
      this.saveOutputDataRequest['TemplateID'] = this.selectedInputVersion.TemplateID;
      this.saveOutputDataRequest['SimulationID'] = this.selectedSimulatedVersion.SimulationID;
      this.saveOutputDataRequest['UseCaseID'] = this.apiCore.paramData.UseCaseID;
      const requestPayload = this.saveOutputDataRequest;
      this.loader.start();
      this.ingrainApiService.post('UpdateSimulation', requestPayload).subscribe(
        (data) => {
          // if (data && data.TemplateInfo && data.TemplateVersions) {
          if (data === 'Success') {
            // this.selectedInputVersion = { 'VersionName': data.TemplateInfo.Version, 'TemplateId': data.TemplateInfo.TemplateID };
            // this.setInputVersionDropDownValues(data.Versions, data);
            // this.setGridInputTable.emit(data);
            this.validator.disableSaveAs = false;
            this.loader.stop(); this.message.success('Data updated successfully.');
          } else {
            this.loader.stop(); this.message.error('Something went wrong.');
          }
        }, error => {
          this.loader.stop(); this.message.error('Something went wrong');
        });
    } else {
      this.message.error('Input must be numbers or decimals.');
    }
  }

  /* -----------------Output header functionalities ----------------------- 
  ------------------------------------------------------------------------- */


  /* ----------------- File Upload  -------------------------------------
 ----------------------ADSP --------------------------------------------- */
  private setFileData(e) {
    this.file = e;
  }

  private setTemplate(name) {
    if (this.typeOfSimulation === 'notgeneric') {
      this.template = name;
    } else if (this.typeOfSimulation === 'generic') {
      this.setGenericTemplate(name);
    }
  }

  private uploadFileFromInputScreen() {

    const InputColumns = this.selectedPhasesName;
    const requestPayload = this.payloadGenerate.getFileUploadPayload();
    requestPayload['InputColumns'] = JSON.stringify(InputColumns);
    requestPayload['file'] = this.file[0];
    requestPayload['InsertBase'] = this.InsertBase;


    this.loader.start();
    this.fileApi.uploadFile('TemplateUpload', this.file, requestPayload).subscribe(
      (data) => {
        // console.log(data);
        if (data.IsUploaded === true) {
          this.message.success('File Uploaded Successfully');

          this.selectedInputVersion = { 'Version': data.Version, 'TemplateID': data.TemplateId };
          this.validator.setRunSimulationButton(data.IsSimulationExist);
          if (data.IsSimulationExist === false) {
            this.apiCore.paramData.viewSimulationFlag = 'No';
          }
          if (data.hasOwnProperty('TemplateInfo')) {
            this.selectedPhasesName = data.TemplateInfo.InputColumns;
            this.apiCore.paramData.InputSelection = data.TemplateInfo.InputSelection;
            if (data.TemplateInfo.hasOwnProperty('blankData') && data.TemplateInfo.blankData) { this.message.warning('Blank/Null values are replaced by 0'); }
            if (data.TemplateInfo.hasOwnProperty('Message') && data.TemplateInfo.Message) { this.message.warning(data.Message); }
          } else {
            this.selectedPhasesName = data.InputColumns;
            this.apiCore.paramData.InputSelection = data.InputSelection;
            if (data.hasOwnProperty('blankData') && data.blankData) { this.message.warning('Blank/Null values are replaced by 0'); }
            if (data.hasOwnProperty('Message') && data.Message) { this.message.warning(data.Message); }
          }
          if (this.selectedPhasesName && this.selectedPhasesName.length > 0) {
            this.selectedPhasesCount = this.selectedPhasesName.length;
          }

          this.setInputSelectionRRP();
          this.disableOutput = data.IsSimulationExist;
          this.setInputVersionDropDownValues(data.TemplateVersions, data);
          this.loader.stop();
        }
        if (data.IsUploaded === false) {
          this.message.error(data.ErrorMessage);
          this.template = 'FileUpload';
          this.loader.stop();
        }
        this.uploadModalRef.hide();
      },
      (error) => {
        this.template = 'FileUpload';
        this.loader.stop();
        this.message.error('Something went wrong.');
        this.uploadModalRef.hide();
      }
    );

  }
  /* ----------------- File Upload -------------------------------------------
   --------------------ADSP------------------------------------------------- */

  /* ----------------- File Upload -------------------------------------------
  -------------------- Generic ---------------------------------------------*/


  public setTargetAndUnique(template) {
    if (this.selected.target === '' || this.selected.unique === '') {
      this.message.warning('Select target, unique identifier');
    } else if (this.selected.target === this.selected.unique) {
      this.message.warning('Target and Unique ID cannot be same');
    } else {
      this.createGeneric();
    }
  }

  public checkedTarget(name, input) {

    if (this.oldSelectedTarget !== undefined) {
      if (this.oldSelectedTarget !== name) {
        this.unique.push(this.oldSelectedTarget);
      }
    }
    this.selected.target = name;
    if (this.unique.includes(this.selected.target)) {
      for (let i = 0; i < this.unique.length; i++) {
        if (this.unique[i] === this.selected.target) {
          this.unique.splice(i, 1);
        }
      }
    }
    this.oldSelectedTarget = name;
  }

  public checkedUnique(name, input) {
    this.selected.unique = name;
  }

  private setGenericTemplate(name) {

    const requestPayload = this.payloadGenerate.getFileUploadPayload();
    const fileparams = {
      'file': this.file[0]
    };
    Object.assign(requestPayload, fileparams);

    this.loader.start();
    this.fileApi.uploadFile('GenericFileUpload', this.file, requestPayload).subscribe(
      (data) => {
        if (data.IsUploaded === true) {
          this.loader.stop();
          this.message.success('File Uploaded Successfully');
          this.template = name;
          this.selected.templateID = data.TemplateId;
          this.target = Object.keys(data.TargetColumnList);
          this.unique = Object.keys(data.UniqueColumnList);
        }
        if (data.IsUploaded === false) {
          this.loader.stop();
          if (data.ErrorMessage) {
            this.message.error(data.ErrorMessage);
          } else {
            this.message.error('Kindly upload .xlsx or .csv file.');
          }
          this.template = 'FileUpload';
        }
      },
      (error) => {
        this.loader.stop();
        this.template = 'FileUpload';
        this.message.error('Something went wrong.');
      }
    );
  }

  public createGeneric() {
    const params = {
      TemplateID: this.selected.templateID,
      TargetColumn: this.selected.target,
      ProblemType: 'Generic',
      UniqueIdentifier: this.selected.unique,
      UseCaseName: this.apiCore.paramData.vdsUsecaseName,
      UseCaseDescription: this.apiCore.paramData.vdsUsecasedesc
    };

    this.loader.start();
    this.ingrainApiService.get('GetGenericUpdate', params).subscribe(
      (data) => {
        if (data.Status) {
          this.message.success('Use case created');
          this.uploadModalRef.hide();
          this.getTemplateInfoById(this.selected.templateID);
          // this.router.navigate(['generic/Input'], { queryParams: { id: this.selected.templateID }});
          // ToDO ::  bind dropdown
        } else {
          this.message.error(data.ErrorMessage);
          this.uploadModalRef.hide();
        }
        this.loader.stop();
      },
      (error) => {
        this.uploadModalRef.hide();
        this.loader.stop();
      }
    );
  }

  /* ----------------- File Upload -------------------------------------------
  -------------------- Generic --------------------------------------------- */

  navigateToRRPInput() {
    if (this.typeOfSimulation === 'notgeneric') {
      this.apiCore.paramData.viewSimulationFlag = 'Yes';
      this.router.navigate(['RiskReleasePredictor/Input']);
      this.loadInput();
    }
  }

  navigateToGenericInput() {
    if (this.typeOfSimulation === 'generic') {
      this.apiCore.paramData.viewSimulationFlag = 'Yes';
      this.router.navigate(['Generic/Input']);
      this.loadInput();
    }
  }

  saveSimulationVersionsGenericOutput() {
    if (this.outputSaveCheckBox) {
      if (!this.newSimulationVersionName || this.newSimulationVersionName === '') {
        this.message.warning('Enter version name.')
        return 0;
      }
      // this.saveAs(simulationName.model);
      this.saveAs(this.newSimulationVersionName);
    } else {
      this.updateOutputSimulationInfo();
    }
  }

  saveSimulationVersions() {
    if (this.outputSaveCheckBox) {
      if (!this.newSimulationVersionName || this.newSimulationVersionName === '' || this.newSimulationVersionName === 'Base_Version_FileUpload' || this.newSimulationVersionName === 'Base_Version') {
        this.message.warning('Enter version name.')
        return 0;
      }
      // this.saveAs(simulationName.model);
      this.saveAs(this.newSimulationVersionName);
    } else {
      this.updateOutputSimulationInfo();
    }
  }

  // outputSimulationCheck(elementRef) {
  //   if (elementRef.checked === true) {
  //     this.outputSaveCheckBox = true;
  //     this.outputSimulationTxt = false;
  //   }
  //   if (elementRef.checked === false) {
  //     this.outputSaveCheckBox = false;
  //     this.outputSimulationTxt = true;
  //   }
  // }

  saveEdits() {
    if (this.typeOfSimulation === 'notgeneric') {
      if (this.apiCore.paramData.SelectedCurrentRelease.length === 0) {
        this.message.warning('Select a release.')
        return 0;
      }
      if (this.baseVersionCheckedBox) {
        if (!this.saveNewName || this.saveNewName === '' || this.saveNewName === 'Base_Version_FileUpload' || this.saveNewName === 'Base_Version') {
          this.message.warning('Enter version name.')
          return 0;
        }
        let Features = {};
        const isValid = this.gridData.isRequiredMandatory();
        // const isMandatory = this.gridData.isReleaseNameMandatory();
        /*  && !isMandatory.duplicate */
        if (this.flowInfo === 'input') {
          if (isValid !== 'RedCellPresent' && isValid !== 'RedDateCellPresent' && isValid !== 'EndDate<StartDate'
            && isValid !== 'NumberLimitRange') {
            const data = this.gridData.rowDataInput;
            Features = this.validator.createPhases(data, Features);
            this.saveAsInputFeatures = Features;
            // Calling UpdateTemplateInfo api
            this.saveAs(this.saveNewName);
          } else {
            this.errorMessage(isValid);
          }
        } else if (this.flowInfo === 'output') {
          if (this.simulatedVersionDropDownList.length >= 10) { this.message.error('Maximum 10 output scenarios can be saved.'); } else {

          }
        }

      } else {
        this.saveGridData();
      }
    }

    if (this.typeOfSimulation === 'generic') {

      if (this.baseVersionCheckedBox) {
        if (!this.saveNewName || this.saveNewName === '') {
          this.message.warning('Enter version name.')
          return 0;
        }
        const isValid = this.gridData.isRequiredMandatory();
        // const isMandatory = this.gridData.isReleaseNameMandatory();
        /*  && !isMandatory.duplicate */
        if (this.flowInfo === 'input') {
          let Features = {};
          Features = this.validator.createGenericPhases(this.gridData.genericRowData);
          this.saveAsInputFeatures = Features;
          const isValid = this.validator.isMandatory();
          if (isValid.red || isValid.invalidRange) {
            if (isValid.red) { this.message.error('Please enter numeric value.'); }
            if (isValid.invalidRange) { this.message.error(' Value cannot be greater than 99999999.'); }
          } else {
            // Calling UpdateTemplateInfo api
            this.saveAs(this.saveNewName);
          }
        } else if (this.flowInfo === 'output') {
          if (this.simulatedVersionDropDownList.length >= 10) { this.message.error('Maximum 10 output scenarios can be saved.'); } else {

          }
        }

      } else {
        this.saveGridData();
      }
    }
  }

  private checkedPhase(name, inputRef) {
    // this.validator.setRunSimulationButton(true);
    // this.apiCore.paramData.viewSimulationFlag = 'No'
    if (this.isInitialSelection) {
      for (const v in this.apiCore.paramData.InputSelection) {
        if (this.apiCore.paramData.InputSelection[v] !== 'NA') {
          this.selectedPhases[v] = '';
          this.apiCore.paramData.InputSelection[v] = '';
        }
      }
    }
    this.selectedPhases = this.apiCore.paramData.InputSelection;
    this.selectedPhasesCount = 0;
    this.selectedPhasesName = [];
    this.selectedPhases[name] = inputRef.target.checked === true ? 'True' : 'False';
    this.isInitialSelection = false;
    for (const key in this.selectedPhases) {
      if (this.selectedPhases.hasOwnProperty(key)) {
        if (this.selectedPhases[key] === 'True') {
          this.selectedPhasesName.push(key);
          this.selectedPhasesCount++;
        }
      }
    }
    this.apiCore.paramData.InputSelection = this.selectedPhases;
  }

  private selectAllPhases() {
    // this.selectedPhases = { 'Plan': true, 'Analyze': true, 'Design': true, 'Detailed Technical Design': true, 'Build': true, 'Component Test': true, 'Assembly Test': true, 'Product Test': true };
    // this.selectedPhasesCount = this.phases.length;
    // for (let i = 0; i < this.phases.length; i++) {
    //   this.selectedPhases[this.phases[i]] = 'disabled';
    // }
    this.availableInputPhases('True');
    this.selectedPhasesCount = Object.keys(this.selectedPhases).length;
    // this.selectedPhasesName = this.selectedPhases;
  }

  private clearAllPhases() {
    // this.selectedPhases = { 'Plan': false, 'Analyze': false, 'Design': false, 'Detailed Technical Design': false, 'Build': false, 'Component Test': false, 'Assembly Test': false, 'Product Test': false };
    // this.selectedPhasesCount = 0;
    // for (let i = 0; i < this.phases.length; i++) {
    //   this.selectedPhases[this.phases[i]] = 'disabled';
    // }
    this.availableInputPhases('');
    this.selectedPhasesCount = 0;
    // this.selectedPhasesName = [];
  }

  private selectAllPhasesUpload() {
    // this.selectedPhases = { 'Plan': true, 'Analyze': true, 'Design': true, 'Detailed Technical Design': true, 'Build': true, 'Component Test': true, 'Assembly Test': true, 'Product Test': true };
    // this.selectedPhasesCount = this.phases.length;
    for (let i = 0; i < this.phases.length; i++) {
      this.selectedPhases[this.phases[i]] = 'True';
    }
    // this.availableInputPhases('True');
    this.selectedPhasesCount = Object.keys(this.selectedPhases).length;
    this.selectedPhasesName = this.phases;
  }

  private clearAllPhasesUpload() {
    // this.selectedPhases = { 'Plan': false, 'Analyze': false, 'Design': false, 'Detailed Technical Design': false, 'Build': false, 'Component Test': false, 'Assembly Test': false, 'Product Test': false };
    // this.selectedPhasesCount = 0;
    for (let i = 0; i < this.phases.length; i++) {
      this.selectedPhases[this.phases[i]] = '';
    }
    // this.availableInputPhases('False');
    this.selectedPhasesCount = 0;
    this.selectedPhasesName = [];
  }

  private availableInputPhases(selectAll: string) {
    this.validator.setRunSimulationButton(true);
    this.apiCore.paramData.viewSimulationFlag = 'No'
    for (const v in this.apiCore.paramData.InputSelection) {
      // const found = this.selectedPhasesName.findIndex(d => { return d === v })
      if (this.apiCore.paramData.InputSelection[v] !== 'NA') {
        this.selectedPhases[v] = selectAll;
        this.apiCore.paramData.InputSelection[v] = selectAll;
      }
    }

  }

  UpdatePhaseSelection() {
    if (this.selectedPhasesCount >= 3) {
      this.validator.setRunSimulationButton(true);
      this.apiCore.paramData.viewSimulationFlag = 'No'
      for (const selectedPhases in this.apiCore.paramData.InputSelection) {
        // if ( (this.apiCore.paramData.InputSelection[selectedPhases] === 'False' )) {
        if (this.apiCore.paramData.InputSelection[selectedPhases] === '') {
          this.apiCore.paramData.InputSelection[selectedPhases] = 'False';
        }
        // this.apiCore.paramData.unSelectedPhases[selectedPhases] = this.apiCore.paramData.InputSelection[selectedPhases];
        // }
      }

      const requestPayload = this.payloadGenerate.getClientIdDCIDUserName();
      const dataRem = this.apiCore.paramData.InputSelection;
      for (const key in this.apiCore.paramData.InputSelection) {
        if (this.apiCore.paramData.InputSelection[key] === 'NA') {
          // dataRem[key] = 'False';
        } else {
          dataRem[key] = this.apiCore.paramData.InputSelection[key];
        }
      }
      const params = {
        'TemplateID': this.selectedInputVersion.TemplateID,
        'UseCaseID': this.apiCore.paramData.UseCaseID,
        'ProblemType': this.apiCore.paramData.problemType,
        "UseCaseName": "ADSP",
        "InputSelection": dataRem,
        "ColSelection": "Phase"
      }
      Object.assign(requestPayload, params);
      this.ingrainApiService.post('UpdateSelection', requestPayload).subscribe((data) => {
        console.log(data);
        if (data === "Success") {
          this.message.success("Selected Phase are updated");
          this.getTemplateInfoById(this.selectedInputVersion.TemplateID);
        }
      });
    } else {
      this.message.warning('Select minimum three phases to proceed.')
    }
  }

  setInputSelectionRRP() {
    if (this.typeOfSimulation === 'notgeneric') {
      for (let i = 0; i < this.phases.length; i++) {
        if (this.apiCore.paramData.InputSelection.hasOwnProperty(this.phases[i])) {
          this.apiCore.paramData.InputSelection[this.phases[i]] = this.apiCore.paramData.InputSelection[this.phases[i]];
        } else {
          this.apiCore.paramData.InputSelection[this.phases[i]] = 'NA';
        }
      }

      for (let i = 0; i < this.phases.length; i++) {
        this.selectedPhases[this.phases[i]] = this.apiCore.paramData.InputSelection[this.phases[i]];
      }
      this.apiCore.paramData.unSelectedPhases = {};
      for (const selectedPhases in this.apiCore.paramData.InputSelection) {
        if (this.apiCore.paramData.InputSelection[selectedPhases] === 'NA' || this.apiCore.paramData.InputSelection[selectedPhases] === 'False')
          this.apiCore.paramData.unSelectedPhases[selectedPhases] = 'False';
      }
    }
  }

  public checkedUpdateBase(check) {
    this.InsertBase = (check.target.checked === true) ? true : false;
  }

  public checkedSaveInput(check) {
    this.baseVersionCheckedBox = (check.target.checked === true) ? true : null;
  }

  public checkedSaveOutput(check) {
    this.outputSaveCheckBox = (check.target.checked === true) ? true : null;
  }

}
