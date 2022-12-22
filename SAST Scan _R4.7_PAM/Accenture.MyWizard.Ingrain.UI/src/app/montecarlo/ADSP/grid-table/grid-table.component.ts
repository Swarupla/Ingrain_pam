import { Component, OnInit, Input, AfterViewInit, ElementRef, TemplateRef, ViewChild } from '@angular/core';
import { GridDataService } from './grid-data.service';
import * as moment from 'moment';
import * as _ from 'lodash';
import { AlertService } from '../../services/alert-service.service';
import { BehaviorSubject } from 'rxjs';
import { SimulationHeader } from '../../shared/component/simulation-header/simulation-header.service';
import { trigger, state, animate, transition, style } from '@angular/animations'
import { ApiCore } from '../../services/api-core.service';
import { ApiService } from 'src/app/_services/api.service';
import { PayloadHelper } from '../../services/payload-helper.service';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
const _moment = moment;

@Component({
  selector: 'app-grid-table',
  templateUrl: './grid-table.component.html',
  styleUrls: ['./grid-table.component.scss'],
  animations: [
    trigger('expandCols', [
      transition(':enter', [
        style({ transform: 'translateX(50%)', opacity: 0, backgroundColor: 'white' }),
        animate('300ms', style({ transform: 'translateX(0)', opacity: 1, backgroundColor: 'white' }))
      ]),
      transition(':leave', [
        style({ transform: 'translateX(0)', opacity: 1, backgroundColor: 'white' }),
        animate('300ms', style({ transform: 'translateX(50%)', opacity: 0, backgroundColor: 'white' }))
      ])
    ])
  ],
})

export class GridTableComponent implements AfterViewInit {
  @Input() gridTableRefresh = new BehaviorSubject<any>('');
  @ViewChild('warningmessage', { static: true }) warningmessage: TemplateRef<any>;
  @ViewChild('phasesWarningMessage', { static: true }) phasesWarningMessage: ElementRef;
  gridDataFeatures;
  isRowSelectable;
  // InputColumns;
  modalRefDataQuality: BsModalRef | null;
  mddalRefphasesWarningMessage: BsModalRef | null;
  featureNameDataQualityLow = '';


  config = {
    ignoreBackdropClick: true,
    class: 'deploymodle-confirmpopup'
  };


  rowData = [];
  rowHeight = 40;
  headerHeight = 80;
  default_RowData = [];
  frameworkComponents;
  count = 0;
  disableDelete = true;
  limitRange = 99999999;

  teamSizeColSpan = 4;
  effortColSpan = 4;
  defectColSpan = 4;
  scheduleColSpan = 6;
  SelectedCurrentRelease = '';
  Effort = {
    'Phases': [],
    'ChartData': [],
    'ReleaseWiseChartData': [],
    'Visible': true,
    'Expanded': 3,
    'MainKey': 'Effort (Hrs)',
    'TotalKey': 'Overall Effort (Hrs)'
  }

  Team = {
    'Phases': [],
    'ChartData': [],
    'ReleaseWiseChartData': [],
    'Visible': true,
    'Expanded': 3,
    'MainKey': 'Team Size',
    'TotalKey': 'Overall Team Size',
  }

  Schedule = {
    'Phases': [],
    'ChartData': [],
    'ReleaseWiseChartData': [],
    'Visible': true,
    'Expanded': 3,
    'MainKey': 'Schedule (Days)',
    'TotalKey': 'Overall Schedule (Days)',
    'StartDate': 'Release Start Date (dd/mm/yyyy)',
    'EndDate': 'Release End Date (dd/mm/yyyy)',
  }

  Defect = {
    'Phases': [],
    'ChartData': [],
    'ReleaseWiseChartData': [],
    'Visible': true,
    'Expanded': 3,
    'MainKey': 'Defect',
    'TotalKey': 'Overall Defect'
  }

  releaseMessage = "For <release name> there is no effort for <Phase Name> but there is values for team size and Schedule for the same phase. Please validate";
  effortMessage = "Effort for phase <Phase Name list> does not have corresponding values for <Influencer Name>"
  realeaseMessageAfterValidation = [];
  effortMessageAfterValidation = [];
  releaseNameSelected: string;
  phaseWise: boolean;
  detailedView: boolean;
  startRowNumber: number = 0;
  lastRowNumber: number = 0;
  TeamCheckedBox;
  unselectedPhases = {};
  countUnselectedPhase = 0;
  addNewRowCount = 0;
  selectedVersionName = '';


  constructor(private gridDataService: GridDataService, private message: AlertService, public apiCore: ApiCore,
    private ingrainApiService: ApiService, private payloadGenerate: PayloadHelper, private _modalService: BsModalService,

    private tableheader: SimulationHeader) { }

  ngAfterViewInit() {
    // this.gridDataFeatures = {};
    this.gridTableRefresh.subscribe(gridDataFeatures => {

      //  if ( gridDataFeatures) {
      // this.SelectedCurrentRelease = this.apiCore.paramData.SelectedCurrentRelease;  
      this.gridDataFeatures = gridDataFeatures;
      this.gridDataService.error = {};

      this.setDefaultInputVersion();
      //  }
    });
  }

  private setDefaultInputVersion() {
    // const categoryList = GRID_DATA.TemplateInfo.Features;
    const categoryList = this.gridDataFeatures.Features;
    this.selectedVersionName = this.gridDataFeatures.Version;
    const teamSize = this.gridDataFeatures.Features[this.Team.MainKey];
    const effort = this.gridDataFeatures.Features[this.Effort.MainKey];
    const schedule = this.gridDataFeatures.Features[this.Schedule.MainKey];
    const defect = this.gridDataFeatures.Features[this.Defect.MainKey];
    this.phaseWise = true;
    this.detailedView = false;
    this.teamSizeColSpan = 4;
    this.effortColSpan = 4;
    this.defectColSpan = 4;
    this.scheduleColSpan = 6;


    this.Team.Expanded = 3;
    this.Effort.Expanded = 3;
    this.Schedule.Expanded = 3;
    this.Defect.Expanded = 3;

    this.Team.ChartData = this.setAccuracyChartReleaseWise(teamSize[this.Team.TotalKey], this.gridDataFeatures.Features['Release Name']);
    this.Effort.ChartData = this.setAccuracyChartReleaseWise(effort[this.Effort.TotalKey], this.gridDataFeatures.Features['Release Name']);
    this.Schedule.ChartData = this.setAccuracyChartReleaseWise(schedule[this.Schedule.TotalKey], this.gridDataFeatures.Features['Release Name']);
    this.Defect.ChartData = this.setAccuracyChartReleaseWise(defect[this.Defect.TotalKey], this.gridDataFeatures.Features['Release Name']);

    this.Team.Phases = Object.keys(teamSize);
    this.Effort.Phases = Object.keys(effort);
    this.Schedule.Phases = Object.keys(schedule);
    this.Defect.Phases = Object.keys(defect);



    const category = Object.keys(categoryList);

    this.default_RowData = this.gridDataService.getRowData(category, categoryList);



    // Set current as first row
    let currentReleaseRowData = [];
    const pastReleaseRowData = [];
    for (const k in this.default_RowData) {
      if (this.default_RowData[k]['Release State'] === 'Current') {
        this.apiCore.paramData.SelectedCurrentRelease = this.default_RowData[k]['Release Name'];
        this.SelectedCurrentRelease = this.default_RowData[k]['Release Name'];
        currentReleaseRowData.push((this.default_RowData[k]));
      } else if (this.default_RowData[k]['Release State'] === 'Past') {
        pastReleaseRowData.push((this.default_RowData[k]));
      }
    }

    currentReleaseRowData = currentReleaseRowData.sort((a, b) => {
      let keyA = moment(a['Schedule (Days)_Release Start Date (dd/mm/yyyy)'], 'DD/MM/YYYY')['_d'];
      let keyB = moment(b['Schedule (Days)_Release Start Date (dd/mm/yyyy)'], 'DD/MM/YYYY')['_d'];
      // const d = keyA - keyB;
      return (keyA - keyB);
    });
    currentReleaseRowData = currentReleaseRowData.reverse();

    // If gridDataFeatures.SelectedCurrentRelease  has value then update selectedCurrentRelease
    // else check for sorted current release 
    if ( this.gridDataFeatures.SelectedCurrentRelease ) {
      this.SelectedCurrentRelease = this.gridDataFeatures.SelectedCurrentRelease;
      this.apiCore.paramData.SelectedCurrentRelease = this.gridDataFeatures.SelectedCurrentRelease;
    } else {
      this.SelectedCurrentRelease = currentReleaseRowData[0]['Release Name'];
      this.apiCore.paramData.SelectedCurrentRelease = currentReleaseRowData[0]['Release Name'];
    }

    this.default_RowData = currentReleaseRowData.concat(pastReleaseRowData);

    // Assign default_RowData to rowData show column header , sub header label sequentailly.
    this.rowData = JSON.parse(JSON.stringify(this.default_RowData));
    this.gridDataService.setRowData(this.rowData);
    this.lastRowNumber = this.getNumberOfPastandCurrentRelease().currentRelease;
    this.TeamCheckedBox = this.apiCore.paramData.TeamCheckedBox === 'True' ? true : null;
    // Commented passReleaseHasNoData function
    // as Thershold percentage discard release, phases is handled while integrating data from pheonix
    // Not require to check validation for Base_Version (created from data fabric)
    if (this.selectedVersionName !== 'Base_Version') {
      // this.passReleaseHas37PercentageThersholdReleaseWise();
      // this.passReleaseHas75PercentageThersholdPhaseWise();
    }
    this.applyUnselectedPhases();
    this.effortMessageAfterValidation = [];
    this.realeaseMessageAfterValidation = [];
    this.checkEffortPhasesValuesAreZero();
    this.checkEffortPhasesValuesAreZeroNot();
    this.currentReleaseValueCheck();
  }

  applyUnselectedPhases() {
    this.unselectedPhases = this.apiCore.paramData.unSelectedPhases;
    const selectedPhases = [];
    this.countUnselectedPhase = 0;
    for (const k in this.unselectedPhases) {
      if (this.unselectedPhases[k] === 'False') {
        this.countUnselectedPhase++;
        selectedPhases.push(k);
      }
    }
    for (let i = 0; i < this.rowData.length; i++) {
      const rowIndex = i;
      this.rowData[rowIndex][this.Effort.MainKey + '_' + this.Effort.TotalKey] = 0;
      this.rowData[rowIndex][this.Team.MainKey + '_' + this.Team.TotalKey] = 0;
      this.rowData[rowIndex][this.Defect.MainKey + '_' + this.Defect.TotalKey] = 0;
      for (const subphase in this.rowData[rowIndex]) {
        if (subphase.includes(this.Schedule.MainKey)) {
          // tslint:disable-next-line: max-line-length
          if (subphase.includes('(dd/mm/yyyy)')) {
          }
        }
        else if (subphase.includes(this.Effort.MainKey)) {
          const unse = subphase.split('_')[1];
          if (!subphase.includes('Overall')) {
            if (Number(this.rowData[rowIndex][subphase]) && this.unselectedPhases[unse] !== 'False') {
              this.rowData[rowIndex][this.Effort.MainKey + '_' + this.Effort.TotalKey] += Number(this.rowData[rowIndex][subphase]);
            }
          }
        } else if (subphase.includes(this.Team.MainKey)) {
            if (this.selectedVersionName !== 'Base_Version') {
          const unse = subphase.split('_')[1];
          if (!subphase.includes('Overall')) {
            if (Number(this.rowData[rowIndex][subphase]) && this.unselectedPhases[unse] !== 'False') {
              this.rowData[rowIndex][this.Team.MainKey + '_' + this.Team.TotalKey] += Number(this.rowData[rowIndex][subphase]);
            }
          }
         } 
        } else if (subphase.includes(this.Defect.MainKey)) {
          const unse = subphase.split('_')[1];
          if (!subphase.includes('Overall')) {
            if (Number(this.rowData[rowIndex][subphase]) && this.unselectedPhases[unse] !== 'False') {
              this.rowData[rowIndex][this.Defect.MainKey + '_' + this.Defect.TotalKey] += Number(this.rowData[rowIndex][subphase]);
            }
          }
        }
      }
    }
    // this.Effort.Expanded = this.Effort.Expanded - this.countUnselectedPhase;
    // this.Team.Expanded = this.Team.Expanded - this.countUnselectedPhase;
    // this.Schedule.Expanded = this.Schedule.Expanded - this.countUnselectedPhase;
    // this.Defect.Expanded = this.Defect.Expanded - this.countUnselectedPhase;
    // this.teamSizeColSpan = this.teamSizeColSpan - this.countUnselectedPhase;
    // this.effortColSpan = this.effortColSpan - this.countUnselectedPhase;
    // this.defectColSpan = this.defectColSpan - this.countUnselectedPhase;
    // this.scheduleColSpan = this.scheduleColSpan - this.countUnselectedPhase;
    this.Team.Phases = this.Team.Phases.filter((el) => !selectedPhases.includes(el));
    this.Effort.Phases = this.Effort.Phases.filter((el) => !selectedPhases.includes(el));
    this.Schedule.Phases = this.Schedule.Phases.filter((el) => !selectedPhases.includes(el));
    this.Defect.Phases = this.Defect.Phases.filter((el) => !selectedPhases.includes(el));

  }
  showGraphicalView() {
    this.detailedView = false;
    this.lastRowNumber = this.getNumberOfPastandCurrentRelease().currentRelease;
  }

  showDetailedView() {
    this.detailedView = true;
    this.lastRowNumber = this.rowData.length;
  }

  deleteRow(rowIndex: number) {
    this.showRunSimulationButton();
    if (this.rowData[rowIndex]['Release Name'] === this.apiCore.paramData.SelectedCurrentRelease) {
      this.apiCore.paramData.SelectedCurrentRelease = "";
    }
    this.rowData.splice(rowIndex, 1);
    this.lastRowNumber = this.rowData.length;
    this.message.success('Record deleted temporarily. Click on Save to save the changes.');
  }

  insertNewRow() {
    this.addNewRowCount = this.rowData.length + 1;
    this.showRunSimulationButton();
    const rowTobeAdded = {};
    if (this.rowData.length < 20) {
      const rowSchema = this.rowData[this.rowData.length - 1];
      for (const key in rowSchema) {
        if (rowSchema) {
          if (key === 'Release Name' || key === 'Release State') {
            rowTobeAdded['Release Name'] = 'New Release' + (this.addNewRowCount)
            rowTobeAdded['Release State'] = 'Current'
          } else {

            const sdkey = this.Schedule.MainKey + '_' + this.Schedule.StartDate;
            const edkey = this.Schedule.MainKey + '_' + this.Schedule.EndDate;
            if (sdkey === key || edkey === key) {
              rowTobeAdded[key] = '01/01/2020';
            } else {
              rowTobeAdded[key] = 0;
            }
          }
        }
      }
      this.rowData.splice(0, 0, rowTobeAdded);
      this.lastRowNumber = this.rowData.length;
      this.message.success('New row added');
      // this.genericRow.push(rowTobeAdded);
      // this.gridtable.genericRowData.row = this.genericRow;
      this.tableheader.setRunSimulationButton(true);
    } else {
      this.message.warning('User can add only 20 rows');
    }
  }
  setAccuracyChart(PlotData, XAxisPointName) {
    const dataFormatted = [];
    let plotIndex = 0;
    if (PlotData && XAxisPointName) {
      for (let index = 0; index < PlotData.length; index++) {
        if (this.unselectedPhases[XAxisPointName[index]] !== 'False') {
          dataFormatted[plotIndex++] = this.chartObject(XAxisPointName[index], Number(PlotData[index]));
        }
      }
      return dataFormatted;
    }
  }

  setAccuracyChartReleaseWise(PlotData, XAxisPointName) {
    const dataFormatted = [];
    let plotIndex = 0;
    if (PlotData && XAxisPointName) {
      for (let index = 0; index < PlotData.length; index++) {
        if (this.gridDataFeatures.Features['Release State'][index] !== 'Current') {
          dataFormatted[plotIndex++] = this.chartObject(XAxisPointName[index], Number(PlotData[index]));
        }
      }
      return dataFormatted;
    }
  }

  public chartObject(xPoint, PlotData) {
    const objLineChart = {};
    objLineChart['XAxisPointName'] = xPoint;
    objLineChart['PlotData'] = PlotData;
    return objLineChart;
  }

  public showPhaseWiseDataChart() {
    const teamSize = this.gridDataFeatures.Features[this.Team.MainKey];
    const effort = this.gridDataFeatures.Features[this.Effort.MainKey];
    const schedule = this.gridDataFeatures.Features[this.Schedule.MainKey];
    const defect = this.gridDataFeatures.Features[this.Defect.MainKey];

    this.Team.ChartData = this.setAccuracyChartReleaseWise(teamSize[this.Team.TotalKey], this.gridDataFeatures.Features['Release Name']);
    this.Effort.ChartData = this.setAccuracyChartReleaseWise(effort[this.Effort.TotalKey], this.gridDataFeatures.Features['Release Name']);
    this.Schedule.ChartData = this.setAccuracyChartReleaseWise(schedule[this.Schedule.TotalKey], this.gridDataFeatures.Features['Release Name']);
    this.Defect.ChartData = this.setAccuracyChartReleaseWise(defect[this.Defect.TotalKey], this.gridDataFeatures.Features['Release Name']);
  }

  public showReleaseWiseData(releaseName: Array<string>) {
    //  console.log(releaseName[0]);
    this.phaseWise = false;
    this.releaseNameSelected = releaseName[0];
    const index = this.rowData.findIndex(d => { return d['Release Name'] === releaseName[0] });
    const XPlotName = {};
    const PlotData = {};
    XPlotName[this.Effort.MainKey] = [];
    XPlotName[this.Team.MainKey] = [];
    XPlotName[this.Schedule.MainKey] = [];
    XPlotName[this.Defect.MainKey] = [];
    PlotData[this.Effort.MainKey] = [];
    PlotData[this.Team.MainKey] = [];
    PlotData[this.Schedule.MainKey] = [];
    PlotData[this.Defect.MainKey] = [];
    for (const phase in this.rowData[index]) {
      if (phase.includes(this.Effort.MainKey)) {
        if (!phase.includes('Overall')) {
          XPlotName[this.Effort.MainKey].push(phase.replace(this.Effort.MainKey + '_', ''));
          PlotData[this.Effort.MainKey].push(this.rowData[index][phase]);
        }
      }

      if (phase.includes(this.Team.MainKey)) {
        if (!phase.includes('Overall')) {
          XPlotName[this.Team.MainKey].push(phase.replace(this.Team.MainKey + '_', ''));
          PlotData[this.Team.MainKey].push(this.rowData[index][phase]);
        }
      }

      if (phase.includes(this.Schedule.MainKey)) {
        if (!phase.includes('Overall') && !phase.includes('(dd/mm/yyyy)')) {
          XPlotName[this.Schedule.MainKey].push(phase.replace(this.Schedule.MainKey + '_', ''));
          PlotData[this.Schedule.MainKey].push(this.rowData[index][phase]);
        }
      }

      if (phase.includes(this.Defect.MainKey)) {
        if (!phase.includes('Overall')) {
          XPlotName[this.Defect.MainKey].push(phase.replace(this.Defect.MainKey + '_', ''));
          PlotData[this.Defect.MainKey].push(this.rowData[index][phase]);
        }
      }

    }

    this.Effort.ReleaseWiseChartData = this.setAccuracyChart(PlotData[this.Effort.MainKey], XPlotName[this.Effort.MainKey])
    this.Team.ReleaseWiseChartData = this.setAccuracyChart(PlotData[this.Team.MainKey], XPlotName[this.Team.MainKey])
    this.Schedule.ReleaseWiseChartData = this.setAccuracyChart(PlotData[this.Schedule.MainKey], XPlotName[this.Schedule.MainKey])
    this.Defect.ReleaseWiseChartData = this.setAccuracyChart(PlotData[this.Defect.MainKey], XPlotName[this.Defect.MainKey])

  }

  showRunSimulationButton() {
    this.tableheader.setRunSimulationButton(true);
    this.apiCore.paramData.viewSimulationFlag = 'No';
  }

  public rowValueChanged(inputRefValue: any, rowIndex: number, Influencer: any, subphaseName: string) {
    this.showRunSimulationButton();
    // this.tableheader.disableSaveAs = true;
    const sdkey = this.Schedule.MainKey + '_' + this.Schedule.StartDate;
    const edkey = this.Schedule.MainKey + '_' + this.Schedule.EndDate;
    let rowkey = (Influencer.MainKey + '_' + subphaseName + '_row=' + rowIndex);
    if (!subphaseName.includes('(dd/mm/yyyy)')) {
      this.rowData[rowIndex][Influencer.MainKey + '_' + subphaseName] = Number(inputRefValue.target.value);

      if (this.apiCore.isSpecialCharacterGeneric(inputRefValue.target.value) === 0) {
        inputRefValue = 'invalid';
        if (inputRefValue == 'invalid') {
          this.gridDataService.error[rowkey] = 'red';
        } else {
          this.gridDataService.error[rowkey] = 'white';
        }
        this.rowData[rowIndex][Influencer.MainKey + '_' + subphaseName] = inputRefValue;
        // return 0;
      } else {
        if ((inputRefValue.target.value === "" || Number(inputRefValue.target.value) === 0) &&
          this.rowData[rowIndex]['Release Name'] === this.apiCore.paramData.SelectedCurrentRelease
          && Influencer.MainKey !== this.Defect.MainKey) {
          let message = "Provide value greater than 0 for " + subphaseName + " phase, as there is past release data available for " + subphaseName + " phase else deselect the phase.";
          this.message.warning(message);
          // this.rowData[rowIndex][Influencer.MainKey + '_' + subphaseName] = 1;
          // this.gridDataService.error[rowkey] = 'red';

        } else {
          this.rowData[rowIndex][Influencer.MainKey + '_' + subphaseName] = (Number(inputRefValue.target.value) || Number(inputRefValue.target.value) === 0) ? inputRefValue.target.value : 'invalid';
          if (Number(inputRefValue.target.value) > Number(this.limitRange)) {
            this.gridDataService.error[rowkey] = 'NumberLimitRange';
          } else {
            this.gridDataService.error[rowkey] = 'white';
          }
        }
      }
    } else {
      this.rowData[rowIndex][Influencer.MainKey + '_' + subphaseName] = inputRefValue.target.value;
      const dateFormat = this.dateFormatCheck(this.rowData[rowIndex][Influencer.MainKey + '_' + subphaseName], Influencer.MainKey + '_' + subphaseName + '_row=' + rowIndex);
      if (dateFormat === 'redDate') {
        this.rowData[rowIndex][Influencer.MainKey + '_' + subphaseName] = 'invalid';
      } else {
        this.rowData[rowIndex][Influencer.MainKey + '_' + subphaseName] = inputRefValue.target.value;
      }
    }
    this.rowData[rowIndex][Influencer.MainKey + '_' + Influencer.TotalKey] = 0;
    for (const subphase in this.rowData[rowIndex]) {
      if (subphase.includes(this.Schedule.MainKey)) {

        // tslint:disable-next-line: max-line-length
        if (subphase.includes('(dd/mm/yyyy)')) {
          this.rowData[rowIndex][this.Schedule.MainKey + '_' + this.Schedule.TotalKey] = _moment(this.rowData[rowIndex][edkey], 'DD/MM/YYYY').diff(_moment(this.rowData[rowIndex][sdkey], 'DD/MM/YYYY'), 'days');
          if (this.rowData[rowIndex][this.Schedule.MainKey + '_' + this.Schedule.TotalKey] < 0) {
            this.gridDataService.error[edkey] = 'DateError';
          } else {
            this.gridDataService.error[edkey] = 'white';
          }
          if (this.rowData[rowIndex][this.Schedule.MainKey + '_' + this.Schedule.TotalKey] === 'NaN') {
            this.rowData[rowIndex][this.Schedule.MainKey + '_' + this.Schedule.TotalKey] = 0;
          }
        }
      }
      else if (subphase.includes(Influencer.MainKey)) {
        if (!subphase.includes('Overall')) {
          if (Number(this.rowData[rowIndex][subphase])) {
            this.rowData[rowIndex][Influencer.MainKey + '_' + Influencer.TotalKey] += Number(this.rowData[rowIndex][subphase]);
          }
        }
      }
    }
    this.gridDataService.setRowData(this.rowData);
  }


  public getNumberOfPastandCurrentRelease() {
    let pastRelease = 0;
    let currentRelease = 0;
    for (let i = 0; i < this.rowData.length; i++) {
      if (this.rowData[i]['Release State'] === 'Current') { currentRelease++; }
      else { pastRelease++ }
    }
    return { pastRelease, currentRelease }
  }

  private dateFormatCheck(Releasedate, rowkey) {
    const cdate = _moment(Releasedate, 'DD/MM/YYYY');
    const validdate = (Releasedate === '') ? false : _moment(cdate)['_isValid'];
    const regex = /^([0-2][0-9]|(3)[0-1])(\/)(((0)[0-9])|((1)[0-2]))(\/)\d{4}$/
    const isValidFormat = regex.test(Releasedate);
    if (validdate && isValidFormat) {
      this.gridDataService.error[rowkey] = 'white';
      return 'white';
    } else {
      this.gridDataService.error[rowkey] = 'redDate';
      return 'redDate';
    }
  }

  updateTeamSizeCheckBox(value) {
    this.showRunSimulationButton();
    this.TeamCheckedBox = value.target.checked;
    this.apiCore.paramData.TeamCheckedBox = this.TeamCheckedBox === true ? 'True' : 'False';
    this.UpdatePhaseSelection();
  }

  UpdatePhaseSelection() {
    const requestPayload = this.payloadGenerate.getClientIdDCIDUserName();

    const params = {
      'TemplateID': this.apiCore.paramData.selectInputId,
      'UseCaseID': this.apiCore.paramData.UseCaseID,
      'ProblemType': this.apiCore.paramData.problemType,
      "UseCaseName": "ADSP",
      "MainSelection": {
        "Effort (Hrs)": "True",
        "Team Size": this.apiCore.paramData.TeamCheckedBox,
        "Schedule (Days)": "True",
        "Defect": "True"
      },
      "ColSelection": "Influencer"
    }
    Object.assign(requestPayload, params);
    this.ingrainApiService.post('UpdateSelection', requestPayload).subscribe((data) => {
      console.log(data);
    });
  }

  selectCurrentRelease(releaseName: string) {
    this.showRunSimulationButton();
    this.apiCore.paramData.SelectedCurrentRelease = releaseName;
  }


  checkEffortPhasesValuesAreZero() {
    const phases = ['Plan', 'Analyze', 'Design', 'Detailed Technical Design', 'Build', 'Component Test', 'Assembly Test', 'Product Test'];
    console.log(this.rowData);
    let message = []; let phase = [];
    for (const rowNo in this.rowData) {
      phase = [];
      if (this.rowData.hasOwnProperty(rowNo)) {
        for (let p = 0; p < this.Team.Phases.length; p++) {
          if (!this.Team.Phases[p].includes("Overall"))
            if (this.rowData[rowNo][this.Effort.MainKey + '_' + this.Team.Phases[p]] === 0 &&
              this.rowData[rowNo][this.Team.MainKey + '_' + this.Team.Phases[p]] !== 0 &&
              this.rowData[rowNo][this.Schedule.MainKey + '_' + this.Team.Phases[p]] !== 0) {
              phase.push(this.Team.Phases[p]);
            }
        }
        if (phase.length > 0) {
          message.push(this.releaseMessage.replace('<release name>', this.rowData[rowNo]['Release Name']).replace('<Phase Name>', phase.join(',')) + " .");
        }
      }
    }
    if (message.length > 0) {
      this.realeaseMessageAfterValidation = message;
      // this.message.warning(message);
      // this.modalRefDataQuality = this._modalService.show(this.warningmessage, this.config);
    }
  }

  checkEffortPhasesValuesAreZeroNot() {
    // const phases = ['Plan', 'Analyze', 'Design', 'Detailed Technical Design', 'Build', 'Component Test', 'Assembly Test', 'Product Test'];
    console.log(this.rowData);
    let message = []; let phase = []; let influencer = [];
    let me = '';
    for (const rowNo in this.rowData) {
      phase = [];
      influencer = [];
       me = '';
      if (this.rowData.hasOwnProperty(rowNo)) {
        for (let p = 0; p < this.Team.Phases.length; p++) {
          phase = [];
          influencer = [];
          if (!this.Team.Phases[p].includes("Overall"))
            if (this.rowData[rowNo][this.Effort.MainKey + '_' + this.Team.Phases[p]] !== 0 && (
              this.rowData[rowNo][this.Team.MainKey + '_' + this.Team.Phases[p]] === 0 ||
              this.rowData[rowNo][this.Schedule.MainKey + '_' + this.Team.Phases[p]] === 0)) {
              if (this.rowData[rowNo][this.Team.MainKey + '_' + this.Team.Phases[p]] === 0) {
                influencer.push(this.Team.MainKey);
                phase.push(this.Team.Phases[p]);
                influencer = Array.from(new Set(influencer));
                phase = Array.from(new Set(phase));
                if (influencer.length > 0 ) {
                  me = (this.effortMessage.replace('<Phase Name list>', phase.join(', ')).replace('<Influencer Name>', influencer.join(',')) + " .");
                }
              }
              if (this.rowData[rowNo][this.Schedule.MainKey + '_' + this.Team.Phases[p]] === 0) {
                phase.push(this.Team.Phases[p]);
                influencer.push(this.Schedule.MainKey);
                influencer = Array.from(new Set(influencer));
                phase = Array.from(new Set(phase));
                if (influencer.length > 0 ) {
                  me = (this.effortMessage.replace('<Phase Name list>', phase.join(', ')).replace('<Influencer Name>', influencer.join(',')) + " .");
                }
              }
            }
        }
        if ( me !== '') {
          message.push(me);
        }

      }
    }
    if (message.length > 0) {
      this.effortMessageAfterValidation = message;
    }

    if (this.realeaseMessageAfterValidation.length > 0 || this.effortMessageAfterValidation.length > 0) {
      this.modalRefDataQuality = this._modalService.show(this.warningmessage, this.config);
    }
  }


  currentReleaseValueCheck() {
    const subphaseName = "";
    const phase = [];
    for (const rowNo in this.rowData) {
      if (this.rowData[rowNo]['Release State'] === 'Current' &&
        this.apiCore.paramData.SelectedCurrentRelease === this.rowData[rowNo]['Release Name']) {
        for (let p = 0; p < this.Team.Phases.length; p++) {
          if (!this.Team.Phases[p].includes("Overall"))
            if (this.rowData[rowNo][this.Effort.MainKey + '_' + this.Team.Phases[p]] === 0 ||
              this.rowData[rowNo][this.Team.MainKey + '_' + this.Team.Phases[p]] === 0 ||
              this.rowData[rowNo][this.Schedule.MainKey + '_' + this.Team.Phases[p]] === 0) {
              phase.push(this.Team.Phases[p]);
            }
        }
      }
    }
    if (phase.length > 0) {
      let message = "Provide value greater than 0 for " + phase.join(',') + " phase, as there is past release data available for " + phase.join(',') + " phase else deselect the phase.";
      this.message.warning(message);
    }
  }


  passReleaseHasNoData() {
    // No past release data has <Phase name> phase data hence the phase will not be considered for simulation
    // const phases = ['Plan', 'Analyze', 'Design', 'Detailed Technical Design', 'Build', 'Component Test', 'Assembly Test', 'Product Test'];
    console.log(this.rowData);
    const warningmessage = 'No past release data has <Phase name> phase data hence the phase will not be considered for simulation';
    let message = '';
    let phase = [];
    let phaseWithData = [];
    let influencer = [];
    for (const rowNo in this.rowData) {
      phase = [];
      phaseWithData = [];
      influencer = [];
      if (this.rowData.hasOwnProperty(rowNo) && this.rowData[rowNo]['Release State'] === 'Past') {
        for (let p = 0; p < this.Team.Phases.length; p++) {
          if (!this.Team.Phases[p].includes("Overall")) {
            if (this.rowData[rowNo][this.Effort.MainKey + '_' + this.Team.Phases[p]] === 0 ||
              this.rowData[rowNo][this.Team.MainKey + '_' + this.Team.Phases[p]] === 0 ||
              this.rowData[rowNo][this.Schedule.MainKey + '_' + this.Team.Phases[p]] === 0) {
              phase.push(this.Team.Phases[p]);
            }
            if (this.rowData[rowNo][this.Effort.MainKey + '_' + this.Team.Phases[p]] !== 0 &&
              this.rowData[rowNo][this.Team.MainKey + '_' + this.Team.Phases[p]] !== 0 &&
              this.rowData[rowNo][this.Schedule.MainKey + '_' + this.Team.Phases[p]] !== 0) {
              phaseWithData.push(this.Team.Phases[p]);
            }
          }
        }
      }
    }
    if (phaseWithData.length < 3) {
      this.gridDataService.minPhaseError = 'Yes';
      this.teamSizeColSpan = phaseWithData.length + 1;
      this.effortColSpan = phaseWithData.length + 1;
      this.defectColSpan = phaseWithData.length + 1;
      this.scheduleColSpan = phaseWithData.length + 3;
    } else {
      this.gridDataService.minPhaseError = '';
      this.teamSizeColSpan = 4;
      this.effortColSpan = 4;
      this.defectColSpan = 4;
      this.scheduleColSpan = 6;
    }
    if (phase.length > 0) {
      message = (warningmessage.replace('<Phase name>', phase.join(',')));
      this.message.warning(message);
    }
    phase = Array.from(new Set(phase));


    // this.Team.Expanded = phaseWithData.length + 1;
    // this.Effort.Expanded = phaseWithData.length + 1;
    // this.Schedule.Expanded = phaseWithData.length + 1;
    // this.Defect.Expanded = phaseWithData.length + 1;
    for (let i = 0; i < phase.length; i++) {
      this.apiCore.paramData.InputSelection[phase[i]] = 'NA';
    }
    this.apiCore.paramData.unSelectedPhases = {};
    for (const selectedPhases in this.apiCore.paramData.InputSelection) {
      if (this.apiCore.paramData.InputSelection[selectedPhases] === 'NA' || this.apiCore.paramData.InputSelection[selectedPhases] === 'False')
        this.apiCore.paramData.unSelectedPhases[selectedPhases] = 'False';
    }
    console.log(this.apiCore.paramData.unSelectedPhases);
    console.log(this.apiCore.paramData.InputSelection);
    // if ( message.length > 0) {
    //   this.effortMessageAfterValidation = message;
    // }

    // if ( this.realeaseMessageAfterValidation.length > 0 || this.effortMessageAfterValidation.length > 0) {
    // this.modalRefDataQuality = this._modalService.show(this.warningmessage, this.config);
    // }
  }



  passReleaseHas75PercentageThersholdPhaseWise() {
    // No past release data has <Phase name> phase data hence the phase will not be considered for simulation
    // const phases = ['Plan', 'Analyze', 'Design', 'Detailed Technical Design', 'Build', 'Component Test', 'Assembly Test', 'Product Test'];
    console.log(this.rowData);
    const warningmessage = '<Phase name> phase is removed due to insufficient data';
    let message = '';
    let phase = [];
    let phaseWithData = [];
    let influencer = [];
    let effortCount = 0;
    let teamCount = 0;
    let scheduleCount = 0;
    phase = [];
    phaseWithData = [];
    influencer = [];
    for (let p = 0; p < this.Team.Phases.length; p++) {
      effortCount = 0;
      teamCount = 0;
      scheduleCount = 0;
      if (!this.Team.Phases[p].includes("Overall")) {
        for (const rowNo in this.rowData) {
          if (this.rowData.hasOwnProperty(rowNo) && this.rowData[rowNo]['Release State'] === 'Past') {

            if (this.rowData[rowNo][this.Effort.MainKey + '_' + this.Team.Phases[p]] === 0) {
              effortCount++;
            }
            if (this.rowData[rowNo][this.Team.MainKey + '_' + this.Team.Phases[p]] === 0) {
              teamCount++;
            }
            if (this.rowData[rowNo][this.Schedule.MainKey + '_' + this.Team.Phases[p]] === 0) {
              scheduleCount++;
            }
          }
        }
      }
      if (!this.Team.Phases[p].includes("Overall")) {
      if ((this.getNumberOfPastandCurrentRelease().pastRelease * (0.75) >= effortCount)
        || (this.getNumberOfPastandCurrentRelease().pastRelease * (0.75) >= teamCount)
        || (this.getNumberOfPastandCurrentRelease().pastRelease * (0.75) >= scheduleCount)) {
        phaseWithData.push(this.Team.Phases[p]);
      } else {
        phase.push(this.Team.Phases[p]);
      }
     }
    }
    phaseWithData = Array.from(new Set(phaseWithData));
    if (phaseWithData.length < 3) {
      // this.gridDataService.minPhaseError = 'Yes';
      this.teamSizeColSpan = phaseWithData.length + 1;
      this.effortColSpan = phaseWithData.length + 1;
      this.defectColSpan = phaseWithData.length + 1;
      this.scheduleColSpan = phaseWithData.length + 3;
    } else {
      // this.gridDataService.minPhaseError = '';
      this.teamSizeColSpan = 4;
      this.effortColSpan = 4;
      this.defectColSpan = 4;
      this.scheduleColSpan = 6;
    }
    phase = Array.from(new Set(phase));
    if (phase.length > 0) {
      message = (warningmessage.replace('<Phase name>', phase.join(',')));
      this.message.warning(message);
    }



    // this.Team.Expanded = phaseWithData.length + 1;
    // this.Effort.Expanded = phaseWithData.length + 1;
    // this.Schedule.Expanded = phaseWithData.length + 1;
    // this.Defect.Expanded = phaseWithData.length + 1;
    for (let i = 0; i < phase.length; i++) {
      this.apiCore.paramData.InputSelection[phase[i]] = 'NA';
    }
    this.apiCore.paramData.unSelectedPhases = {};
    for (const selectedPhases in this.apiCore.paramData.InputSelection) {
      if (this.apiCore.paramData.InputSelection[selectedPhases] === 'NA' || this.apiCore.paramData.InputSelection[selectedPhases] === 'False')
        this.apiCore.paramData.unSelectedPhases[selectedPhases] = 'False';
    }
    console.log(this.apiCore.paramData.unSelectedPhases);
    console.log(this.apiCore.paramData.InputSelection);
    // if ( message.length > 0) {
    //   this.effortMessageAfterValidation = message;
    // }

    // if ( this.realeaseMessageAfterValidation.length > 0 || this.effortMessageAfterValidation.length > 0) {
    // this.modalRefDataQuality = this._modalService.show(this.warningmessage, this.config);
    // }
  }

  passReleaseHas37PercentageThersholdReleaseWise() {
    // No past release data has <Phase name> phase data hence the phase will not be considered for simulation
    // const phases = ['Plan', 'Analyze', 'Design', 'Detailed Technical Design', 'Build', 'Component Test', 'Assembly Test', 'Product Test'];
    console.log(this.rowData);
    const warningmessage = '<Phase name> release is removed due to insufficient data';
    let message = '';
    let phase = [];
    let phaseWithData = [];
    let influencer = [];
    let rowReleaseCount = 0;
    let countSelectedPhase = 0;
    for (const selectedPhases in this.apiCore.paramData.InputSelection) {
      if (this.apiCore.paramData.InputSelection[selectedPhases] === 'True')
        countSelectedPhase++;
    }
    phase = [];
    phaseWithData = [];
    influencer = [];

    for (const rowNo in this.rowData) {
      rowReleaseCount = 0;
      for (let p = 0; p < this.Team.Phases.length; p++) {
        if (this.rowData.hasOwnProperty(rowNo) && this.rowData[rowNo]['Release State'] === 'Past') {
          if (!this.Team.Phases[p].includes("Overall")) {
            if (this.rowData[rowNo][this.Effort.MainKey + '_' + this.Team.Phases[p]] === 0) {
              rowReleaseCount++;
            }
            if (this.rowData[rowNo][this.Team.MainKey + '_' + this.Team.Phases[p]] === 0) {
              rowReleaseCount++;
            }
            if (this.rowData[rowNo][this.Schedule.MainKey + '_' + this.Team.Phases[p]] === 0) {
              rowReleaseCount++;
            }
          } 
        }
      }
      const index = Number(rowNo) * 1;
      if ((countSelectedPhase * 3 * 0.375 + 1) < rowReleaseCount) {
        phase.push(this.rowData[index]['Release Name']);
        this.rowData.splice(index, 1);
      }
    }
    phaseWithData = Array.from(new Set(phaseWithData));
    if (this.getNumberOfPastandCurrentRelease().pastRelease < 4) {
      this.gridDataService.minPhaseError = 'Yes';
      // this.teamSizeColSpan = phaseWithData.length + 1;
      // this.effortColSpan = phaseWithData.length + 1;
      // this.defectColSpan = phaseWithData.length + 1;
      // this.scheduleColSpan = phaseWithData.length + 3;
    } else {
      this.gridDataService.minPhaseError = '';
      // this.teamSizeColSpan = 4;
      // this.effortColSpan = 4;
      // this.defectColSpan = 4;
      // this.scheduleColSpan = 6;
    }
    phase = Array.from(new Set(phase));
    if (phase.length > 0) {
      message = (warningmessage.replace('<Phase name>', phase.join(',')));
      this.message.warning(message);
    }



    // this.Team.Expanded = phaseWithData.length + 1;
    // this.Effort.Expanded = phaseWithData.length + 1;
    // this.Schedule.Expanded = phaseWithData.length + 1;
    // this.Defect.Expanded = phaseWithData.length + 1;
    // for ( let i = 0; i < phase.length; i++) {
    //   this.apiCore.paramData.InputSelection[phase[i]] = 'NA';
    // }
    // this.apiCore.paramData.unSelectedPhases = {};
    // for (const selectedPhases in this.apiCore.paramData.InputSelection) {
    // if (this.apiCore.paramData.InputSelection[selectedPhases] === 'NA' || this.apiCore.paramData.InputSelection[selectedPhases] === 'False' )
    //   this.apiCore.paramData.unSelectedPhases[selectedPhases] = 'False';
    // }
    // console.log(this.apiCore.paramData.unSelectedPhases);
    // console.log(this.apiCore.paramData.InputSelection);
    // if ( message.length > 0) {
    //   this.effortMessageAfterValidation = message;
    // }

    // if ( this.realeaseMessageAfterValidation.length > 0 || this.effortMessageAfterValidation.length > 0) {
    // this.modalRefDataQuality = this._modalService.show(this.warningmessage, this.config);
    // }
  }

}
