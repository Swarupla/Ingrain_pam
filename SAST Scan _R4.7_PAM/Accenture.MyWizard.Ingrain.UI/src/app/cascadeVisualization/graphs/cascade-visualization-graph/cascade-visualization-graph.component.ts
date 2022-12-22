import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import * as d3 from 'd3';
import { hierarchy, tree } from 'd3-hierarchy';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { CascadeVisualizationService } from '../../services/cascade-visualization.service';
import { DialogService } from 'src/app/dialog/dialog.service';
import { ShowCascadeDataComponent } from '../../show-data/show-data.component';
import d3Tip from 'd3-tip';
import { ActivatedRoute, Router } from '@angular/router';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import { ExcelService } from 'src/app/_services/excel.service';
import { throwError, of, timer, EMPTY } from 'rxjs';
import { NotificationData } from 'src/app/_services/usernotification';
import * as $ from 'jquery';
import { EnvironmentService } from 'src/app/_services/EnvironmentService';
import { ApiService } from 'src/app/_services/api.service';
import { UserNotificationpopupComponent } from 'src/app/components/user-notificationpopup/user-notificationpopup.component';
declare var userNotification: any;

interface IGraphData {
    name: string;
    parent: string;
    totalValue: string;
    categories: any;
    children: any;
    modelName: string;
    featureValue: any;
    targetName: string;
}

@Component({
    selector: 'app-cascade-visualization-graph',
    templateUrl: './cascade-visualization-graph.component.html',
    styleUrls: ['./cascade-visualization-graph.component.scss']
})
export class CascadeVisualizationGraphComponent implements OnInit {
    isnavBarToggle: boolean;

    constructor(private cascadeVisService: CascadeVisualizationService, private ns: NotificationService,
        private dialogService: DialogService, private route: ActivatedRoute, private router: Router,
        private appUtilsService: AppUtilsService, private _modalService: BsModalService, private _excelService: ExcelService,
        private envService: EnvironmentService, private api: ApiService) {
        this.route.queryParams.subscribe(params => {
            this.cascadedId = params['CascadedId'];
            this.uniqueId = params['UniqueId'];
            this.modelName = params['ModelName'];
            this.dcid = params['DeliveryConstructUId'];
            this.clientId = params['ClientUId'];
        });
    }

    @ViewChild('cascadeVisualization', { static: true }) element: ElementRef;
    htmlElement: HTMLElement;
    host;
    width;
    height;
    svg;
    treeData = [{
        'name': 'null',
        'children': []
    }];
    tree;
    root;
    nodeWidth;
    nodeHeight;
    cascadedId;
    uniqueId;
    treemap;
    _this = this;
    dataForGraph = [];
    graphNodes = {} as IGraphData;
    modelName;
    createdDate;
    modelLastPredictionTime;

    sourceTotalLength = 0;
    allowedExtensions = ['.xlsx', '.csv'];
    files = [];
    entityArray = [];
    uploadFilesCount = 0;
    agileFlag: boolean;
    view = 'file';
    dcid;
    clientId;
    modalRef: BsModalRef;
    config = {
        backdrop: true,
        class: 'deploymodle-confirmpopupCascadeVisualization'
    };
    progress = 0;
    isUploading = false;
    modelType = 'Classification';
    cascadeVisData;
    dataforDownload;
    isBoth;
    isOnlyFileupload;
    isonlySingleEntity;
    isMultipleEntities;
    dataUploadValidationMessage;

    pager: any = {};
    pagedItems: any[];
    dataForVisualization;
    categoryType;
    timerSubscripton: any;
    infoMessage = `<div>
  <b>Export File Password Hint:</b><br>
  &nbsp;&nbsp;&nbsp;&nbspFirst four digits of account id followed by First character of First Name in upper case followed by First character of Last Name in small case.<br>
  &nbsp;&nbsp;&nbsp;&nbsp <b>Example 1:</b> Account id having four or more digits:<br>
  &nbsp;&nbsp;&nbsp;&nbspIf your account id is 12345678 and   your Name is - John Doe then the password would be 1234Jd<br>
  &nbsp;&nbsp;&nbsp;&nbsp <b>Example 2:</b> Account id having less than 4 digits:<br>
  &nbsp;&nbsp;&nbsp;&nbspIf your account id is 57 and   your Name is - John Doe then the password would be 0057Jd<br>
  &nbsp;&nbsp;&nbsp;&nbspClick on <img src="assets/images/user.svg" width="12" height="12"> top right corner of the screen to see your details like name (Last Name, First Name) and account id. Click on <img src="assets/images/icon-myw-eye.svg" width="12" height="12"> to view the account id.
  </div>`;
    instanceType: string;
    env: string;

    ngOnInit() {
        this.instanceType = sessionStorage.getItem('Instance');
        this.env = sessionStorage.getItem('Environment');
        this.api.openDisclaimer();
        this.appUtilsService.loadingStarted();
        this.htmlElement = this.element.nativeElement;
        this.host = d3.select(this.htmlElement);
        this.width = 1800;
        this.height = 1000;
        this.cascadeVisData = this.cascadeVisService.getCascadeVisData();
        if (Object.keys(this.cascadeVisData).length !== 0) {
            this.dataforDownload = this.cascadeVisData['dataForDownload'];
            this.isBoth = this.cascadeVisData['isBoth'];
            this.isOnlyFileupload = this.cascadeVisData['isOnlyFileupload'];
            this.isonlySingleEntity = this.cascadeVisData['isonlySingleEntity'];
            this.isMultipleEntities = this.cascadeVisData['isMultipleEntities'];
            this.dataUploadValidationMessage = this.cascadeVisData['dataUploadValidationMessage'];
            this.categoryType = this.cascadeVisData['categoryType'];
            this.getCascadeVisualization();
        } else {
            this.cascadeVisService.getCascadeInfluencers(this.cascadedId)
                .subscribe(colData => {
                    this.dataforDownload = colData;
                    this.cascadeVisService.cascadeVisData.dataForDownload = this.dataforDownload;
                    this.cascadeVisService.cascadeVisData.isOnlyFileupload = colData['IsonlyFileupload'];
                    this.cascadeVisService.cascadeVisData.isBoth = colData['IsBoth'];
                    this.cascadeVisService.cascadeVisData.isonlySingleEntity = colData['IsonlySingleEntity'];
                    this.cascadeVisService.cascadeVisData.isMultipleEntities = colData['IsMultipleEntities'];
                    this.cascadeVisService.cascadeVisData.categoryType = colData['Category'];
                    if (colData['IsonlyFileupload'] === true) {
                        this.dataUploadValidationMessage = 'Sub-models are created using File upload. Please select appropriate data source.';
                    } else if (colData['IsBoth'] === true) {
                        this.dataUploadValidationMessage = 'Few Sub-models are created using File. Please select appropriate data source.';
                    }
                    if (Object.keys(this.cascadeVisData).length !== 0) {
                        this.dataforDownload = this.cascadeVisData['dataForDownload'];
                        this.isBoth = this.cascadeVisData['isBoth'];
                        this.isOnlyFileupload = this.cascadeVisData['isOnlyFileupload'];
                        this.isonlySingleEntity = this.cascadeVisData['isonlySingleEntity'];
                        this.isMultipleEntities = this.cascadeVisData['isMultipleEntities'];
                        this.dataUploadValidationMessage = this.cascadeVisData['dataUploadValidationMessage'];
                        this.categoryType = this.cascadeVisData['categoryType'];
                    }
                    //  if (colData['IsVisualizationAvaialble'] === true) {
                    this.uniqueId = colData['UniqueId'];
                    this.cascadeVisService.cascadeVisData.uniqueId = this.uniqueId;
                    this.getCascadeVisualization();
                    //  }
                }, error => {
                    this.appUtilsService.loadingEnded();
                    throwError(error);
                });
        }
        /*     this.isUploading = false;
              let data= {
              "CascadedId": "caa5d0b2-0ddd-471b-b4da-3509806a6333",
              "ModelCreatedDate": "2021-03-08 10:21:25",
              "ModelLastPredictionTime": "2021-03-10 11:25:18.688672",
              "Status": "C",
              "Progress": "100",
              "Message": "success",
              "ErrorMessage": null,
              "UniqueId": "aef7bba4-fa68-45ef-a021-b6e9b1286ea4",
              "IsException": false,
              "Visualization": [
                  {
                      "PredictionProbability": [
                          {
                              "Id": "16884.924",
                              "IdName": "charges",
                              "TargetName" : "region",
                              "Categories": [
                                  {
                                      "name": "southwest",
                                      "value": 31.480000000000004
                                  },
                                  {
                                      "name": "southeast",
                                      "value": 26.169999999999998
                                  },
                                  {
                                      "name": "northeast",
                                      "value": 22.17
                                  },
                                  {
                                      "name": "northwest",
                                      "value": 20.169999999999998
                                  }
                              ]
                          },
                          {
                              "Id": "16244.924",
                              "IdName": "charges",
                              "TargetName" : "region",
                              "Categories": [
                                  {
                                      "name": "northwest",
                                      "value": 29.15
                                  },
                                  {
                                      "name": "northeast",
                                      "value": 26.040000000000003
                                  },
                                  {
                                      "name": "southeast",
                                      "value": 24.759999999999998
                                  },
                                  {
                                      "name": "southwest",
                                      "value": 20.05
                                  }
                              ]
                          }
                      ],
                      "FeatureWeights": {
                          "DeployedTill": "Model5",
                          "16884.924": {
                              "Model1": {
                                  "age": -0.09070091449835843,
                                  "sex": -0.15994236804781292
                              },
                              "Model2": {
                                  "casVizDev1_smoker": 0.028244451809262158,
                                  "children": -0.010079691493732527,
                                  "casVizDev1_Proba1": -0.537672554036665
                              },
                              "Model3": {
                                  "age": 0.0461596702129918,
                                  "casVizDev2_bmi": 0.0020279701950854353,
                                  "sex": -0.010678894830418832
                              },
                              "Model4": {
                                  "smoker": 0.09780466320036778,
                                  "bmi": 0.012339918542332526,
                                  "casVizDev3_children": 0.0,
                                  "casVizDev3_Proba1": -0.012101157311236848
                              },
                              "Model5": {
                                  "casVizDev4_Proba1": -0.00605812096688404,
                                  "casVizDev4_sex": -0.027234696893510494,
                                  "smoker": -0.032287174416524025
                              },
                              "Clickable": {
                                  "Model2": "casVizDev1_smoker",
                                  "Model3": "casVizDev2_bmi",
                                  "Model4": "casVizDev3_children",
                                  "Model5": "casVizDev4_sex"
                              },
                              "ModelName": {
                                "Model1": "Cascade 1",
                                "Model2": "Cascade 2",
                                "Model3": "Cascade 3",
                                "Model4": "Cascade 4",
                                "Model5": "Cascade 5"
                              },
                              "FeatureValues":{
                                    "Model1_age":'23',
                                   "Model1_sex": 'female',
                                   "Model2_casVizDev1_smoker": 'yes',
                                   "Model2_children": '5',
                                   "Model2_casVizDev1_Proba1": '2.2'
                              }
                          },
                          "16244.924": {
                              "Model1": {
                                  "sex": 0.17241375752770619,
                                  "age": 0.12543705549078518
                              },
                              "Model2": {
                                  "casVizDev1_Proba1": 0.6731085319997334,
                                  "children": -0.0003783129636875802,
                                  "casVizDev1_smoker": -0.025511279323331765
                              },
                              "Model3": {
                                  "age": 0.027239072154123883,
                                  "sex": 0.012027072179403527,
                                  "casVizDev2_bmi": -0.0007724085055908402
                              },
                              "Model4": {
                                  "casVizDev3_Proba1": 0.10181605736412297,
                                  "casVizDev3_children": 0.0,
                                  "bmi": -0.01576187806444452,
                                  "smoker": -0.09238144747700727
                              },
                              "Model5": {
                                  "smoker": 0.031891612994832286,
                                  "casVizDev4_Proba1": 0.00650806335115382,
                                  "casVizDev4_sex": -0.023081535956881775
                              },
                              "Clickable": {
                                  "Model2": "casVizDev1_smoker",
                                  "Model3": "casVizDev2_bmi",
                                  "Model4": "casVizDev3_children",
                                  "Model5": "casVizDev4_sex"
                              },
                              "ModelName": {
                                "Model1": "Cascade 1",
                                "Model2": "Cascade 2",
                                "Model3": "Cascade 3",
                                "Model4": "Cascade 4",
                                "Model5": "Cascade 5"
                              },
                              "FeatureValues":{
                                "Model1_sex":'female',
                                "Model1_age": '44',
                                "Model2_casVizDev1_Proba1": '54',
                                "Model2_children": '2',
                                "Model2_casVizDev1_smoker": 'no'
                              }
                          }
                      }
                  }
              ]
          };
          this.formatJson(data.Visualization[0]);
          this.drawChart(); */
        /* this.treeData = [{
          "name": "null",
         "children" : [{
            "name": "Level 1: A",
          "parent": "null",
          "totalValue": "90",
          "categories": [{'name': 'Category 1', 'value': '30'},{'name': 'Category 1', 'value': '20'}, {'name': 'Category 1', 'value': '10'},{'name': 'Category 1', 'value': '40'}],
            "children": [
              {
                "name": "Level 2: A",
            "totalValue": "60%",
            "parent": "Level 1: A",
            "categories": [{'name': 'Category 1', 'value': '20'}],
                "children": [
                  { 
              "name": "Level 3: A",
              "totalValue": "20%",
              "parent": "Level 2: A",
              "categories": [{'name': 'Category 1', 'value': '70'}],
              "children": [
                { 
                "name": "Level 4: A",
                "totalValue": "40%",
                "parent": "Level 3: A",
                "categories": [{'name': 'Category 1', 'value': '30'}],
                "children": [
                  { "name": "Level 5: A", "totalValue": "60", "parent": "Level 4: A", "categories": [{'name': 'Category 1', 'value': '70'}] },
                  { "name": "Level 5: B", "totalValue": "30", "parent": "Level 4: A", "categories": [{'name': 'Category 1', 'value': '50'}] }
                ] 
                },
                { "name": "Level 4: B", "totalValue": "20%", "parent": "Level 3: A", "categories": [{'name': 'Category 1', 'value': '40'}] }
              ]
                },
                  { "name": "Level 3: B", "totalValue": "90%", "parent": "Level 2: A", "categories": [{'name': 'Category 1', 'value': '20'}] },
              { "name": "Level 3: C", "totalValue": "90%", "parent": "Level 2: A", "categories": [{'name': 'Category 1', 'value': '30'}] },
              { "name": "Level 3: D", "totalValue": "90%", "parent": "Level 2: A", "categories": [{'name': 'Category 1', 'value': '40'}] },
              { "name": "Level 3: E", "totalValue": "90%", "parent": "Level 2: A", "categories": [{'name': 'Category 1', 'value': '50'}] }
                ]
              },
              { "name": "Level 2: B", "totalValue": "90%", "parent": "Level 1: A", "categories": [{'name': 'Category 1', 'value': '60'}] },
            { "name": "Level 2: C", "totalValue": "90%", "parent": "Level 1: A", "categories": [{'name': 'Category 1', 'value': '30'}] },
            { "name": "Level 2: D", "totalValue": "90%", "parent": "Level 1: A", "categories": [{'name': 'Category 1', 'value': '25'}] },
            { "name": "Level 2: E", "totalValue": "90%", "parent": "Level 1: A", "categories": [{'name': 'Category 1', 'value': '15'}] }
            ]
          },
          {
            "name": "Level 1: B",
          "parent": "null",
          "totalValue": "90",
          "categories": [{'name': 'Category 1', 'value': '40'},{'name': 'Category 1', 'value': '10'}, {'name': 'Category 1', 'value': '10'},{'name': 'Category 1', 'value': '10'},{'name': 'Category 1', 'value': '10'}],
            "children": [
              {
                "name": "Level 2: A",
            "totalValue": "60%",
            "parent": "Level 1: B",
            "categories": [{'name': 'Category 1', 'value': '20'},{'name': 'Category 1', 'value': '80'}],
                "children": [
                  { 
              "name": "Level 3: A",
              "totalValue": "20%",
              "parent": "Level 2: A",
              "categories": [{'name': 'Category 1', 'value': '70'},{'name': 'Category 1', 'value': '30'}],
              "children": [
                { 
                "name": "Level 4: A",
                "totalValue": "40%",
                "parent": "Level 3: A",
                "categories": [{'name': 'Category 1', 'value': '30'},{'name': 'Category 1', 'value': '30'}],
                "children": [
                  { "name": "Level 5: A", "totalValue": "60", "parent": "Level 4: A", "categories": [{'name': 'Category 1', 'value': '30'},{'name': 'Category 1', 'value': '70'}] },
                  { "name": "Level 5: B", "totalValue": "30", "parent": "Level 4: A", "categories": [{'name': 'Category 1', 'value': '50'},{'name': 'Category 1', 'value': '50'}] }
                ] 
                },
                { "name": "Level 4: B", "totalValue": "20%", "parent": "Level 3: A", "categories": [{'name': 'Category 1', 'value': '40'},{'name': 'Category 1', 'value': '60'}] }
              ]
                },
                  { "name": "Level 3: B", "totalValue": "90%", "parent": "Level 2: A", "categories": [{'name': 'Category 1', 'value': '20'},{'name': 'Category 1', 'value': '80'}] },
              { "name": "Level 3: C", "totalValue": "90%", "parent": "Level 2: A", "categories": [{'name': 'Category 1', 'value': '30'},{'name': 'Category 1', 'value': '70'}] },
              { "name": "Level 3: D", "totalValue": "90%", "parent": "Level 2: A", "categories": [{'name': 'Category 1', 'value': '40'},{'name': 'Category 1', 'value': '60'}] },
              { "name": "Level 3: E", "totalValue": "90%", "parent": "Level 2: A", "categories": [{'name': 'Category 1', 'value': '50'},{'name': 'Category 1', 'value': '50'}] }
                ]
              },
              { "name": "Level 2: B", "totalValue": "90%", "parent": "Level 1: B", "categories": [{'name': 'Category 1', 'value': '60'},{'name': 'Category 1', 'value': '40'}] },
            { "name": "Level 2: C", "totalValue": "90%", "parent": "Level 1: B", "categories": [{'name': 'Category 1', 'value': '30'},{'name': 'Category 1', 'value': '70'}] },
            { "name": "Level 2: D", "totalValue": "90%", "parent": "Level 1: B", "categories": [{'name': 'Category 1', 'value': '25'},{'name': 'Category 1', 'value': '75'}] },
            { "name": "Level 2: E", "totalValue": "90%", "parent": "Level 1: B", "categories": [{'name': 'Category 1', 'value': '15'},{'name': 'Category 1', 'value': '85'}] }
            ]
          },
          {
            "name": "Level 1: C",
          "parent": "null",
          "totalValue": "90",
          "categories": [{'name': 'Category 1', 'value': '40'},{'name': 'Category 1', 'value': '10'}, {'name': 'Category 1', 'value': '10'},{'name': 'Category 1', 'value': '10'},{'name': 'Category 1', 'value': '10'},{'name': 'Category 1', 'value': '10'},{'name': 'Category 1', 'value': '10'}],
            "children": [
              {
                "name": "Level 2: A",
            "totalValue": "60%",
            "parent": "Level 1: C",
            "categories": [{'name': 'Category 1', 'value': '20'},{'name': 'Category 1', 'value': '80'}],
                "children": [
                  { 
              "name": "Level 3: A",
              "totalValue": "20%",
              "parent": "Level 2: A",
              "categories": [{'name': 'Category 1', 'value': '70'},{'name': 'Category 1', 'value': '30'}],
              "children": [
                { 
                "name": "Level 4: A",
                "totalValue": "40%",
                "parent": "Level 3: A",
                "categories": [{'name': 'Category 1', 'value': '30'},{'name': 'Category 1', 'value': '30'}],
                "children": [
                  { "name": "Level 5: A", "totalValue": "60", "parent": "Level 4: A", "categories": [{'name': 'Category 1', 'value': '30'},{'name': 'Category 1', 'value': '70'}] },
                  { "name": "Level 5: B", "totalValue": "30", "parent": "Level 4: A", "categories": [{'name': 'Category 1', 'value': '50'},{'name': 'Category 1', 'value': '50'}] }
                ] 
                },
                { "name": "Level 4: B", "totalValue": "20%", "parent": "Level 3: A", "categories": [{'name': 'Category 1', 'value': '40'},{'name': 'Category 1', 'value': '60'}] }
              ]
                },
                  { "name": "Level 3: B", "totalValue": "90%", "parent": "Level 2: A", "categories": [{'name': 'Category 1', 'value': '20'},{'name': 'Category 1', 'value': '80'}] },
              { "name": "Level 3: C", "totalValue": "90%", "parent": "Level 2: A", "categories": [{'name': 'Category 1', 'value': '30'},{'name': 'Category 1', 'value': '70'}] },
              { "name": "Level 3: D", "totalValue": "90%", "parent": "Level 2: A", "categories": [{'name': 'Category 1', 'value': '40'},{'name': 'Category 1', 'value': '60'}] },
              { "name": "Level 3: E", "totalValue": "90%", "parent": "Level 2: A", "categories": [{'name': 'Category 1', 'value': '50'},{'name': 'Category 1', 'value': '50'}] }
                ]
              },
              { "name": "Level 2: B", "totalValue": "90%", "parent": "Level 1: C", "categories": [{'name': 'Category 1', 'value': '60'},{'name': 'Category 1', 'value': '40'}] },
            { "name": "Level 2: C", "totalValue": "90%", "parent": "Level 1: C", "categories": [{'name': 'Category 1', 'value': '30'},{'name': 'Category 1', 'value': '70'}] },
            { "name": "Level 2: D", "totalValue": "90%", "parent": "Level 1: C", "categories": [{'name': 'Category 1', 'value': '25'},{'name': 'Category 1', 'value': '75'}] },
            { "name": "Level 2: E", "totalValue": "90%", "parent": "Level 1: C", "categories": [{'name': 'Category 1', 'value': '15'},{'name': 'Category 1', 'value': '85'}] }
            ]
          }
          ]
        }]; */
    }

    getCascadeVisualization() {
        this.cascadeVisService.getCascadeVisualization(this.cascadedId, this.uniqueId).subscribe(data => {
            this.createdDate = data['ModelCreatedDate'];
            this.modelLastPredictionTime = data['ModelLastPredictionTime'];
            this.modelType = data['ModelType'];
            if (data['IsException'] === false && data['Status'] === 'C') {
                this.appUtilsService.loadingEnded();
                this.htmlElement.innerHTML = '';
                this.host.innerhtml = '';
                this.dataForVisualization = data.Visualization[0];
                this.setPage(1);
                //  this.formatJson(data.Visualization[0]);
                this.isUploading = false;
                //  this.drawChart();
            } else if (data['Status'] === 'E') {
                this.appUtilsService.loadingEnded();
                this.ns.error('Something went wrong.');
            } else {
                this.retryCascadeVisualization();
            }
        }, error => {
            this.appUtilsService.loadingEnded();
            this.ns.error('Something went wrong.');
        });
    }

    retryCascadeVisualization() {
        this.timerSubscripton = timer(5000).subscribe(() => this.getCascadeVisualization());
        return this.timerSubscripton;
    }

    formatJson(data) {
        this.treeData = [{
            'name': 'null',
            'children': []
        }];
        const _this = this;
        const influencers = data.PredictionProbability;
        const featureWeights = data.FeatureWeights;
        let lastModel;
        Object.entries(influencers).forEach(
            ([key, value]) => {
                let mainParent;
                this.graphNodes = {} as IGraphData;
                this.graphNodes.name = value['IdName'];
                this.graphNodes.parent = 'null';
                this.graphNodes.featureValue = value['Id'];
                this.graphNodes.targetName = value['TargetName'];
                this.graphNodes.totalValue = value['Outcome'] ? value['Outcome'][0].value : '';
                this.graphNodes.categories = value['Categories'] ? value['Categories'] : value['Outcome'];
                if (value['Outcome']) {
                    value['Outcome'][0].name = value['IdName'];
                }
                Object.entries(featureWeights).forEach(
                    ([key1, value1]) => {
                        const deployedTill = featureWeights['DeployedTill'];
                        lastModel = deployedTill.charAt(String(deployedTill).length - 1);
                        if (key1 === 'DeployedTill') {
                            lastModel = String(value1).charAt(String(value1).length - 1);
                        } else if (key1 === String(value['Id'])) {
                            mainParent = value['Id'];
                            let children = {};
                            Object.entries(value1).map(function (key2, index) {
                                const nestedNode = value1['Clickable']['Model' + (index + 1)];
                                let parent = value1['Clickable']['Model' + (index + 2)];
                                const modelName = value1['ModelName']['Model' + (index + 1)];
                                const featureValues = value1['FeatureValues'];
                                if (key2[0] === ('Model' + (index + 1))) {
                                    if (parent === undefined) {
                                        parent = mainParent;
                                    }
                                    const modelNumber = 'Model' + (index + 1);
                                    const childNodes = _this.createChildNodes(value1[key2[0]], parent, nestedNode, children, modelName, modelNumber, featureValues);
                                    children = childNodes;
                                    if (key2[0] === ('Model' + lastModel)) {
                                        _this.graphNodes.children = childNodes;  // For attching child nodes to parent's children node
                                    }
                                }
                            });
                        }
                    }
                );
                _this.treeData[0].children.push(this.graphNodes);
            }
        );
    }

    createChildNodes(obj, parent, nestedNode, children, modelName, modelNumber, featureValues) {
        const childArray = [];
        Object.entries(obj).forEach(
            ([key1, value1]) => {
                const childNode = {} as IGraphData;
                childNode.parent = parent;
                childNode.totalValue = String(value1);
                childNode.name = key1;
                childNode.modelName = modelName;
                childNode.featureValue = featureValues[modelNumber + '_' + key1];
                const categories = [{ 'name': key1, 'value': String(value1) }];
                childNode.categories = categories;
                if (nestedNode === key1) {
                    if (children) {
                        childNode.children = children;
                    }
                }
                childArray.push(childNode);
            });
        return childArray;
    }

    downloadData() {
        const self = this;
        if (this.instanceType === 'PAM' || this.env === 'FDS' || this.env === 'PAM') {
            this.dialogService.open(UserNotificationpopupComponent, {}).afterClosed.subscribe(confirmationflag => {
                if (confirmationflag === true) {
                    if (self.dataforDownload) {
                        self.ns.success('Your Data will be downloaded shortly');
                        self.dataforDownload = self.dataforDownload['InputSample'] || [];
                        self._excelService.exportAsExcelFile(self.dataforDownload, 'TemplateDownloaded');
                    }
                }
            });
        } else {
            userNotification.showUserNotificationModalPopup();
            $(".notification-button-close").click(function () {
                if (self.dataforDownload) {
                    self.ns.success('Your Data will be downloaded shortly');
                    self.dataforDownload = self.dataforDownload['InputSample'] || [];
                    self._excelService.exportAsPasswordProtectedExcelFile(self.dataforDownload, 'TemplateDownloaded').subscribe(response => {
                        self.ns.warning('Downloaded file is password protected click on Info icon to view the Login Hint');
                        let binaryData = [];
                        binaryData.push(response);
                        let downloadLink = document.createElement('a');
                        downloadLink.href = window.URL.createObjectURL(new Blob(binaryData, { type: 'blob' }));
                        downloadLink.setAttribute('download', 'TemplateDownloaded' + '.zip');
                        document.body.appendChild(downloadLink);
                        downloadLink.click();
                    }, (error) => {
                        self.ns.error(error);
                    });
                }
            });
        }
    }

    toggleNavBar() {
        this.isnavBarToggle = !this.isnavBarToggle;
        return false;
    }

    drawChart() {
        const _this = this;
        this.htmlElement.innerHTML = '';
        _this.host.innerhtml = '';
        const margin = {
            top: 20,
            right: 120,
            bottom: 20,
            left: 20
        },
            width = _this.width - margin.right - margin.left,
            height = _this.height - margin.top - margin.bottom;
        this.nodeWidth = 200;
        this.nodeHeight = 15;
        this.tree = d3.tree().size([height, width]);

        this.svg = this.host.append('svg')
            .attr('width', width + margin.right + margin.left)
            .attr('height', height + margin.top + margin.bottom)
            .append('g')
            .attr('transform', 'translate(' + (margin.left + 40) + ',' + margin.top + ')');

        // declares a tree layout and assigns the size
        this.treemap = d3.tree().size([height, width]);

        // Assigns parent, children, height, depth
        this.root = d3.hierarchy(this.treeData[0], function (d) { return d.children; });
        this.root.x0 = height / 2;
        this.root.y0 = 0;

        /* _this.root.children.forEach(function(d) {
          d._children = d.children;
          d.children = null;
        }); */

        _this.root.children.forEach(function (d, i) {
            _this.root.children[i].children.forEach(collapse);
        });

        _this.root.children.forEach(function (d, i) {
            if (i !== 0) {
                d._children = d.children;
                d.children = null;
            }
        });

        update(this.root);

        function update(source) {
            const tip = d3Tip();
            tip.attr('class', 'd3-tip casVis')
                .html(function (d) {
                    if (d.data) {
                        if (d.data.categories[0].elPosition === 'first') {
                            d = d.data.categories[0];
                        }
                    }
                    if (d.hasParent === false) {
                        const name = d.data ? d.data.name : d.name;
                        const value = d.data ? d.data.totalValue : d.value;
                        const featureValue = d.data ? d.data.featureValue : d.featureValue;
                        const targetName = d.data ? d.data.targetName : d.targetName;
                        if (_this.modelType === 'Regression') {
                            return `<div>
                    <div><strong>Target Name:</strong> <span>${targetName}</span></div>
                    <div><strong>Target Value:</strong> <span>${value}</span></div>
                  </div>`;
                        } else {
                            return `<div>
                    <div><strong>Target Name:</strong> <span>${targetName}</span></div>
                    <div><strong>Category Name:</strong> <span>${name}</span></div>
                    <div><strong>Category Probability:</strong> <span>${value}</span></div>
                  </div>`;
                        }
                    } else {
                        const name = d.data ? d.data.name : d.name;
                        const value = d.data ? d.data.totalValue : d.value;
                        const featureValue = d.data ? d.data.featureValue : d.featureValue;
                        return `<div>
                    <div><strong>Feature Name:</strong> <span>${name}</span></div>
                    <div><strong>Feature Weight:</strong> <span>${value}</span></div>
                    <div><strong>Feature Value:</strong> <span>${featureValue}</span></div>
                  </div>`;
                    }
                });
            _this.svg.call(tip);
            const div = d3.select('body').append('div')
                .attr('class', 'cascade-tooltip')
                .style('opacity', 0)
                .style('z-index', 1051);

            let i = 0;
            const duration = 750;
            const colorScale = d3.scaleOrdinal(d3.schemePaired);
            const x = d3.scaleLinear()
                .domain([0, 100])
                .range([0, 200]);

            // Assigns the x and y position for the nodes
            const treeData = _this.treemap(_this.root);

            // Compute the new tree layout.
            const nodes = treeData.descendants().reverse();
            const links = treeData.descendants().slice(1);

            // Normalize for fixed-depth.
            nodes.forEach(function (d, i) {
                if (d.parent === null) {
                    d.y = d.depth * (_this.nodeWidth * 1.5);
                } else {
                    d.y = (d.depth - 1) * (_this.nodeWidth * 1.5);
                }
            });

            // Update the nodes…
            const node = _this.svg.selectAll('g.node')
                .data(nodes, function (d) {
                    return d.id || (d.id = ++i);
                });

            // Enter any new nodes at the parent's previous position.
            const nodeEnter = node.enter().append('g')
                .attr('class', function (d) {
                    return 'node level-' + d.depth;
                })
                .attr('transform', function (d) {
                    return 'translate(' + source.y0 + ',' + source.x0 + ')';
                })
                .on('click', collapse);

            nodeEnter.append('path')
                .attr('class', 'rect-node')
                .attr('d', function (d) {
                    const radius = _this.nodeHeight / 2;
                    return leftRoundedRectData(radius);
                })
                .style('fill', '#E4E4E4');

            nodeEnter.append('g').attr('class', 'rects')
                .selectAll('.data-rects').data(function (d, i) {
                    d.data.categories = d.data.categories !== undefined ? d.data.categories : 0;
                    if (d.data.categories) {
                        let hasChildren = false;
                        if (d.data.children || d.data._children) {
                            hasChildren = true;
                        }
                        let hasParent = false;
                        if (d.data.parent !== 'null') {
                            hasParent = true;
                        }
                        let xVal = 0;
                        d.data.categories.forEach(function (g, j: number) {
                            g['hasParent'] = hasParent;
                            g['hasChildren'] = hasChildren;
                            g['featureValue'] = d.data.featureValue;
                            g['targetName'] = d.data.targetName;
                            if (hasParent === false) {
                                let previousxVal = 0;
                                if (j > 0) {
                                    previousxVal = x(d.data.categories[j - 1].value);
                                }
                                xVal = xVal + previousxVal;
                                if (j === (d.data.categories.length - 1) && (d.data.categories.length !== 1)) {
                                    g['elPosition'] = 'last';
                                    g['xVal'] = xVal;
                                } else if (j === 0) {
                                    g['elPosition'] = 'first';
                                    g['xVal'] = xVal;
                                } else {
                                    g['elPosition'] = 'middle';
                                    g['xVal'] = xVal;
                                }
                            } else {
                                g['elPosition'] = 'first';
                                g['xVal'] = 0;
                            }
                        });
                    }
                    return d.data.categories;
                }).enter().append('path')
                .attr('d', function (d) {
                    const radius = _this.nodeHeight / 2;
                    const xPos = d.xVal;
                    if (d.elPosition === 'first') {
                        return firstElement(radius, xPos, x(d.value));
                    } else if (d.elPosition === 'last' && Math.ceil((xPos + x(d.value))) >= _this.nodeWidth) {
                        return lastElement(radius, xPos, x(d.value));
                    } else {
                        return middleElement(radius, xPos, x(d.value));
                    }
                })
                .attr('class', 'node')
                .attr('x', function (d, i) {
                    if (d.hasParent === false) {
                        return x(d.value);
                    } else {
                        return 0;
                    }
                })
                .style('fill', function (d, i) {
                    if (d.hasParent === false) {
                        return colorScale(i);
                    } else {
                        if (d.value >= 0) {
                            return 'grey';
                        } else {
                            return 'lightgrey';
                        }
                    }
                })
                .on('mouseover', function (d, i) {
                    d3.select(this).transition()
                        .duration('50')
                        .attr('font-weight', '900')
                        .attr('cursor', 'pointer');
                    tip.show(d, this);
                })
                .on('mouseout', function (d, i) {
                    d3.select(this).transition()
                        .duration('50')
                        .attr('font-weight', '700');
                    tip.hide(d, this);
                });

            // Add labels for the nodes
            nodeEnter.append('text')
                .attr('dy', '30')
                .attr('font-family', 'sans-serif')
                .attr('class', 'influencer')
                .attr('x', function (d) {
                    return 0;
                })
                .style('fill', function (d) {
                    if (d.data.parent === 'null') {
                        return 'black';
                    } else {
                        return (d.children || d._children) ? 'blue' : 'black;';
                    }
                })
                .attr('text-anchor', function (d) {
                    return 'start';
                })
                .text(function (d) {
                    if (d.data.parent === 'null') {
                        return d.data.name + ':' + d.data.featureValue;
                    } else {
                        return d.data.name;
                    }
                });

            nodeEnter.selectAll('text.influencer')
                .call(wrap, 200);

            // Add labels for the nodes
            nodeEnter.append('text')
                .attr('dy', '45')
                .attr('font-family', 'sans-serif')
                .attr('font-weight', '600')
                .attr('x', function (d) {
                    return 0;
                })
                .attr('text-anchor', function (d) {
                    return 'start';
                })
                .text(function (d) { return d.data.totalValue; });

            // Add labels for the nodes
            nodeEnter.append('text')
                .attr('y', function (d) { return d.x ? -d.x : -d.x0; })
                .attr('font-family', 'sans-serif')
                .attr('font-weight', '600')
                .attr('class', 'modelName')
                .attr('x', function (d) {
                    return 0;
                })
                .attr('text-anchor', function (d) {
                    return 'start';
                })
                .text(function (d) { return d.data.modelName; })


            const nodeUpdate = nodeEnter.merge(node);

            // Transition nodes to their new position.
            nodeUpdate.transition()
                .duration(duration)
                .attr('transform', function (d) {
                    return 'translate(' + d.y + ',' + d.x + ')';
                });

            nodeUpdate.select('path.node')
                .attr('height', _this.nodeHeight)
                .attr('cursor', 'pointer');

            nodeUpdate.select('text.modelName')
                .attr('y', function (d) { return d.x ? -d.x : -d.x0; })
                .style('fill-opacity', 1)
                .call(wrapWordEllipse)
                .append('title').text(function (d) {
                    return d.data.modelName;
                });

            // Transition exiting nodes to the parent's new position.
            const nodeExit = node.exit().transition()
                .duration(duration)
                .attr('transform', function (d) {
                    return 'translate(' + source.y + ',' + source.x + ')';
                })
                .remove();

            nodeExit.select('path.node')
                .attr('width', 0)
                .attr('height', 0);

            nodeExit.select('text')
                .style('fill-opacity', 1e-6);

            nodeExit.select('text.modelName')
                .attr('y', function (d) { return d.x ? -d.x : -d.x0; })
                .style('fill-opacity', 1e-6);

            // Update the links…
            const link = _this.svg.selectAll('path.link')
                .data(links, function (d) {
                    return d.id;
                });

            // Enter any new links at the parent's previous position.
            const linkEnter = link.enter().insert('path', 'g')
                .attr('class', function (d) {
                    return 'link link-level-' + d.depth;
                })
                .attr('d', function (d) {
                    const o = { x: source.x0, y: source.y0 };
                    return diagonal(o, o);
                });

            const linkUpdate = linkEnter.merge(link);

            // Transition links to their new position.
            linkUpdate.transition()
                .duration(duration)
                .attr('d', function (d) { return diagonal(d, d.parent); });

            // Transition exiting nodes to the parent's new position.
            const linkExit = link.exit().transition()
                .duration(duration)
                .attr('d', function (d) {
                    const o = { x: source.x, y: source.y };
                    return diagonal(o, o);
                })
                .remove();

            // Stash the old positions for transition.
            nodes.forEach(function (d) {
                d.x0 = d.x;
                d.y0 = d.y;
            });

            _this.svg.selectAll('.link')
                .attr('fill', 'none')
                .attr('stroke', '#e4e4e4')
                .attr('stroke-width', '1px');

            _this.svg.selectAll('.link-level-1')
                .attr('display', 'none');
            _this.svg.selectAll('.level-0')
                .attr('display', 'none');
        }

        // Toggle children on click.
        function collapse(d) {
            if (d.data.parent === 'null') {
                d.parent.children.forEach(function (g) {
                    if (g.id !== d.id) {
                        if (g.children) {
                            g._children = g.children;
                            g.children = null;
                        }
                    }
                });
            }
            if (d.children) {
                d._children = d.children;
                d.data._children = d.children;
                d.children = null;
                d.data.children = null;
            } else {
                d.children = d._children;
                d.data.children = d._children;
                d._children = null;
                d.data._children = null;
            }
            update(d);
        }

        // Creates a curved (diagonal) path from parent to the child nodes
        function diagonal(s, t) {
            const path = `M ${s.y} ${s.x + (_this.nodeHeight / 2)}
            C ${((s.y + t.y) / 2) + 50} ${s.x + (_this.nodeHeight / 2)},
              ${((s.y + t.y) / 2) + 50 + 50} ${t.x + (_this.nodeHeight / 2)},
              ${t.y + _this.nodeWidth} ${t.x + (_this.nodeHeight / 2)}`;

            return path;
        }

        function leftRoundedRectData(radius) {

            const roundRect = `
              M${radius},0
              h${200 - (2 * radius)}
              a${radius},${radius} 0 0 1 ${radius},${radius}
              v0
              a${radius},${radius} 0 0 1 ${-radius},${radius}
              h${-(200 - (2 * radius))}
			        a${radius},${radius} 0 0 1 ${-radius},${-radius}
              v0
              a${radius},${radius} 0 0 1 ${radius},${-radius}
            `;

            return roundRect;
        }

        function lastElement(radius, xVal, value) {
            let roundRect = `
              M${xVal},0
              h${value - radius}
              a${radius},${radius} 0 0 1 ${radius},${radius}
              v0
              a${radius},${radius} 0 0 1 ${-radius},${radius}
              h${-(value - radius)}Z
            `;
            if (value - radius < 0) {
                roundRect = `
              M${xVal},0
              h0
              a${radius},${radius} 0 0 1 ${radius},${radius}
              v0
              a${radius},${radius} 0 0 1 ${-radius},${radius}
              h0Z
            `;
            }
            return roundRect;
        }

        function firstElement(radius, xVal, value) {
            if (value <= 1) {
                value = 2 * radius;
            }
            if (value >= 200) {
                value = 192;
            }
            const roundRect = `
              M${radius + xVal},0
              h${value - radius}
              v${_this.nodeHeight}
              h${-(value - radius)}
			        a${radius},${radius} 0 0 1 ${-radius},${-radius}
              v0
              a${radius},${radius} 0 0 1 ${radius},${-radius}Z
            `;

            return roundRect;
        }

        function middleElement(radius, xVal, value) {
            const roundRect = `
              M${xVal},0
              h${value}
              v${_this.nodeHeight}
              h${-value}
              v${-_this.nodeHeight}Z
            `;

            return roundRect;

        }

        function wrap(text, width) {
            text.each(function () {
                const text = d3.select(this),
                    words = text.text().split(':').reverse();
                let word,
                    line = [],
                    lineNumber = 0;
                const lineHeight = 2, // ems
                    x = 0,
                    y = text.attr('y'),
                    dy = 2;
                let tspan = text.text(null)
                    .append('tspan')
                    .attr('x', x)
                    .attr('y', y)
                    .attr('dy', dy + 'em');
                while (word = words.pop()) {
                    line.push(word);
                    tspan.text(line.join(':'));
                    if ((tspan.node().getComputedTextLength() - 20) > width) {
                        line.pop();
                        tspan.text(line.join(':'));
                        line = [word];
                        tspan = text.append('tspan')
                            .attr('x', x)
                            .attr('y', y)
                            .attr('dy', ++lineNumber * lineHeight + 'em')
                            .text(word);
                    }
                }
            });
        }

        function wrapWordEllipse(label, width) {
            label.each(function () {
                // const self = d3.select(this);
                // let textLength = label.length;
                // let text = self.text();
                let text = d3.select(this);
                const text2 = text._groups[0][0];
                if (!text2.innerHTML.includes('<title>'))
                    if (text2.textContent.length > 30) {
                        text2.textContent = text2.textContent.substring(0, 30) + '..';
                    }
            });
        }
    }

    showData() {
        this._modalService.show(ShowCascadeDataComponent, { class: 'modal-dialog-centered modal-xl', backdrop: 'static', initialState: { cascadedId : this.cascadedId, uniqueId : this.uniqueId}});
    }

    uploadData(uploadDataPopup) {
        this.uploadFilesCount = 0;
        this.files = [];
        const config = {
            ignoreBackdropClick: true,
            class: 'ingrAI-create-model ingrAI-enter-model deploymodle-confirmpopup'
        };
        this.modalRef = this._modalService.show(uploadDataPopup, config);
    }

    uploadView(value) {
        if (value === 'entity') {
            if (this.dataUploadValidationMessage) {
                this.ns.error(this.dataUploadValidationMessage);
            } else {
                this.view = value;
            }
        } else {
            this.view = value;
        }
    }

    getFileDetails(e) {
        if (this.agileFlag === true) {
            this.uploadFilesCount = e.target.files.length;
        } else {
            this.uploadFilesCount += e.target.files.length;
        }
        let validFileExtensionFlag = true;
        let validFileNameFlag = true;
        let validFileSize = true;
        const resourceCount = e.target.files.length;
        if (this.sourceTotalLength < 5 && resourceCount <= (5 - this.sourceTotalLength)) {
            const files = e.target.files;
            let fileSize = 0;
            for (let i = 0; i < e.target.files.length; i++) {
                const fileName = files[i].name;
                const dots = fileName.split('.');
                const fileType = '.' + dots[dots.length - 1];
                if (!fileName) {
                    validFileNameFlag = false;
                    break;
                }
                if (this.allowedExtensions.indexOf(fileType) !== -1) {
                    fileSize = fileSize + e.target.files[i].size;
                    const index = this.files.findIndex(x => (x.name === e.target.files[i].name));
                    validFileNameFlag = true;
                    validFileExtensionFlag = true;
                    if (index < 0) {
                        if (this.agileFlag === true) {
                            this.files = [];
                            this.files.push(e.target.files[0]);
                        } else {
                            this.files.push(e.target.files[i]);
                        }
                        this.uploadFilesCount = this.files.length;
                        this.sourceTotalLength = this.files.length + this.entityArray.length;
                    }
                } else {
                    validFileExtensionFlag = false;
                    break;
                }
            }
            if (fileSize <= 136356582) {
                validFileSize = true;
            } else {
                validFileSize = false;
                this.files = [];
                this.uploadFilesCount = this.files.length;
                this.sourceTotalLength = this.files.length + this.entityArray.length;
            }
            if (validFileNameFlag === false) {
                this.ns.error('Kindly upload a file with valid name.');
            }
            if (validFileExtensionFlag === false) {
                this.ns.error('Kindly upload .xlsx or .csv file.');
            }
            if (validFileSize === false) {
                this.ns.error('Kindly upload file of size less than 130MB.');
            }
        } else {
            this.uploadFilesCount = this.files.length;
            this.ns.error('Maximum 5 sources of data allowed.');
        }
    }

    uploadFile(view?) {
        this.appUtilsService.loadingStarted();
        if (view !== 'entity') {
            this.modalRef.hide();
        }
        this.isUploading = true;
        let isFileUpload = true;
        if (view === 'entity') {
            isFileUpload = false;
        }
        const params = {
            'UserId': this.appUtilsService.getCookies().UserId,
            'IsFileUpload': isFileUpload,
            'CascadedId': this.cascadedId,
            'ModelName': this.modelName,
            'ClientUID': this.clientId,
            'DCUID': this.dcid
        };
        this.cascadeVisService.uploadData(this.files, params).subscribe(res => {
            this.progress = res['percentDone'];
            if (res.body !== '') {
                if (res.body['Status'] === 'C') {
                    if (res.body['ValidatonMessage'] !== null) {
                        this.isUploading = false;
                        this.ns.error(res.body['ValidatonMessage']);
                    } else {
                        if (res.body['IsUploaded'] === true) {
                            this.progress = 100;
                            this.isUploading = false;
                            if (view === 'entity') {
                                this.ns.success('Data Processed Successfully.');
                            } else {
                                this.ns.success('File Uploaded Successfully.');
                            }
                            this.cascadedId = res.body['CascadedId'];
                            this.uniqueId = res.body['UniqueId'];
                            this.getCascadeVisualization();
                        } else {
                            this.appUtilsService.loadingEnded();
                            this.ns.error(res.body['ErrorMessage']);
                        }
                    }
                } else if ((res.body['Status'] === 'E')) {
                    this.appUtilsService.loadingEnded();
                    this.isUploading = false;
                    this.ns.error(res.body['ErrorMessage']);
                }
            }
        }, error => {
            this.appUtilsService.loadingEnded();
            this.isUploading = false;
            this.ns.error('Something went wrong.');
        });
    }

    cancel() {
        this.files = [];
        this.entityArray = [];
        this.sourceTotalLength = 0;
        this.uploadFilesCount = 0;
        this.modalRef.hide();
    }

    setPage(page: number) {
        if (page < 1 || page > this.pager.totalPages) {
            return;
        }
        const data = JSON.parse(JSON.stringify(this.dataForVisualization));
        const newData = {};
        newData['FeatureWeights'] = this.dataForVisualization['FeatureWeights'];
        this.pager = this.getPager(this.dataForVisualization['PredictionProbability'].length, page);
        this.pagedItems = data['PredictionProbability'].slice(this.pager.startIndex, this.pager.endIndex + 1);
        newData['PredictionProbability'] = this.pagedItems;
        this.formatJson(newData);
        this.drawChart();
    }

    getPager(totalItems: number, currentPage: number = 1, pageSize: number = 5) {
        // calculate total pages
        const totalPages = Math.ceil(totalItems / pageSize);

        let startPage: number, endPage: number;
        if (totalPages <= 5) {
            startPage = 1;
            endPage = totalPages;
        } else {
            if (currentPage <= 3) {
                startPage = 1;
                endPage = 5;
            } else if (currentPage + 1 >= totalPages) {
                startPage = totalPages - 4;
                endPage = totalPages;
            } else {
                startPage = currentPage - 2;
                endPage = currentPage + 2;
            }
        }

        // calculate start and end item indexes
        const startIndex = (currentPage - 1) * pageSize;
        const endIndex = Math.min(startIndex + pageSize - 1, totalItems - 1);

        function numberRange(start, end) {
            return new Array(end - start).fill(1).map((d, i) => i + start);
        }

        // create an array of pages to ng-repeat in the pager control
        const pages = numberRange(startPage, endPage + 1);

        // return object with all pager properties required by the view
        return {
            totalItems: totalItems,
            currentPage: currentPage,
            pageSize: pageSize,
            totalPages: totalPages,
            startPage: startPage,
            endPage: endPage,
            startIndex: startIndex,
            endIndex: endIndex,
            pages: pages
        };
    }


    allowDrop(event) {
        event.preventDefault();
    }

    onDrop(event) {
        event.preventDefault();

        for (let i = 0; i < event.dataTransfer.files.length; i++) {
            this.files.push(event.dataTransfer.files[i]);
        }
    }

}
