import { Component, ElementRef, HostBinding, HostListener, OnInit, ViewChild, AfterViewInit } from '@angular/core';
import * as d3 from 'd3';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import { FmVisualizationServiceService } from '../../services/fm-visualization-service.service';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { ActivatedRoute, Router } from '@angular/router';
import { ExcelService } from 'src/app/_services/excel.service';
import { throwError, of, timer, EMPTY } from 'rxjs';
import { DatasetDeletionPopupComponent } from 'src/app/components/dashboard/problem-statement/dataset-deletion-popup/dataset-deletion-popup.component';
import { NotificationData } from 'src/app/_services/usernotification';
import * as $ from 'jquery';
import { UserNotificationpopupComponent } from 'src/app/components/user-notificationpopup/user-notificationpopup.component';
import { DialogService } from 'src/app/dialog/dialog.service';
declare var userNotification: any;

@Component({
  selector: 'app-fm-visualization-graph',
  templateUrl: './fm-visualization-graph.component.html',
  styleUrls: ['./fm-visualization-graph.component.scss']
})
export class FmVisualizationGraphComponent implements OnInit, AfterViewInit {
  isnavBarToggle: boolean;

  constructor(private _modalService: BsModalService, private el: ElementRef,
    private dialogService: DialogService,
    private fmService: FmVisualizationServiceService, private ns: NotificationService,
    private appUtilsService: AppUtilsService, private route: ActivatedRoute, private router: Router,
    private _excelService: ExcelService) {
    this.route.queryParams.subscribe(params => {
      this.category = params['Category']; // 'AD'; // 
      this.modelName = params['ModelName'];
      this.dcid = params['DeliveryConstructUId']; //'c79694f4-265d-4b01-a391-9ff8c6e77562'; //
      this.clientId = params['ClientUId']; // '23a1fc6f-1676-4083-89fc-6d73a5cbebae'; 
      this.userid = params['UserId'] + '@accenture.com';
    });
  }


  modalRef: BsModalRef | null;
  innerUploadmodalRef: BsModalRef | null;
  category = 'AD';
  dcid;
  clientId;
  modelName;
  userid;
  dataforDownload = [];
  sourceTotalLength = 0;
  allowedExtensions = ['.xlsx', '.csv'];
  files = [];
  entityArray = [];
  uploadFilesCount = 0;
  // @ViewChild('fmLineChart') element: ElementRef;
  element: ElementRef;
  @ViewChild('fmLineChart') set content(content: ElementRef) {
    if (content) { // initially setter gets called with undefined
      this.element = content;
      if (content.nativeElement.innerHTML === '') {
        this.initializeChart();
      }
    }
  }
  htmlElement: HTMLElement;
  host;
  lineChartWidth;
  lineChartHeight;
  hexBinChartData = [];
  influencersList = [];
  SNChangeNumber;
  changeOutcome;
  negativeReleaseArr = [];
  // isUploading = true;
  isUploadView = false;
  isModelTraining = false;
  isGraphView = false;
  timerSubscripton: any;
  dataForVisualization = [];
  correlationId;
  fmCorrelationId;
  step = 1;
  isRefresh = false;
  uniqueId;
  observations = [];
  samplePredictionDone = true;
  selectedReleaseFromGraph;

  ngOnInit() {
    console.log('FM Visualization-- ng oninit');
    this.appUtilsService.loadingStarted();

    this.fmService.getFMVisualizationDetails(this.clientId, this.dcid, this.userid, this.category).subscribe(colData => {
      this.appUtilsService.loadingEnded(); console.log('FM Visualization-- getFMVisualizationDetails', colData);
      this.correlationId = colData['CorrelationId'];
      this.fmCorrelationId = colData['FMCorrelationId'];
      if (colData['IsFMDataAvaialble'] === true) {
        this.samplePredictionDone = false;
        if (colData['FMVisualizeData']) {
          this.dataForVisualization = colData['FMVisualizeData'];
          this.isUploadView = false;
          this.isModelTraining = false;
          this.isGraphView = true;
          // this.initializeChart();
        }
      } else {
        this.isUploadView = true;
      }
    }, error => {
      this.appUtilsService.loadingEnded();
      this.isUploadView = true;
      this.ns.error('Something went wrong.');
    });
  }

  ngAfterViewInit() {
    if (this.isGraphView === true) {
      this.initializeChart();
    }
  }

  downloadData() {
    const self = this;
    this.dialogService.open(UserNotificationpopupComponent, {}).afterClosed.subscribe(confirmationflag => {
      const useCaseId = 'f2e5af7b-e504-4d21-9bb7-f50a1c8f90d6'; // UsecaseId for FM Scenario
      self.fmService.getTemplateData(useCaseId).subscribe(data => {
        self.dataforDownload = data['ColumnListDetails'] || [];
        if (self.isRefresh) {
          delete self.dataforDownload[0]['ChangeOutcome'];
        }
        if (self.dataforDownload) {
          self.ns.success('Your Data will be downloaded shortly');
          if ((sessionStorage.getItem('Environment') === 'PAM' || sessionStorage.getItem('Environment') === 'FDS')){
            self._excelService.exportAsExcelFile(self.dataforDownload, 'TemplateDownloaded');
          }else{
            self._excelService.exportAsPasswordProtectedExcelFile(self.dataforDownload, 'TemplateDownloaded').subscribe(response => {
              self.ns.warning('Downloaded file is password protected, click on the Info icon to view the password hint');
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
        
        }
      }, error => {
        self.ns.error('Something went wrong.');
      });
    });
  }

  getFileDetails(e) {
    this.files = [];
    this.uploadFilesCount = e.target.files.length;
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
            this.files.push(e.target.files[i]);
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

  uploadFile() {
    this.appUtilsService.loadingStarted();
    this.hexBinChartData = [];
    this.influencersList = [];
    this.negativeReleaseArr = [];
    this.observations = [];
    const params = {
      'UserId': this.appUtilsService.getCookies().UserId,
      'Category': this.category,
      'ClientUID': this.clientId,
      'DCUID': this.dcid,
      'IsRefresh': this.isRefresh,
      'CorrelationId': this.correlationId
    };
    this.fmService.fmFileUpload(this.files, params).subscribe(res => {
      if (res.body !== '') {
        if (res.body['Status'] === 'C') {
          this.appUtilsService.loadingEnded();
          if (res.body['ValidatonMessage'] !== null) {
            this.ns.error(res.body['ValidatonMessage']);
          } else {
            if (res.body['IsUploaded'] === true) {
              this.files = [];
              this.uploadFilesCount = 0;
              this.ns.success('File Uploaded Successfully.');
              this.fmCorrelationId = res.body['FMCorrelationId'];
              this.correlationId = res.body['CorrelationId'];
              if (res.body['IsRefresh'] === true) {
                this.uniqueId = res.body['UniqueId'];
                this.appUtilsService.loadingStarted();
                this.getFMVisualizationPrediction();
              } else {
                this.step = 1;
                this.isUploadView = false;
                this.isGraphView = false;
                this.isModelTraining = true;
                this.fmModelTrainingStatus();
              }
            } else {
              this.ns.error(res.body['ErrorMessage']);
              this.files = [];
            }
          }
        } else if ((res.body['Status'] === 'E')) {
          this.files = [];
          this.appUtilsService.loadingEnded();
          this.ns.error(res.body['ErrorMessage']);
        }
      }
    }, error => {
      this.files = [];
      this.appUtilsService.loadingEnded();
      this.ns.error('Something went wrong.');
    });
  }

  fmModelTrainingStatus() {
    this.fmService.fmModelTrainingStatus(this.correlationId, this.fmCorrelationId, this.userid).subscribe(data => {
      if (data['Status'] === 'C') {
        if (data['IsModel1Completed'] === true && data['IsModel2Completed'] === true) {
          this.step = 4;
          this.dataForVisualization = data.FMVisualizationData;
          this.isUploadView = false;
          this.isModelTraining = false;
          this.isGraphView = true;
          this.samplePredictionDone = true;
          // this.initializeChart();
        } else {
          if (data['IsModel1Completed'] === true) {
            this.step = 2;
            if (data['ProcessName'] === 'DeployModel') {
              this.step = 3;
            } else if (data['ProcessName'] === 'ME' || data['ProcessName'] === 'DE') {
              this.step = 3;
            }
          }
          this.retryFMVisualization();
        }
      } else if (data['Status'] === 'E') {
        this.trainingIncompleteError();
      } else {
        if (data['IsModel1Completed'] === true) {
          this.step = 2;
          if (data['ProcessName'] === 'DeployModel') {
            this.step = 3;
          } else if (data['ProcessName'] === 'ME' || data['ProcessName'] === 'DE') {
            this.step = 3;
          }
        }
        this.retryFMVisualization();
      }
    }, (error) => {
      this.trainingIncompleteError();
    });
  }

  retryFMVisualization() {
    this.timerSubscripton = timer(5000).subscribe(() => this.fmModelTrainingStatus());
    return this.timerSubscripton;
  }

  getFMVisualizationPrediction() {
    this.samplePredictionDone = true;
    this.fmService.getFMVisualizationPrediction(this.correlationId, this.uniqueId).subscribe(data => {
      if (data['Status'] === 'C') {
        this.innerUploadmodalRef.hide();
        // this.modalRef.hide();
        this.uploadFilesCount = 0;
        if (data['FMVisualizationData']) {
          this.samplePredictionDone = false;
          this.isUploadView = false;
          this.isModelTraining = false;
          this.isGraphView = true;
          this.dataForVisualization = data.FMVisualizationData;
          this.appUtilsService.loadingEnded();
          this.initializeChart();
        }
      } else if (data['Status'] === 'E') {
        this.appUtilsService.loadingEnded();
        this.uploadFilesCount = 0;
        this.ns.error('Prediction failed due to some backend error. Please try again.');
      } else {
        this.retryFMVisualizationPrediction();
      }
    }, error => {
      this.appUtilsService.loadingEnded();
      this.uploadFilesCount = 0;
      this.ns.error('Prediction failed due to some backend error. Please try again.');;
    });
  }

  retryFMVisualizationPrediction() {
    this.timerSubscripton = timer(5000).subscribe(() => this.getFMVisualizationPrediction());
    return this.timerSubscripton;
  }

  initializeChart() {
    this.htmlElement = this.element.nativeElement;
    this.host = d3.select(this.htmlElement);
    this.htmlElement.innerHTML = '';
    this.host.innerhtml = '';
    this.lineChartWidth = 900;
    this.lineChartHeight = 200;
    this.drawLineChart();
  }

  drawLineChart() {
    const _this = this;
    const margin = { top: 50, right: 50, bottom: 50, left: 50 },
      height = 300 - margin.top - margin.bottom;
    let width = 1100 - margin.left - margin.right;
    const dataset = this.dataForVisualization;
    if (dataset.length > 10) {
      width = dataset.length * 70 + width;
    }
    // dataset = dataset.sort(function(a, b) { new Date(a.date) - new Date(b.date) });
    const xScale = d3.scaleTime()
      .rangeRound([0, width]);
    const xFormat = "%d %b'%Y";
    const parseTime = d3.timeParse("%d/%m/%Y %H:%M:%S");

    xScale.domain(d3.extent(dataset, function (d) {
      const removeUTC = d.date.substring(0, d.date.length - 4);
      return parseTime(removeUTC);
    }));

    let maxy = d3.max(dataset, function (d) {
      return d3.max([(d.successProbability)]);
    })
    maxy = (maxy * 100).toFixed(0);
    const yScale = d3.scaleLinear()
      // .domain([0, 1]) // input
      .range([height, 0]); // output
    yScale.domain([0, maxy]).nice();


    const line = d3.line()
      .curve(d3.curveMonotoneX) // apply smoothing to the line
      .x(function (d, i) {
        const removeUTC = d.date.substring(0, d.date.length - 4);
        return xScale(parseTime(removeUTC));
      }) // set the x values for the line generator
      .y(function (d) { return yScale((d.successProbability * 100).toFixed(0)); }); // set the y values for the line generator



    const svg = this.host.append('svg')
      .attr('width', width + margin.left + margin.right)
      .attr('height', height + margin.top + margin.bottom)
      .append('g')
      .attr('transform', 'translate(' + margin.left + ',' + margin.top + ')');

    // Add x-axis
    svg.append('g')
      .attr('class', 'x axis')
      .attr('transform', 'translate(0,' + height + ')')
      .call(d3.axisBottom(xScale).tickFormat(d3.timeFormat(xFormat))); // Create an axis component with d3.axisBottom

    // Label for x-axis
    svg.append('text')
      .attr('transform',
        'translate(' + (width / 2) + ' ,' +
        (height + margin.top) + ')')
      .style('text-anchor', 'middle')
      .style('color', 'grey')
      .text('Release Date');

    // gridlines in y axis function
    function make_y_gridlines() {
      return d3.axisLeft(yScale)
        .ticks(5);
    }

    // Add y-axis
    svg.append('g')
      .attr('class', 'y axis')
      .call(make_y_gridlines()
        .tickSize(-width)); // Create an axis component with d3.axisLeft

    // Label for y-axis
    svg.append('text')
      .attr('transform', 'rotate(-90)')
      .attr('y', 0 - margin.left)
      .attr('x', 0 - (height / 2))
      .attr('dy', '1em')
      .style('text-anchor', 'middle')
      .style('color', 'grey')
      .text('Success Probability (%) ');

    svg.append('path')
      .datum(dataset) // Binds data to the line
      .attr('class', 'line') // Assign a class for styling
      .attr('fill', 'none')
      .attr('stroke', '#c07cc0')
      .attr('stroke-width', '2')
      .attr('d', line); // Calls the line generator

    svg.selectAll('.dot')
      .data(dataset)
      .enter().append('circle') // Uses the enter().append() method
      .attr('class', 'dot') // Assign a class for styling
      .attr('fill', 'purple')
      .attr('stroke', '#fff')
      .attr('cx', function (d, i) {
        const removeUTC = d.date.substring(0, d.date.length - 4);
        return xScale(parseTime(removeUTC));
      })
      .attr('cy', function (d) { console.log((d.successProbability * 100).toFixed(0)); return yScale((d.successProbability * 100).toFixed(0)); })
      .attr('r', 4)
      .on('click', function (d) {
        svg.selectAll('.dot')
          .attr('fill', 'purple')
          .attr('r', 4)
          .attr('cursor', 'pointer');
        d3.select(this).transition()
          .duration('50')
          .attr('r', 6)
          .attr('fill', 'green')
          .attr('cursor', 'default');
        showChangeReq(d);
      });
    /* .on('mouseover', function() {
      d3.select(this).transition()
         .duration('50')
         .attr('fill', '3px')
         .attr('cursor', 'pointer');
    })
    .on('mouseout', function (d, i) {
      d3.select(this).transition()
           .duration('50')
           .attr('stroke-width', '2px');
    }); */

    svg.selectAll('.dot-text')
      .data(dataset)
      .enter().append('text') // Uses the enter().append() method
      .attr('x', function (d, i) {
        const removeUTC = d.date.substring(0, d.date.length - 4);
        return xScale(parseTime(removeUTC));
      })
      .attr('dy', function (d) { return yScale((d.successProbability * 100).toFixed(0)) - 10; })
      .text(function (d) { return (d.successProbability * 100).toFixed(0); })
      .attr('font-size', '12px')
      .attr('font-weight', '500')
      .attr('fill', 'navy');

    // date: "31/05/2020 05:00:03 UTC"
    // observations: (3) ["Emergency_Data Validation Service", "PredOut_Unix Support", "Standard_Gateway to CSP"]
    // releaseName: "Release100"
    // successProbability: 0.34


    svg.selectAll('circle')
      .append('title')
      .text(function (d) {
        if (d.hasOwnProperty('releaseName')) {
          return "Release Name : " + d.releaseName + " Release End Date : " + d.date;
        } else {
          return '';
        }
      });

    svg.selectAll('.y')
      .data(dataset)
      .enter()
      .append('line')
      .attr('class', 'line-class')
      .attr('x1', function (d) {
        const removeUTC = d.date.substring(0, d.date.length - 4);
        return xScale(parseTime(removeUTC));
      })
      .attr('x2', function (d) {
        const removeUTC = d.date.substring(0, d.date.length - 4);
        return xScale(parseTime(removeUTC));
      })
      .attr('y1', function (d) {
        return yScale((d.successProbability * 100).toFixed(0));
      })
      .attr('y2', height)
      .style('stroke-width', 1)
      .style('stroke-dasharray', ('3, 2'))
      .style('stroke', '#000');

    function showChangeReq(values) {
      _this.populateHexbinData(values);
    }

    svg.selectAll('.x path')
      .attr('stroke', '#e4e4e4');
    svg.selectAll('.y path')
      .attr('stroke', '#e4e4e4');
    svg.selectAll('.tick line')
      .attr('stroke', '#e4e4e4');
    svg.selectAll('.x text')
      .attr('color', 'grey');
    svg.selectAll('.y text')
      .attr('color', 'grey');
  }

  populateHexbinData(data) {
    this.hexBinChartData = [];
    this.influencersList = [];
    this.negativeReleaseArr = [];
    this.observations = [];
    this.observations = data.observations;
    this.selectedReleaseFromGraph = data.releaseName;
    const changeReqArr = data.changeReqArr;
    this.negativeReleaseArr = changeReqArr.filter(el => el.ChangeOutcome === 'Missed');
    if (changeReqArr.length <= 10) {
      this.hexBinChartData.push(changeReqArr);
    } else {
      const size = 10;
      for (let i = 0; i < changeReqArr.length; i += size) {
        this.hexBinChartData.push(changeReqArr.slice(i, i + size));
      }
    }
  }

  onHexClick(data) {
    this.influencersList = [];
    this.SNChangeNumber = data.ChangeNumber;
    this.changeOutcome = data.ChangeOutcome;
    const influencerList = data.InfluencersM1.sort(function (a, b) { return a['featureWeight'] < b['featureWeight'] ? 1 : -1; });
    influencerList.forEach((element, j) => {
      const color = 'rgb(0,' + (70 + (j * 15)) + ',255)';
      element['color'] = color;
    });
    const size = Math.ceil(influencerList.length / 3);
    for (let i = 0; i < influencerList.length; i += size) {
      /* const h = 240;
      const s = Math.floor((influencerList.length - i));
      const l = Math.floor((influencerList.length - i));
      const color = 'hsl(' + h + ', ' + s + '%, ' + l + '%)'; */
      this.influencersList.push(influencerList.slice(i, i + size));
    }
  }

  showModelView(fmModalPopup) {
    const config = {
      backdrop: true,
      ignoreBackdropClick: true,
      class: 'cascade-popup'
    };
    this.modalRef = this._modalService.show(fmModalPopup, config);
  }

  retrainModelPopup(modelRetrainPopup) {
    const config = {
      ignoreBackdropClick: true,
      class: 'ingrAI-create-model ingrAI-enter-model'
    };
    this.modalRef = this._modalService.show(modelRetrainPopup, config);
  }

  onConfirm() {
    this.modalRef.hide();
    this.isRefresh = false;
    this.isModelTraining = false;
    this.isGraphView = false;
    this.isUploadView = true;
    this.retrainModel();
  }

  retrainModel() {
    this.appUtilsService.loadingStarted();
    this.fmService.fmModelsDelete(this.correlationId, this.fmCorrelationId).subscribe(data => {
      this.appUtilsService.loadingEnded();
      if (data === true) {
        // this.ns.success('Success');
        this.isRefresh = false;
        this.isModelTraining = false;
        this.isGraphView = false;
        this.isUploadView = true;
      } else {
        this.ns.error('Something went wrong');
      }
    }, error => {
      this.appUtilsService.loadingEnded();
      this.ns.error('Something went wrong');
    });
  }

  refreshModel(InnerUploadPop, process) {
    this.isRefresh = true;
    this.isModelTraining = false;
    this.isGraphView = true;
    const config = {
      ignoreBackdropClick: true,
      class: 'ingrAI-create-model ingrAI-enter-model deploymodle-confirmpopup'
    };
    this.innerUploadmodalRef = this._modalService.show(InnerUploadPop, config);
    // this.isUploadView = true;
  }

  toggleNavBar() {
    this.isnavBarToggle = !this.isnavBarToggle;
  }

  allowDrop(event) {
    event.preventDefault();
  }

  onDrop(event) {
    event.preventDefault();
    for (let i = 0; i < event.dataTransfer.files.length; i++) {
      this.files.push(event.dataTransfer.files[i]);
      this.uploadFilesCount = 1;
    }
  }

  trainingIncompleteError() {
    this.isUploadView = true;
    this.isGraphView = false;
    this.isModelTraining = false;
    this.ns.error('Training failed due to some backend error. Please try again.');
  }
}
