import { Component, EventEmitter, OnInit, Output, ViewChild, ViewEncapsulation } from '@angular/core';
import { CustomRoutingService } from 'src/app/_services/custom-routing.service';
import { CascadeModelsChartComponent } from 'src/app/_reusables/cascade-models-chart/cascade-models-chart.component';
// import { elementClassProp } from '@angular/core/src/render3';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import { CascadeModelsService } from 'src/app/_services/cascade-models.service';
import { Router, ActivatedRoute } from '@angular/router';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';
import { NONE_TYPE } from '@angular/compiler/src/output/output_ast';

@Component({
  selector: 'app-map-cascade-models',
  templateUrl: './map-cascade-models.component.html',
  styleUrls: ['./map-cascade-models.component.scss'],
  encapsulation: ViewEncapsulation.None
})
export class MapCascadeModelsComponent implements OnInit {

  constructor(private customRouter: CustomRoutingService, private _modalService: BsModalService,
    private cascadeService: CascadeModelsService, private route: ActivatedRoute,
    private ns: NotificationService, private appUtilsService: AppUtilsService,
    private router: Router, private coreUtilService: CoreUtilsService) { }

  @ViewChild(CascadeModelsChartComponent, { static: false }) cascadeMapComp: CascadeModelsChartComponent;
  modalRef: BsModalRef | null;
  cascadedId;
  config = {
    ignoreBackdropClick: true,
    class: 'deploymodle-confirmpopup',
    index: null,
    targetId: undefined
  };
  modelTemplateData = {};
  public nextDisable = true;
  /* data = [
    [
      {id: 0, column: 'Ticket No.'},
      {id: 1, column: 'Created'},
      {id: 2, column: 'Priority'},
      {id: 3, column: 'Ticket Description'},
      {id: 4, column: 'Support Group'}
    ],
    [
      {id: 5, column: 'Created'},
      {id: 6, column: 'Configuration Item'},
      {id: 7, column: 'Priority'},
      {id: 8, column: 'Description'},
      {id: 9, column: 'Ticket Importance'},
      {id: 10, column: 'Assignment Group'},
      {id: 11, column: 'Resolving Comments'},
      {id: 12, column: 'Resolved'},
      {id: 13, column: 'Closed'}
    ],
    [
      {id: 14, column: 'Created'},
      {id: 15, column: 'Configuration Item'},
      {id: 16, column: 'Priority'},
      {id: 17, column: 'Description'}
    ],
    [
      {id: 18, column: 'Description'},
      {id: 19, column: 'Ticket Importance'},
      {id: 20, column: 'Assignemnt Group'},
      {id: 21, column: 'Resolving Comments'}
    ],
    [
      {id: 22, column: 'Support Group'}
    ]
  ]; */
  data = [];
  targetIdArray = [undefined, undefined, undefined, undefined];
  uniqueIdArray = [[undefined, undefined], [undefined, undefined], [undefined, undefined], [undefined, undefined]];
  /* allAttributes = [
    ['Ticket No.', 'Created', 'Priority', 'Ticket Description', 'Support Group'],
    ['Created', 'Configuration Item', 'Priority', 'Description', 'Ticket Importance', 'Assignemnt Group',
    'Resolving Comments', 'Resolved', 'Closed'],
    ['Created', 'Configuration Item', 'Priority', 'Description'],
    ['Description', 'Ticket Importance', 'Assignemnt Group',
    'Resolving Comments'],
    ['Support Group']
  ]; */
  allAttributes = [];
  openMenu = false;
  modelNames = [];
  mappings = {};
  deliveryType = 'AD';
  objectKeys = Object.keys;
  svgWidth;
  isModelUpdated = false;
  masterData;
  svgHeight = 600;
  zoomValue = 1;
  rangeValue = 0;
  isCustomCascadeModel = false;
  @Output() modelname: EventEmitter<any> = new EventEmitter();

  ngOnInit() {
    this.nextDisable = true;
    this.route.queryParams.subscribe(params => {
      this.cascadedId = params.cascadedId;
      if (this.coreUtilService.isNil(this.cascadedId)) {
        this.cascadedId = sessionStorage.getItem('cascadedId');
      }
      sessionStorage.setItem('cascadedId', this.cascadedId);
      if (params['redirectedFromModelTemp'] === 'true') {
        const localData = JSON.parse(localStorage.getItem('localData'));
        this.modelTemplateData['category'] = localData['modelCategory'];
        this.modelTemplateData['modelName'] = localData['modelName'];
        this.modelTemplateData['correlationId'] = localData['correlationId'];
      }
     this.appUtilsService.loadingStarted();
       this.cascadeService.getCascadeModelMapping(this.cascadedId).subscribe(response => {
        this.appUtilsService.loadingEnded();
        this.modelname.emit(response.ModelName);
        if(response.IsException === true) {
          this.ns.error(response['ErrorMessage']);
        } else {
          this.masterData = response;
          this.data = response.MappingData;
          this.isCustomCascadeModel = response.IsCustomModel;
          this.svgWidth = Object.keys(this.data).length * 260;
          if (Object.keys(this.data).length === 5) {
            this.svgWidth = 1200;
          }
          this.deliveryType = response.Category;
          this.modifyData(this.data);
          this.mappings = JSON.parse(JSON.stringify(response.MappingList));
          if (!this.coreUtilService.isNil(this.mappings)) {
            this.populateTargetAndUniqueId();
        }
        }
      }, error => {
        this.appUtilsService.loadingEnded();
        this.ns.error(error.error);
      });
    });

  }

  modifyData(rawModels) {
    const dataArray = [];
    const allAttributeNamesArray = [];
    const modelNamesArray = [];
    let id = 0;
    /* rawModels.forEach((rawModel, i) => {
      const attributeArray = [];
      const attributeNames = [];
      Object.entries(rawModel.InputColumns).forEach(
        ([key, value]) => {
         const modifiedAttribute = {id: id++, column: key };
         attributeArray.push(modifiedAttribute);
         attributeNames.push(key);
        }
      );
      let modelName;
      if (i === 0) {
        modelName = {'modelName': rawModel.ModelName, 'modelIndex': 'Start' };
      } else if (i === (rawModels.length - 1)) {
        modelName = {'modelName': rawModel.ModelName, 'modelIndex': 'End' };
      } else {
        const modelIndex = 'Intermediate' + i;
        modelName = {'modelName': rawModel.ModelName, 'modelIndex': modelIndex };
      }
      dataArray.push(attributeArray);
      allAttributeNamesArray.push(attributeNames);
      modelNamesArray.push(modelName);
    }); */
    Object.entries(rawModels).forEach(
      ([key, value], i) => {
        const attributeArray = [];
        const attributeNames = [];
        Object.entries(value['InputColumns']).forEach(
          ([key1, value1]) => {
            const modifiedAttribute = {id: id++, column: key1, targetColumn : value['TargetColumn'], dataType: value1['Datatype'], uniqueValues: value1['UniqueValues'], metric: value1['Metric']  };
            attributeArray.push(modifiedAttribute);
            attributeNames.push(key1);
          }
        );
        let modelName;
        if (i === 0) {
          modelName = {'modelName': value['ModelName'], 'modelIndex': 'Start', 'correlationId': value['CorrelationId'], 'selectedTrainedModelName': value['ModelType'], targetColumn : value['TargetColumn']};
        } else if (i === (Object.keys(rawModels).length - 1)) {
          modelName = {'modelName': value['ModelName'], 'modelIndex': 'End', 'correlationId': value['CorrelationId'], 'selectedTrainedModelName': value['ModelType'], targetColumn : value['TargetColumn'] };
        } else {
          const modelIndex = 'Intermediate' + i;
          modelName = {'modelName': value['ModelName'], 'modelIndex': modelIndex, 'correlationId': value['CorrelationId'], 'selectedTrainedModelName': value['ModelType'], targetColumn : value['TargetColumn'] };
        }
        dataArray.push(attributeArray);
        allAttributeNamesArray.push(attributeNames);
        const lengthOfArrays = allAttributeNamesArray.map(a => a.length);
        const max = lengthOfArrays.reduce(function(a, b) {
          return Math.max(a, b);
        });
        this.svgHeight = 80+60+(60*max);
        /* if (max >= 10 && max <= 12) {
          this.svgHeight = 700;
        }
        if (max > 12) {
          this.svgHeight = 800;
        } */
        modelNamesArray.push(modelName);
      }
    );
    this.modelNames = modelNamesArray;
    this.data = dataArray;
    this.allAttributes = allAttributeNamesArray;
  }

  populateTargetAndUniqueId() {
    if (!this.coreUtilService.isNil(this.mappings)) {
      this.nextDisable = false;
      Object.entries(this.mappings).forEach(
        ([key, value], i) => {
          Object.entries(value).forEach(
            ([key1, value1]) => {
              if (key1 === 'UniqueMapping') {
                const uniqueIdSource = this.data[i].filter(val => val.column === value[key1].Source);
                const uniqueIdSourceId = parseInt(uniqueIdSource[0].id, 0);
                const uniqueIdTarget = this.data[i + 1].filter(val => val.column === value[key1].Target);
                const uniqueIdTargetId = parseInt(uniqueIdTarget[0].id, 0);
                this.uniqueIdArray[i][0] = uniqueIdSourceId;
                this.uniqueIdArray[i][1] = uniqueIdTargetId;
              } else if (key1 === 'TargetMapping') {
                const mapSource = this.data[i + 1].filter(val => val.column === value[key1].Target);
                const mapSourceId = parseInt(mapSource[0].id, 0);
                this.targetIdArray[i] = mapSourceId;
              }
            });
        });
    }
  }

  next(modelChangesConfirmation) {
    if(this.isCustomCascadeModel === true) {
      this.router.navigate(['/dashboard/problemstatement/cascadeModels/publishCascadeModels'], {});
    } else {
      const totalModelsLength = Object.keys(this.data).length;
      const targetArray  = this.targetIdArray.slice(0, (totalModelsLength - 1));
      if (targetArray.indexOf(undefined) > -1) {
        this.ns.error('Mapping of all the models is required.');
      } else {
        if (!this.coreUtilService.isNil(this.masterData['MappingList'])) {
          this.checkModelSequence(modelChangesConfirmation);
        } else {
          this.saveMappings();
        }
      }
    }
  }

  previous() {
    this.router.navigate(['/dashboard/problemstatement/cascadeModels/chooseCascadeModels'],
      {
        queryParams: {
       //   'modelName': this.modelName,
          'cascadedId': this.cascadedId,
          'category': this.deliveryType
        }
      });
  //  this.customRouter.redirectToPrevious();
  }

  /* drawGraph() {
    this.sankeyComp.buildLinksData(this.targetId1, this.targetId2);
  } */


  onTargetColumnChange(event, index, uniqueIdMapPopup) {
    let targetId;
    const id = event.value;
    if (id !== 'undefined') {
      if (id) {
        targetId = parseInt(id, 0);
        const uniqueIdSource = this.data[(index - 1)].filter(val => val.targetColumn === val.column);
        const uniqueIdTarget = this.data[index].filter(val => targetId === val.id);
        const validateTarget = this.validate(uniqueIdSource[0], uniqueIdTarget[0], false);
        this.nextDisable = !validateTarget;
        if (validateTarget === true) {
          this.targetIdArray[index - 1] = targetId;
          this.uniqueIdPopup(index, uniqueIdMapPopup, targetId);
        } else {
          this.ns.error('Choose appropriate attribute for Mapping.');
          this.targetIdArray[index - 1] = undefined;
          this.cascadeMapComp.buildLinksData(this.targetIdArray);
        }
      }
    } else {
      this.targetIdArray[index - 1] = undefined;
      this.cascadeMapComp.buildLinksData(this.targetIdArray);
    }
  }

  confirm(source, target, index, targetId) {
    if (source !== 'undefined' && target !== 'undefined') {
      this.modalRef.hide();
      this.onUniqueIdentifierChange(source, target, index, targetId);
    } else {
      this.ns.error('Kindly select source and target unique identifier.');
    }
  }

  cancel(index) {
    this.targetIdArray[index - 1] = undefined;
    this.cascadeMapComp.buildLinksData(this.targetIdArray);
    this.modalRef.hide();
  }

  onUniqueIdentifierChange(source, target, index, targetId) {
      const sourceUniqueId = parseInt(source, 0);
      const targetUniqueId = parseInt(target, 0);
      const uniqueIdSource = this.data[(index - 1)].filter(val => sourceUniqueId === val.id);
      const uniqueIdSourceColumn = uniqueIdSource[0].column;
      const uniqueIdTarget = this.data[index].filter(val => targetUniqueId === val.id);
      const uniqueIdTargetColumn = uniqueIdTarget[0].column;
      const mapSourceColumn = this.data[(index - 1)][0].targetColumn;
      const mapTarget = this.data[index].filter(val => targetId === val.id);
      const mapTargetColumn = mapTarget[0].column;
      const validation = this.validate(uniqueIdSource[0], uniqueIdTarget[0], true);
      this.nextDisable = !validation;
      console.log('Validation --- ' + validation);
      if (validation === true) {
        this.uniqueIdArray[index - 1][0] = sourceUniqueId;
        this.uniqueIdArray[index - 1][1] = targetUniqueId;
        if (!this.coreUtilService.isNil(this.mappings)) {
          if (Object.keys(this.mappings).length === (Object.keys(this.data).length - 1)) {
            this.mappings['Model' + index].UniqueMapping.Source = uniqueIdSourceColumn;
            this.mappings['Model' + index].UniqueMapping.Target = uniqueIdTargetColumn;
            this.mappings['Model' + index].TargetMapping.Source = mapSourceColumn;
            this.mappings['Model' + index].TargetMapping.Target = mapTargetColumn;
          } else {
            const mapping = {
              'UniqueMapping' : {
                'Source' : uniqueIdSourceColumn,
                'Target' : uniqueIdTargetColumn
              },
              'TargetMapping' : {
                'Source' : mapSourceColumn,
                'Target' : mapTargetColumn
              }
            };
            if (this.mappings === null) {
              this.mappings = {};
            }
            this.mappings['Model' + index] = mapping;
          }
        } else {
          const mapping = {
            'UniqueMapping' : {
              'Source' : uniqueIdSourceColumn,
              'Target' : uniqueIdTargetColumn
            },
            'TargetMapping' : {
              'Source' : mapSourceColumn,
              'Target' : mapTargetColumn
            }
          };
          if (this.mappings === null) {
            this.mappings = {};
          }
          this.mappings['Model' + index] = mapping;
        }
        this.cascadeMapComp.buildLinksData(this.targetIdArray);
      //  this.cascadeMapComp.buildUniqueIdentifierLinksData(this.uniqueIdArray);
      } else {
        this.ns.error('Please map appropriate unique identifier.');
      }
  }

  /* changeModelSequence(targetArray, sequence) {
    targetArray.forEach((targetId, i) => {
      if (targetId) {
        const targetItem = this.allAttributes[sequence].slice(targetId, targetId + 1);
        this.allAttributes[sequence].splice(targetId, 1);
        this.allAttributes[sequence].unshift(targetItem[0]);
        console.log(targetItem);
      }
    });
    console.log(this.allAttributes);
  } */

  backToModel() {
    this.router.navigate(['/dashboard/problemstatement/usecasedefinition'],
    {
      queryParams: {
        'modelCategory': this.modelTemplateData['category'],
        'displayUploadandDataSourceBlock': true,
        modelName: this.modelTemplateData['modelName']
      }
    });
  }

  openCloseNav() {
    this.openMenu = !this.openMenu;
  }

  uniqueIdPopup(index, uniqueIdMapPopup, targetId) {
    this.config.index = parseInt(index, 0);
    if (targetId !== undefined) {
      this.config.targetId = targetId;
    } else {
      this.config.targetId = this.targetIdArray[index - 1];
    }
    this.modalRef = this._modalService.show(uniqueIdMapPopup, this.config);
  }

  validate(source, target, isUniqueIdMapping?) {
    if(isUniqueIdMapping === true) {
      let sourceStringArray = source.uniqueValues;
      let targetStringArray = target.uniqueValues;
      if (source.dataType.includes('float') || source.dataType.includes('int') || source.dataType.includes('Integer')) {
         sourceStringArray = source.uniqueValues.map(String);
      }
      if (target.dataType.includes('float') || target.dataType.includes('int') || target.dataType.includes('Integer')) {
        targetStringArray = target.uniqueValues.map(String);
      }
      const isSubset = sourceStringArray.every(val => targetStringArray.includes(val));
      if (isSubset === true) {
        return true;
      } else {
        return false;
      }
    } else {
      if ((source.dataType.includes('float') || source.dataType.includes('int')) &&
     (target.dataType.includes('float') || target.dataType.includes('int'))) {
      if (source.metric >= (target.metric - 10) && source.metric <= (target.metric + 10)) {
        return true;
      } else {
        return false;
      }
    } else {
      let sourceStringArray = source.uniqueValues;
      let targetStringArray = target.uniqueValues;
      if (source.dataType.includes('float') || source.dataType.includes('int')) {
         sourceStringArray = source.uniqueValues.map(String);
      } else if (target.dataType.includes('float') || target.dataType.includes('int')) {
        targetStringArray = target.uniqueValues.map(String);
      }
      const isSubset = sourceStringArray.every(val => targetStringArray.includes(val));
      if (isSubset === true) {
        return true;
      } else {
        return false;
      }
    }
    }
    
  }

  checkModelSequence(modelChangesConfirmation) {
    let equalObjects = true;
    Object.entries(this.mappings).forEach(
      ([key, value]) => {
        if (JSON.stringify(this.masterData['MappingList']) !== JSON.stringify(this.mappings)) {
          equalObjects = false;
        }
    });
    if (equalObjects === true) {
      this.saveMappings();
    } else {
      this.modalRef = this._modalService.show(modelChangesConfirmation, this.config);
    }
  }

  saveMappings() {
    const payload = {
      'CascadedId' : this.cascadedId,
      'Mappings' : this.mappings,
      'isModelUpdated' : this.isModelUpdated
    };
    this.appUtilsService.loadingStarted();
    this.cascadeService.updateCascadeMapping(payload).subscribe(response => {
      this.appUtilsService.loadingEnded();
      if (response.hasOwnProperty('IsInserted')) {
        if (response.IsInserted === true) {
          if (response.Status !== 'Deployed') {
            this.router.navigate(['/dashboard/problemstatement/cascadeModels/publishCascadeModels'], {});
          } else {
            this.router.navigate(['/dashboard/problemstatement/cascadeModels/deployCascadeModels'], {});
          }
        }
      }
    }, error => {
      this.appUtilsService.loadingEnded();
      this.ns.error(error.error);
    });
  }

  onConfirm() {
    this.modalRef.hide();
    this.isModelUpdated = true;
    this.saveMappings();
  }

  onClose() {
    this.modalRef.hide();
  }

  zoom(slider, mode?) {
    let value = slider.value;
    if(mode) {
      if(mode === 'increase') {
        value = parseInt(value) + 1;
      } else {
        value = parseInt(value) - 1;
      }
    }
    if(value >= 10 && value <= 20) {
      slider.value = value;
      this.zoomValue = parseInt(slider.value)/10;
      if(slider.min && slider.max){
        this.rangeValue = Math.ceil(((slider.value - slider.min) / (slider.max - slider.min)) * 100) || 0;
        slider.setAttribute('style','--range-size:'+ this.rangeValue + '%');
      }
      this.cascadeMapComp.zoom(this.zoomValue);
    }
  }

  openFullScreen(slider) {
    slider.value = 1;
    this.zoom(slider);
    this.cascadeMapComp.openFullscreen();
  }

}
