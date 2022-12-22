import { Component, OnInit } from '@angular/core';
import { ApiCore } from '../../services/api-core.service';
import { BehaviorSubject } from 'rxjs';
import { GridDataService } from '../grid-table/grid-data.service';

@Component({
  selector: 'app-input-simulation',
  templateUrl: './input-simulation.component.html',
  styleUrls: ['./input-simulation.component.scss']
})
export class InputSimulationComponent {

  public gridDataFeatures;

  public gridTableRefresh = new BehaviorSubject<any>('');


  constructor( public gridDataService: GridDataService, public apiCore: ApiCore) { }


  public setGridInputTable(data) {
    if (data.hasOwnProperty('TemplateInfo')) {
      this.gridDataFeatures = data.TemplateInfo;
    } else {
      this.gridDataFeatures = data;
    }
    // 'Plan'	,'Analyze',	'Design',	'Detailed Technical Design',	'Build',	'Component Test',	'Assembly Test',	'Product Test'

    this.gridDataService.error = {};
    this.gridDataService.rowDataInput = [];
    this.gridDataService.adspMainColumn = [];
    this.gridDataService.adspMainColumn = this.gridDataFeatures.InputColumns;
    this.apiCore.paramData.TeamCheckedBox = this.gridDataFeatures.MainSelection['Team Size']; 
    // this.gridDataFeatures = GRID_DATA.TemplateInfo;
    // const clonedData = { 'gridDataFeatures' : JSON.parse(JSON.stringify(this.gridDataFeatures)) , changed : 'Yes'};


    // this.gridDataFeatures.Feature

    this.gridDataFeatures.Features['Team Size'] = this.setPhasesInSequence(this.gridDataFeatures.Features['Team Size']);
    this.gridDataFeatures.Features['Effort (Hrs)'] = this.setPhasesInSequence(this.gridDataFeatures.Features['Effort (Hrs)']);
    this.gridDataFeatures.Features['Schedule (Days)'] = this.setPhasesInSequence(this.gridDataFeatures.Features['Schedule (Days)']);
    this.gridDataFeatures.Features['Defect'] = this.setPhasesInSequence(this.gridDataFeatures.Features['Defect']);

    const clonedData =  JSON.parse(JSON.stringify(this.gridDataFeatures));
    this.gridTableRefresh.next(clonedData);
  }

  public setPhasesInSequence(influenceData, schedule?) {
    const Phases = {
      'Plan' : []	,
      'Analyze': [],
      'Design': [],	'Detailed Technical Design': [],	'Build': [],	'Component Test': [],	'Assembly Test': [],	'Product Test': []
    }
    Phases['Plan'] = (  influenceData.hasOwnProperty('Plan'))  ? influenceData['Plan']: '';
    Phases['Analyze'] =  ( influenceData.hasOwnProperty('Analyze')) ? influenceData['Analyze'] : '';
    Phases['Design'] = ( influenceData.hasOwnProperty('Design') ) ? influenceData['Design']: '';
    Phases['Detailed Technical Design'] =  (influenceData.hasOwnProperty('Detailed Technical Design') ) ?  influenceData['Detailed Technical Design'] : '';
    Phases['Build'] = ( influenceData.hasOwnProperty('Build')) ? influenceData['Build']: '';
    Phases['Component Test'] = ( influenceData.hasOwnProperty('Component Test')) ? influenceData['Component Test']: '';
    Phases['Assembly Test'] =  ( influenceData.hasOwnProperty('Assembly Test') ) ? influenceData['Assembly Test'] : '';
    Phases['Product Test'] = ( influenceData.hasOwnProperty('Product Test')) ? influenceData['Product Test']: '';
    if ( influenceData.hasOwnProperty('Overall Effort (Hrs)')) {
    Phases['Overall Effort (Hrs)'] = influenceData['Overall Effort (Hrs)'];
    }

    if ( influenceData.hasOwnProperty('Overall Team Size')) {
      Phases['Overall Team Size'] = influenceData['Overall Team Size'];
    }

    if ( influenceData.hasOwnProperty('Overall Schedule (Days)')) {
      Phases['Release Start Date (dd/mm/yyyy)'] = influenceData['Release Start Date (dd/mm/yyyy)'];
      Phases['Release End Date (dd/mm/yyyy)'] = influenceData['Release End Date (dd/mm/yyyy)'];
      Phases['Overall Schedule (Days)'] = influenceData['Overall Schedule (Days)'];
    }

    if ( influenceData.hasOwnProperty('Overall Defect')) {
      Phases['Overall Defect'] = influenceData['Overall Defect'];
    }

   

    const PhasesInSequence = {};
    for( const k in Phases) {
      if ( Phases[k]) {
      PhasesInSequence[k] = Phases[k]
      }
    }
    return PhasesInSequence;
  }

}
