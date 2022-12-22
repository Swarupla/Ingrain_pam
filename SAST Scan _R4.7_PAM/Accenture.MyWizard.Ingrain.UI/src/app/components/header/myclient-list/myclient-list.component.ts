import { Component, Input, ElementRef, Inject, OnChanges, Output, EventEmitter, OnInit, SimpleChanges } from '@angular/core';
import { ClientDeliveryStructureService } from '../../../_services/client-delivery-structure.service';
import { DomSanitizer } from '@angular/platform-browser';
import { HttpClient } from '@angular/common/http';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';
import { Router } from '@angular/router';
import { NotificationService } from '../../../_services/notification-service.service';
import { from, forkJoin } from 'rxjs';
import { browserRefreshforApp } from '../../../components/root/app.component';
import { EnvironmentService } from 'src/app/_services/EnvironmentService';
// import { NotificationData } from 'src/app/_services/usernotification';
// declare var userNotification: any;

@Component({
  selector: 'app-myclient-list',
  templateUrl: './myclient-list.component.html',
  styleUrls: ['./myclient-list.component.scss'],
  host: {
    '(document:click)': 'onClick($event)'
  }
})
export class MyclientListComponent implements OnInit, OnChanges {
  @Input() userId: string;
  @Input() token: string;
  @Input() deliveryConstructData: string;
  @Input() ingrainAPIToken;
  @Output() autorizedError = new EventEmitter();
  @Output() getClientDetails = new EventEmitter();
  @Output() passaccessRoleUID = new EventEmitter();
  @Output() setDecimalPoint = new EventEmitter();
  ClientUID: string;
  clientName: string;
  chldName: string;
  cltDelStructData: any;
  display = 'none';
  btnApply: boolean;
  clientUID: any;
  deliveryConstructUId: string;
  parentUID: any;
  paramData: any;
  selectedDCUId: string;
  selectedClientUId: string;
  isLoading = true;
  showSearchTxt = false;
  clientDeliveryStructList: any;
  userCookie: any;
  role: any;
  selectedDCStructLoaded: boolean;
  fromApp: string;
  fromSource: string;
  displayDrodpdown: boolean;
  show: boolean;
  accessRoleUID = '';
  env;
  requestType;
  End2EndId;
  End2EndName;
  fromSourceSession;
  TYPING_INTERVAL = 500; //Time to wait when user stops typing before searching.
  MAX_SEARCH_CACHE = 1;
  typingTimer;
  searchQueryMap = new Map();
  isSearch = false;
  selectedSearchResult: any;
  searchText = '';

  constructor(@Inject(ElementRef) private elementRef: ElementRef, private router: Router,
    private domSanitizer: DomSanitizer, private _httpClient: HttpClient,
    private coreUtilsService: CoreUtilsService, private clientDelStructService: ClientDeliveryStructureService,
    private appUtilService: AppUtilsService,
    private notifyService: NotificationService, private _eref: ElementRef, private environmentService: EnvironmentService) {
    this.env = sessionStorage.getItem('Environment') ? sessionStorage.getItem('Environment') : sessionStorage.getItem('fromSource');
    this.requestType = sessionStorage.getItem('RequestType');
    this.fromSourceSession = sessionStorage.getItem('fromSource');
    if (this.fromSourceSession === 'PAM') {
      this.fromSourceSession = this.fromSourceSession.toLowerCase();
    }
  }

  ngOnInit(): void {
    this.environmentService.msalToken$.subscribe(data => {
      if (data) {
        console.log('myclientlist msal')
      }
    });
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes.ingrainAPIToken) {
      if ((this.env === 'PAM' || this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) {
        this.getVDSData();
      } else {
        console.log('myclientlist this.deliveryConstructData', this.deliveryConstructData);
        if (!this.coreUtilsService.isNil(this.deliveryConstructData)) {
          this.chldName = 'Not Selected';
          this.selectedDCStructLoaded = false;
          if (!this.coreUtilsService.isNil(localStorage.getItem('fromApp'))) {
            this.fromApp = localStorage.getItem('fromApp').toLocaleLowerCase();
          }
          if (!this.coreUtilsService.isNil(localStorage.getItem('fromSource'))) {
            this.fromSource = localStorage.getItem('fromSource').toLocaleLowerCase();
          }
          if (this.fromApp === 'vds' && (this.fromSourceSession === 'FDS' || this.fromSourceSession === 'pam')) {
            this.getVDSData();
          } else {
            this.getUserStoryclientStructures();
          }
        }
      }
    }
  }

  getUserStoryclientStructures() {
    const _this = this;
    this.paramData = this.deliveryConstructData;
    this.parentUID = this.paramData.parentUID;
    this.clientUID = this.paramData.clientUID;
    this.selectedClientUId = this.paramData.clientUID;
    this.selectedDCUId = this.paramData.deliveryConstructUId;
    this.deliveryConstructUId = this.paramData.deliveryConstructUId;

    const showLabel = this.elementRef.nativeElement.querySelector('#showLablel');
    const showDropDown = this.elementRef.nativeElement.querySelector('#showDropDown');
    if (((this.router.url.toString().indexOf('/dashboard/problemstatement/templates') > -1
      || this.router.url.toString().indexOf('/landingPage') > -1
      || this.router.url.toString().indexOf('choosefocusarea') > -1)
      && this.coreUtilsService.isNil(this.fromApp)) && (this.env !== 'PAM' || this.env !== 'FDS') && this.requestType !== 'AM' && this.requestType !== 'IO') {
      if (!this.coreUtilsService.isNil(showLabel)) {
        showLabel.className = 'display';
      }
      if (!this.coreUtilsService.isNil(showDropDown)) {
        showDropDown.className = 'enableheader';

      }
      // this.displayDrodpdown = true;
    } else {
      if (!this.coreUtilsService.isNil(showLabel)) {
        showLabel.className = 'enableheader';
      }
      if (!this.coreUtilsService.isNil(showDropDown)) {
        showDropDown.className = 'display';
      }
      // this.displayDrodpdown = false;
    }
    if (!this.coreUtilsService.isNil(this.fromApp) && this.fromApp === 'vds'
      && this.fromSource === 'pam') {
      // call the api  from PAM
      this.clientName = 'Accenture - DCN Internal';
      _this.appUtilService.loadingStarted();
      this.clientDelStructService.getPamDeliveryConstructName(this.deliveryConstructUId).subscribe(
        data => {
          if (!this.coreUtilsService.isNil(data)) {
            const chldName = JSON.parse(data);
            sessionStorage.setItem('dcName', chldName);
            if (!this.coreUtilsService.isNil(chldName)) {
              this.chldName = chldName;
            }
          }
          _this.appUtilService.loadingEnded();
        }, error => {
          this.isLoading = false;
          _this.appUtilService.loadingEnded();
          this.notifyService.error('PAM API is not responding. Please try after some time.');
        });
      if (!this.coreUtilsService.isNil(localStorage.getItem('BrowsRefresh'))) {
        localStorage.removeItem('BrowsRefresh');
        this.router.navigate(['/dashboard/dataengineering/datacleanup'],
          {
            queryParams: {}
          });
      }
    } else {
      this.clientUID = this.coreUtilsService.isNil(this.clientUID) ? '00506000-0000-0000-1111-000000001234' : this.clientUID;
      if (!this.coreUtilsService.isNil(this.clientUID)) {
        this.getAllClientsData();
      }
      if (!this.coreUtilsService.isNil(this.deliveryConstructUId)) {
        this.getDCStructNameAndAccessPrivileges();
      }
      if (!this.coreUtilsService.isNil(localStorage.getItem('BrowsRefresh')) && this.fromApp === 'vds'
        && this.fromSource === 'pad') {
        localStorage.removeItem('BrowsRefresh');
        this.router.navigate(['/dashboard/dataengineering/datacleanup'],
          {
            queryParams: {}
          });
      }
    }
  }

  get enableDropdown() {
    if ((this.env === 'PAM' || this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) {
      return false;
    } else {
      if (((this.router.url.toString().indexOf('dashboard/problemstatement/templates') > -1
        || this.router.url.toString().indexOf('/landingPage') > -1
        || this.router.url.toString().indexOf('choosefocusarea') > -1)
        && this.coreUtilsService.isNil(this.fromApp)) && (this.env !== 'PAM' || this.env !== 'FDS') && this.requestType !== 'AM' && this.requestType !== 'IO') {
        return true;
      } else {
        return false;
      }
    }
  }

  getAllClientsData(calledFrom?: string) {
    const _this = this;
    _this.isLoading = true;
    if (calledFrom === 'searchClient') {
      _this.appUtilService.loadingStarted();
    }
    this.clientDelStructService.getUserStoryclientStructures(this.clientUID, this.userId).subscribe(
      data => {

        // const jsonData = JSON.parse(data);
        // _this.cltDelStructData = jsonData.Clients;
        // _this.clientDeliveryStructList = jsonData.Clients;
        _this.accessRoleUID = data[0].AccessRoleUId;
        _this.cltDelStructData = data; // jsonData.Clients;
        _this.clientDeliveryStructList = data; // jsonData.Clients;
        if (!_this.coreUtilsService.isNil(_this.cltDelStructData) && _this.cltDelStructData.length > 0) {
          this.setClientUID();
          this.showScope();
        }
        this.isLoading = false;
        this.passaccessRoleUID.emit(this.accessRoleUID);
        _this.appUtilService.loadingEnded();

      }, error => {
        this.isLoading = false;
        _this.appUtilService.loadingEnded();
        this.notifyService.error('Phoenix API is not responding. Please try after some time.');
      });
  }

  getDCStructNameAndAccessPrivileges() {
    const _this = this;
    this.appUtilService.loadingStarted();
    // getDeliveryConstructName - to get the DCName //
    this.clientDelStructService.getDeliveryConstructName(this.clientUID,
      this.deliveryConstructUId, this.userId).subscribe(
        result => {
          this.appUtilService.loadingEnded();
          if (!this.coreUtilsService.isNil(result)) {
            // const chldName = JSON.parse(result);
            if (!result.Message) {
              if (result[0]['Name'] !== undefined) {
                const chldName = result[0]['Name'];
                sessionStorage.setItem('dcName', chldName);
                if (!this.coreUtilsService.isNil(chldName)) {
                  this.chldName = chldName;
                }
              }
            }
          }
        }, error => {
          this.appUtilService.loadingEnded();
          if (error.status === 401) {
            this.notifyService.error('token has expired so redirecting to login page');
            this.autorizedError.emit('401');
          } else if (error.status === 500) {
            this.notifyService.error(error.message);
          } else {
            this.notifyService.error('Phoenix API is not responding. Please try after some time.');
          }
        });

    // getPriviligesForAccountorUser - to get the role of the user //
    this.appUtilService.loadingStarted();
    this.clientDelStructService.getPriviligesForAccountorUser(this.selectedClientUId,
      this.selectedDCUId, this.userId, this.token).subscribe(
        result => {
          this.appUtilService.loadingEnded();
          if (this.coreUtilsService.isNil(result)) {
            this.role = '';
          } else {
            const entity = JSON.parse(result);
            if (this.coreUtilsService.isNil(entity)) {
              this.role = '';
            } else {
              this.role = entity[0];
            }
          }
          this.appUtilService.setRoleData(this.role);
        }, error => {
          this.appUtilService.loadingEnded();
          if (error.status === 401) {
            this.notifyService.error('token has expired so redirecting to login page');
            this.autorizedError.emit('401');
          } else if (error.status === 500) {
            this.notifyService.error(error.message);
          } else {
            this.notifyService.error('Phoenix API is not responding. Please try after some time.');
          }
        });

    // getClientName - to get the Client Name //
    this.appUtilService.loadingStarted();
    this.clientDelStructService.getClientName(this.clientUID, this.deliveryConstructUId, this.userId).subscribe(
      result => {
        this.appUtilService.loadingEnded();
        if (!this.coreUtilsService.isNil(result)) {
          // const clientName = JSON.parse(result);
          if (!result.Message) {
            if (result[0]['Name'] !== undefined) {
              const clientName = result[0]['Name'];
              if (!this.coreUtilsService.isNil(clientName)) {
                this.clientName = clientName;
              }
            }
          }
        }
      }, error => {
        this.appUtilService.loadingEnded();
        if (error.status === 401) {
          this.notifyService.error('token has expired so redirecting to login page');
          this.autorizedError.emit('401');
        } else if (error.status === 500) {
          this.notifyService.error(error.message);
        } else {
          this.notifyService.error('Phoenix API is not responding. Please try after some time.');
        }
      });
  }

  showScope() {
    if (!this.coreUtilsService.isNil(this.cltDelStructData)) {
      this.selectedDCStructLoaded = false;
      this.cltDelStructData.forEach(val => {
        if (!this.coreUtilsService.isNil(val) && !this.coreUtilsService.isNil(val.ClientUId) && val.ClientUId === this.clientUID) {
          if (!this.coreUtilsService.isNil(val.DeliveryStructureList) && val.DeliveryStructureList.length > 0) {
            this.selectedDCStructLoaded = true;
          }
        }
      });
      if (!this.selectedDCStructLoaded) {
        this.appUtilService.loadingStarted();
        this.clientDelStructService.GetDeliveryStructsForClient(this.clientUID, this.deliveryConstructUId, this.userId, null).subscribe(
          value => {
            this.appUtilService.loadingEnded();
            if (!this.coreUtilsService.isNil(value)) {
              this.cltDelStructData.forEach(val => {
                if (!this.coreUtilsService.isNil(val) && !this.coreUtilsService.isNil(val.ClientUId) && val.ClientUId === this.clientUID) {
                  val.SelectedIndex = 'True';
                  val.DeliveryStructureList = [];
                  if (value !== '') {
                    val.DeliveryStructureList = JSON.parse(value);
                    this.setClientUID();
                  }
                }
              });
              this.onCloseHandled();
            } else {
              this.onCloseHandled();
            }
          },
          error => {
            this.appUtilService.loadingEnded();
            this.onCloseHandled();
          });
      }
    }
  }

  // getURL(value, imgBin) {
  getURL(imgBin) {
    const value = 'data:image/png;base64,';
    if (imgBin !== null) {
      if (imgBin !== undefined) {
        return this.domSanitizer.bypassSecurityTrustUrl(value + imgBin);
      }
    } else if (imgBin === null) {
      return this.domSanitizer.bypassSecurityTrustUrl('././././assets/images/client1.jpg');
    }
  }

  toggleclass(event: any, value: any, client: any) {
    //  if (client.AcessRole != null && client.AcessRole !== undefined && client.AcessRole.length > 0) {
    this.btnApply = true;
    // } else {
    //   this.btnApply = false;
    // }
    const ssmenus = this.elementRef.nativeElement.querySelectorAll('.ss-menu-wrap li');
    const items = Array.from(ssmenus);
    Array.prototype.forEach.call(items, function (eachelement) {
      eachelement.classList.remove('selected');
    });

    const highlightedmenus = this.elementRef.nativeElement.querySelectorAll('.ss-menu-wrap li');
    const highlighted = Array.from(highlightedmenus);
    Array.prototype.forEach.call(highlighted, function (eachelement) {
      eachelement.classList.remove('highlighted');
    });

    this.setToggleClass(event);
    event.stopPropagation();

  }
  openModal() {
    this.display = 'block';
  }
  onCloseHandled() {
    this.display = 'none';
  }
  flterClick(event: any, val: any, value: any) {
    this.openModal();
    if (value === 'name') {
      this.btnApply = true;
      const ssmenus = this.elementRef.nativeElement.querySelectorAll('.ss-menu-wrap li');
      const items = Array.from(ssmenus);
      Array.prototype.forEach.call(items, function (eachelement) {
        eachelement.classList.remove('selected');
      });

      this.setNameClass(event);

      event.stopPropagation();
    } else if (value === 'parent') {
      this.btnApply = true;
      const ssmenus = this.elementRef.nativeElement.querySelectorAll('.ss-menu-wrap li');
      const items = Array.from(ssmenus);
      Array.prototype.forEach.call(items, function (eachelement) {
        eachelement.classList.remove('selected');
      });
      this.setParentClass(event);
    } else if (value === 'span') {
      this.btnApply = true;
      const ssmenus = this.elementRef.nativeElement.querySelectorAll('.ss-menu-wrap li');
      const items = Array.from(ssmenus);
      Array.prototype.forEach.call(items, function (eachelement) {
        eachelement.classList.remove('selected');
      });

      this.setSpanClass(event);
      event.stopPropagation();

    }
    if (this.coreUtilsService.isNil(val.DeliveryStructureList) || val.DeliveryStructureList.length === 0) {
      let deliveryConstructUId = null;
      if (!this.coreUtilsService.isNil(this.deliveryConstructUId)) {
        deliveryConstructUId = this.deliveryConstructUId;
      }
      this.appUtilService.loadingStarted();
      this.clientDelStructService.GetDeliveryStructsForClient(val.ClientUId, deliveryConstructUId, this.userId, null).subscribe(
        mapValues => {
          this.appUtilService.loadingEnded();
          val.DeliveryStructureList = [];
          if (!this.coreUtilsService.isNil(mapValues)) {
            if (mapValues !== '') {
              val.DeliveryStructureList = JSON.parse(mapValues);
            }
            this.onCloseHandled();
          } else {
            this.onCloseHandled();
          }
        },
        error => {
          this.appUtilService.loadingEnded();
          this.onCloseHandled();
        });
    } else {
      this.appUtilService.loadingEnded();
      this.onCloseHandled();
    }
  }

  setNameClass(event: any) {
    if (event.target.parentElement.parentElement.className.indexOf('ng-star-inserted') > -1) {
      event.target.parentElement.parentElement.className = event.target.parentElement.parentElement.className.replace('ng-star-inserted', '').trim();
    }

    switch (event.target.parentElement.parentElement.className) {
      case '':
        event.target.parentElement.parentElement.className = 'ss-exp-col open selected';
        break;
      case 'ss-exp-col':
        event.target.parentElement.parentElement.className = 'ss-exp-col open selected';
        break;
      case 'open selected':
        event.target.parentElement.parentElement.className = '';
        break;
      case 'ss-exp-col open selected':
        event.target.parentElement.parentElement.className = '';
        break;
      case 'open':
        event.target.parentElement.parentElement.className = '';
        break;
      case 'ss-exp-col open':
        event.target.parentElement.parentElement.className = '';
        break;
    }
    if (event.target.parentElement.className === 'selected') {
      event.target.parentElement.className = '';
    } else if (event.target.parentElement.className === 'open') {
      event.target.parentElement.className = '';
    }
  }
  setParentClass(event: any) {
    if (event.target.parentElement.className.indexOf('ng-star-inserted') > -1) {
      const tt = event.target.parentElement.className.split(' ng-star-inserted');
      if (tt.length === 1 && tt[0] === 'ng-star-inserted') {
        event.target.parentElement.className = '';
      } else {
        event.target.parentElement.className = tt[0];
      }
    }
    switch (event.target.parentElement.className) {
      case '':
        event.target.parentElement.className = 'ss-exp-col open selected';
        break;
      case 'ss-exp-col':
        event.target.parentElement.className = 'ss-exp-col open selected';
        break;
      case 'open selected':
        event.target.parentElement.className = '';
        break;
      case 'ss-exp-col open selected':
        event.target.parentElement.className = '';
        break;
      case 'open':
        event.target.parentElement.className = '';
        break;
      case 'ss-exp-col open':
        event.target.parentElement.className = '';
        break;
      case 'selected':
        event.target.parentElement.className = '';
        break;
      case 'open':
        event.target.parentElement.className = '';
        break;
    }
  }

  setSpanClass(event: any) {
    if (event.target.parentElement.parentElement.className.indexOf('ng-star-inserted') > -1) {
      const tt = event.target.parentElement.parentElement.className.split(' ng-star-inserted');
      if (tt.length === 1 && tt[0] === 'ng-star-inserted') {
        event.target.parentElement.parentElement.className = '';
      } else {
        event.target.parentElement.parentElement.className = tt[0];
      }
    }
    switch (event.target.parentElement.parentElement.className) {
      case '':
        event.target.parentElement.parentElement.className = 'open selected';
        break;
      case 'open selected':
        event.target.parentElement.parentElement.className = '';
        break;
      case 'open':
        event.target.parentElement.parentElement.className = '';
        break;
      case 'ss-client':
        event.target.parentElement.parentElement.className = 'ss-client open selected';
        break;
      case 'ss-client open selected':
        event.target.parentElement.parentElement.className = 'ss-client';
        break;
      case 'ss-client open':
        event.target.parentElement.parentElement.className = 'ss-client';
        break;
      case 'ss-client open':
        event.target.parentElement.parentElement.className = 'ss-client';
        break;
      case 'selected':
        event.target.parentElement.parentElement.className = '';
        break;
    }
    if (event.target.parentElement.className === 'ss-client') {
      event.target.parentElement.className = 'ss-client open selected';
    } else if (event.target.parentElement.className === 'ss-client open selected') {
      event.target.parentElement.className = 'ss-client';
    } else if (event.target.parentElement.className === 'ss-client open') {
      event.target.parentElement.className = 'ss-client';
    }
  }

  setToggleClass(event: any) {
    if (event.target.parentElement.parentElement.className.indexOf('ng-star-inserted') > -1) {
      const tt = event.target.parentElement.parentElement.className.split(' ng-star-inserted');
      if (tt.length === 1 && tt[0] === 'ng-star-inserted') {
        event.target.parentElement.parentElement.className = '';
      } else {
        event.target.parentElement.parentElement.className = tt[0];
      }
    }
    switch (event.target.parentElement.parentElement.className) {
      case '':
        event.target.parentElement.parentElement.className = 'open selected';
        break;
      case 'open selected':
        event.target.parentElement.parentElement.className = '';
        break;
      case 'ss-exp-col':
        event.target.parentElement.parentElement.className = 'ss-exp-col open selected';
        break;
      case 'ss-exp-col open selected':
        event.target.parentElement.parentElement.className = 'ss-exp-col';
        break;
      case 'selected':
        event.target.parentElement.parentElement.className = '';
        break;
      case 'ss-exp-col open':
        event.target.parentElement.parentElement.className = 'ss-exp-col selected';
        break;
      case 'open':
        event.target.parentElement.parentElement.className = 'open selected';
        break;
      case 'ss-exp-col highlighted':
        event.target.parentElement.parentElement.className = 'ss-exp-col open selected';
        break;
    }
  }

  apply() {
    const selectedElement = this.elementRef.nativeElement.querySelector('.selected');
    if (this.coreUtilsService.isNil(selectedElement)) {
    } else {
      const selectedElementId = selectedElement.id;
      const splitArr = selectedElementId.split('&clientuid&');
      if (splitArr.length === 2 && splitArr[0] != 'undefined' && splitArr[1] != 'undefined') {
        this.selectedDCUId = splitArr[0];
        this.selectedClientUId = splitArr[1];
        this.clientUID = this.selectedClientUId;
        this.deliveryConstructUId = this.selectedDCUId;
        sessionStorage.setItem('clientID', this.selectedClientUId);
        sessionStorage.setItem('dcID', this.selectedDCUId);
        sessionStorage.setItem('ClientUId', this.selectedClientUId);
        sessionStorage.setItem('DeliveryConstructUId', this.selectedDCUId);
        this.getAllClientsData();
        this.setClientUID();
        this.appUtilService.setParamData(this.selectedDCUId, this.selectedClientUId, this.parentUID, false, true);
        //  this.router.routeReuseStrategy.shouldReuseRoute = () => false;

        const obj = {
          clientUID: this.selectedClientUId,
          deliveryConstructUId: this.selectedDCUId
        };
        this.getClientDetails.emit(obj);
        // this.getClientImage.emit(this.selectedClientUId);
        this.setDecimalPoint.emit();
        if (this.router.url.toString().indexOf('reusable-NLP-services') > -1) {

        } else {
          /* this.router.navigate(['/dashboard/problemstatement/templates'],
         {
           queryParams: {
             'clientUId': this.selectedClientUId,
             'deliveryConstructUID': this.selectedDCUId
           }
         }); */
          this.router.navigate(['/dashboard/problemstatement/templates'], {});
          this.searchText = '';
          this.isSearch = false;
          this.getAllClientsData('searchClient');
        }
      }
    }
  }

  applyDefault() {
    const selectedElement = this.elementRef.nativeElement.querySelector('.selected');
    if (this.coreUtilsService.isNil(selectedElement)) {
    } else {
      const selectedElementId = selectedElement.id;
      const splitArr = selectedElementId.split('&clientuid&');
      if (splitArr.length === 2 && splitArr[0] != 'undefined' && splitArr[1] != 'undefined') {
        this.selectedDCUId = splitArr[0];
        this.selectedClientUId = splitArr[1];
        this.clientUID = this.selectedClientUId;
        this.deliveryConstructUId = this.selectedDCUId;
        sessionStorage.setItem('clientID', this.selectedClientUId);
        sessionStorage.setItem('dcID', this.selectedDCUId);
        this.getAllClientsData();
        this.setClientUID();
        if (this.selectedClientUId && this.selectedDCUId) {
          // this.getClientImage.emit(this.selectedClientUId);
          const obj = {
            clientUID: this.selectedClientUId,
            deliveryConstructUId: this.selectedDCUId
          };
          this.getClientDetails.emit(obj);
          sessionStorage.setItem('clientID', this.selectedClientUId);
          sessionStorage.setItem('dcID', this.selectedDCUId);
          this.appUtilService.setParamData(this.selectedDCUId, this.selectedClientUId, this.parentUID, false, true);
          localStorage.removeItem('ClientUId');
          localStorage.removeItem('DeliveryConstructUId');
          sessionStorage.setItem('ClientUId', this.selectedClientUId);
          sessionStorage.setItem('DeliveryConstructUId', this.selectedDCUId);

          const postData = {
            'UserId': this.userId,
            'ClientUId': this.selectedClientUId,
            'DeliveryConstructUID': this.selectedDCUId
          };
          // forkJoin([
          this.appUtilService.loadingStarted();
          this.setDecimalPoint.emit();
          this.clientDelStructService.postDeliveryConstruct(postData).subscribe(
            (results) => {
              if (results === 'Success') {
                this.appUtilService.loadingEnded();
                this.cltDelStructData.forEach(val => {
                  if (!this.coreUtilsService.isNil(val) && !this.coreUtilsService.isNil(val.ClientUId)) {
                    val.SelectedIndex = 'False';
                  }
                });
                this.cltDelStructData.forEach(val => {
                  if (!this.coreUtilsService.isNil(val) && !this.coreUtilsService.isNil(val.ClientUId)
                    && val.ClientUId === this.selectedClientUId) {
                    val.SelectedIndex = 'True';
                  }
                });
                this.notifyService.success('Delivery Structure saved Succesfully');
                this.router.navigate(['/dashboard/problemstatement/templates'],
                  {
                    queryParams: {
                      'clientUId': this.selectedClientUId,
                      'deliveryConstructUID': this.selectedDCUId
                    }
                  });
              }
            },
            error => {
              this.appUtilService.loadingEnded();
              if (error.status === 401) {
                this.notifyService.error('token expired to get roles in AccessPrivileges.');
              } else {
                this.notifyService.error('Something went wrong in apply set as default');
              }
            }
          );

          this.appUtilService.loadingStarted();
          this.clientDelStructService.getPriviligesForAccountorUser
            (this.selectedClientUId, this.selectedDCUId, this.userId, this.token)
            .subscribe(
              results => {
                this.appUtilService.loadingEnded();
                if (this.coreUtilsService.isNil(results)) {
                  this.role = '';
                } else {
                  const entity = JSON.parse(results);
                  if (this.coreUtilsService.isNil(entity)) {
                    this.role = '';
                  } else {
                    this.role = entity[0];
                  }
                }
                this.appUtilService.setRoleData(this.role);
              },
              error => {
                this.appUtilService.loadingEnded();
                if (error.status === 401) {
                  this.notifyService.error('token expired to get roles in AccessPrivileges.');
                } else {
                  this.notifyService.error('Something went wrong in apply set as default');
                }
              }
            );
        }
        this.searchText = '';
        this.isSearch = false;
        this.getAllClientsData('searchClient');
      }
    }
  }


  setClientUID() {
    this.cltDelStructData.forEach(ar => {
      const clientUID = !this.coreUtilsService.isNil(ar) ? ar.ClientUId : '';
      if (!this.coreUtilsService.isNil(ar) && ar.ClientUId === this.selectedClientUId) {
        this.clientName = ar.Name;

        if (!this.coreUtilsService.isNil(ar) && !this.coreUtilsService.isNil(ar.DeliveryStructureList)) {
          ar.DeliveryStructureList.forEach(ar1 => {
            if (!this.coreUtilsService.isNil(ar1) && ar1.DeliveryConstructUID === this.selectedDCUId) {
              this.chldName = ar1.Name;
              this.clientUID = clientUID;
              ar1.SelectedIndex = 'True';
            } else if (!this.coreUtilsService.isNil(ar1) && !this.coreUtilsService.isNil(ar1.Children)) {
              ar1.Children.forEach(ar2 => {
                if (!this.coreUtilsService.isNil(ar2) && ar2.DeliveryConstructUID === this.selectedDCUId) {
                  this.chldName = ar2.Name;
                  this.clientUID = clientUID;
                  ar2.SelectedIndex = 'True';
                } else if (!this.coreUtilsService.isNil(ar2) && !this.coreUtilsService.isNil(ar2.Children)) {
                  ar2.Children.forEach(ar3 => {
                    if (!this.coreUtilsService.isNil(ar3) && ar3.DeliveryConstructUID === this.selectedDCUId) {
                      this.chldName = ar3.Name;
                      this.clientUID = clientUID;
                      ar3.SelectedIndex = 'True';
                    } else if (!this.coreUtilsService.isNil(ar3) && !this.coreUtilsService.isNil(ar3.Children)) {
                      ar3.Children.forEach(ar4 => {
                        if (!this.coreUtilsService.isNil(ar4) && ar4.DeliveryConstructUID === this.selectedDCUId) {
                          this.chldName = ar4.Name;
                          this.clientUID = clientUID;
                          ar4.SelectedIndex = 'True';
                        } else if (!this.coreUtilsService.isNil(ar4) && !this.coreUtilsService.isNil(ar4.Children)) {
                          ar4.Children.forEach(ar5 => {
                            if (!this.coreUtilsService.isNil(ar5) && ar5.DeliveryConstructUID === this.selectedDCUId) {
                              this.chldName = ar5.Name;
                              this.clientUID = clientUID;
                              ar5.SelectedIndex = 'True';
                            } else if (!this.coreUtilsService.isNil(ar5) && !this.coreUtilsService.isNil(ar5.Children)) {
                              ar5.Children.forEach(ar6 => {
                                if (!this.coreUtilsService.isNil(ar6) && ar6.DeliveryConstructUID === this.selectedDCUId) {
                                  this.chldName = ar6.Name;
                                  this.clientUID = clientUID;
                                  ar6.SelectedIndex = 'True';
                                } else if (!this.coreUtilsService.isNil(ar6) && !this.coreUtilsService.isNil(ar6.Children)) {
                                  ar6.Children.forEach(ar7 => {
                                    if (!this.coreUtilsService.isNil(ar7) && ar7.DeliveryConstructUID === this.selectedDCUId) {
                                      this.chldName = ar7.Name;
                                      this.clientUID = clientUID;
                                      ar7.SelectedIndex = 'True';
                                    } else if (!this.coreUtilsService.isNil(ar7) && !this.coreUtilsService.isNil(ar7.Children)) {
                                      ar7.Children.forEach(ar8 => {
                                        if (!this.coreUtilsService.isNil(ar8) && ar8.DeliveryConstructUID === this.selectedDCUId) {
                                          this.chldName = ar8.Name;
                                          this.clientUID = clientUID;
                                          ar8.SelectedIndex = 'True';
                                        } else if (!this.coreUtilsService.isNil(ar8) && !this.coreUtilsService.isNil(ar8.Children)) {
                                          ar8.Children.forEach(ar9 => {
                                            if (!this.coreUtilsService.isNil(ar9) && ar9.DeliveryConstructUID === this.selectedDCUId) {
                                              this.chldName = ar9.Name;
                                              this.clientUID = clientUID;
                                              ar9.SelectedIndex = 'True';
                                            } else {

                                            }
                                          });
                                        }
                                      });

                                    }
                                  });
                                }
                              });
                            }
                          });

                        }

                      });

                    }
                  });

                }

              });
            }
          });
        }

      }
    });
  }

  onSearchChange(event: any) {
    if (!this.coreUtilsService.isNil(event) && event.length >= 3 && !this.coreUtilsService.isNil(this.clientDeliveryStructList)) {
      this.cltDelStructData = [];
      if (!this.coreUtilsService.isNil(this.clientDeliveryStructList)) {
        this.clientDeliveryStructList.forEach(ar => {
          if (!this.coreUtilsService.isNil(ar) && !this.coreUtilsService.isNil(ar.Name)
            && ar.Name.toLowerCase().indexOf(event.toLowerCase()) > -1) {
            this.showSearchTxt = false;
            this.cltDelStructData.push(ar);
          }
        });
      }

      if (this.cltDelStructData.length === 0) {
        this.showSearchTxt = true;
      }
    } else {
      this.showSearchTxt = false;
      this.cltDelStructData = [];
      this.cltDelStructData = this.clientDeliveryStructList;
    }
  }

  onClick(event) {
    if (!this._eref.nativeElement.contains(event.target)) {
      this.show = false;
      if (this.searchText.length) {
        this.isSearch = false;
        this.searchText = '';
        this.getAllClientsData();
      }
    } else {
      if (document.getElementsByClassName('nav-active').length && !this.isSearch) {
        // this.show = false;
        this.show = (event.target.id === 'c-icons__search') ? true : false;
      } else {
        this.show = (event.target.id === 'apply' || event.target.id === 'applyDefault') ?
          false : true;
      }

    }
  }

  highlightSelection(value, i) {
    this.btnApply = true;
    this.selectedSearchResult = value;
    this.cltDelStructData.forEach((element, index) => {
      if (index === i) {
        element['SelectedIndex'] = 'True';
      } else {
        element['SelectedIndex'] = '';
      }
    });
  }

  getVDSData() {
    this.appUtilService.loadingStarted();
    this.clientUID = sessionStorage.getItem('ClientUId');
    this.deliveryConstructUId = sessionStorage.getItem('DeliveryConstructUId');
    if (this.clientUID === null) {
      this.clientUID = sessionStorage.getItem('clientID');
    }
    if (this.deliveryConstructUId === null) {
      this.deliveryConstructUId = sessionStorage.getItem('dcID');
    }
    this.End2EndId = sessionStorage.getItem('End2EndId');
    const params = {
      'ClientUID': this.clientUID,
      'E2EUID': this.End2EndId,
      'DeliveryConstructUID': this.deliveryConstructUId,
      'UserId': this.userId
    };
    this.clientDelStructService.getDemographicsName(params).subscribe(
      data => {
        this.appUtilService.loadingEnded();
        if (this.env === 'PAM') {
          this.clientName = 'Accenture - DCN Internal';
          this.chldName = data.name;
        } else {
          this.clientName = data.ClientName;
          this.chldName = data.DeliveryConstructName;
        }
        this.End2EndName = data.E2EName;
      },
      error => {
        this.appUtilService.loadingEnded();
        if (error.error) {
          this.notifyService.error(error.error);
        } else {
          this.notifyService.error('Error in VDS api.');
        }
      });
  }

  searchClientAndDC(searchText: string) {

    if (searchText.length >= 3) {
      let keyName = this.searchQueryMap.keys().next().value;
      if (keyName) {
        if (searchText.indexOf(keyName) !== -1) {
          let searchedResult = this.searchQueryMap.get(keyName);
          this.cltDelStructData = searchedResult.filter(x => x.Name.toLowerCase().indexOf(searchText.toLowerCase()) !== -1);
        } else {
          this.clientDelStructService.getSearchDeliveryClientName(this.clientUID,
            this.deliveryConstructUId, this.userId, searchText)
            .subscribe((data: any) => {
              this.isSearch = true;
              this.cltDelStructData = data;
              // this.tree = null;
              this.searchQueryMap.set(searchText, data);
              if (this.searchQueryMap.size > this.MAX_SEARCH_CACHE) {
                this.searchQueryMap.delete(keyName)
              }
            });
        }
      } else {
        this.clientDelStructService.getSearchDeliveryClientName(this.clientUID,
          this.deliveryConstructUId, this.userId, searchText)
          .subscribe((data: any) => {
            this.isSearch = true;
            this.cltDelStructData = data;
            // this.tree = null;
            this.searchQueryMap.set(searchText, data);
            if (this.searchQueryMap.size > this.MAX_SEARCH_CACHE) {
              this.searchQueryMap.delete(keyName)
            }
          });
      }
    }
  }

  walkNodes(searchText: string) {
    if (searchText.trim().length) {
      clearTimeout(this.typingTimer);
      this.typingTimer = setTimeout(() => this.searchClientAndDC(searchText.trim()), this.TYPING_INTERVAL);
    } else {
      this.isSearch = false;
      this.getAllClientsData();
    }
  }

}


