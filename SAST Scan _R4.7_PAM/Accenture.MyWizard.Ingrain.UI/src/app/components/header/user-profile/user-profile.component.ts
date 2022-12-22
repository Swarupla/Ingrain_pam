import { Component, OnInit, Input, ElementRef, Output, EventEmitter } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { EnvironmentService } from 'src/app/_services/EnvironmentService';

@Component({
  selector: 'app-user-profile',
  templateUrl: './user-profile.component.html',
  styleUrls: ['./user-profile.component.scss'],
  host: {
    '(document:click)': 'onClick($event)'
  }
})
export class UserProfileComponent implements OnInit {
  @Input() userId: string;
  @Input() accountID: string;
  @Input() fullName;
  @Output() logout = new EventEmitter();
  show: boolean;
  logedinUserDetails: any;
  logedinUserRole: string;
  userName: string;
  env;
  requestType;
  fromApp;
  authprovider;
  isShowAccountID = false;
  isFDSEU: boolean = false;

  constructor(private _eref: ElementRef, private appUtilsService: AppUtilsService, private envService: EnvironmentService, private activatedRoute: ActivatedRoute) { }

  ngOnInit() {
    if (this.activatedRoute.url.toString().includes('mywizardingraineu')) {
      this.isFDSEU = true;
    }
    this.authprovider = this.envService.environment.authProvider.toLowerCase();
    this.env = sessionStorage.getItem('Environment');
    this.requestType = sessionStorage.getItem('RequestType');
    if (sessionStorage.getItem('fromSource') !== null) {
      this.fromApp = sessionStorage.getItem('fromSource').toUpperCase();
    }
    let userEmail = [];
    if (((this.env === 'PAM' || this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) || this.fromApp === 'FDS') {
      this.logedinUserRole = 'Client Admin';
    } else {
      this.logedinUserDetails = this.appUtilsService.getRoleData().subscribe(userDetails => {
        this.logedinUserRole = userDetails.accessRoleName;
      });
    }

    userEmail = this.userId.split('@');
    this.userName = userEmail[0];
  }

  onClick(event) {
    if (event.target.id === 'imgOff' || event.target.id === 'imgOn') {
      this.show = true;
    } else if (!this._eref.nativeElement.contains(event.target)) {
      this.show = false;
    } else {
      if (document.getElementsByClassName('nav-active').length) {
        this.show = false;
      } else {
        this.show = true;
      }

    }
  }

  signOut() {
    this.logout.emit();
  }

}
