import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { LocalStorageService } from 'src/app/_services/local-storage.service';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { AppUtilsService } from 'src/app/_services/app-utils.service';

import {
  Router, ActivatedRoute, NavigationEnd

} from '@angular/router';
import { Subscription } from 'rxjs';


@Injectable({
  providedIn: 'root'
})
export class CustomRoutingService {
  public previousUrl: string = undefined;
  public nextUrl: string;
  private ArrayOfIndicesOfMatchedSegments = [];
  private routes: {} = {};
  private isLast: boolean;
  private isFirst: boolean;
  private pathToredirect: string;
  public urlAfterRedirects: string;
  private copyofarrayindicies: any[];
  private nextUrlSegmentGroup = new Set();
  private previousUrlSegmentGroup = new Set();
  urlSegmengGroup: Set<any>;
  subscription: Subscription;
  userId;
  deliveryConstructUID;
  clientUId;
  userCookie;

  constructor(private router: Router, private activatedRoute: ActivatedRoute, private api: ApiService,
    private ls: LocalStorageService, private notificationService: NotificationService, private aus: AppUtilsService) {
    this.routes = this.router.config;

    this.router.events.subscribe(event => {
      if (event instanceof NavigationEnd) {
        this.nextUrlSegmentGroup = new Set();
        this.urlSegmengGroup = new Set();
        this.previousUrlSegmentGroup = new Set();
        this.ArrayOfIndicesOfMatchedSegments = [];
        this.urlAfterRedirects = event.urlAfterRedirects;
        let primarySegments = this.urlAfterRedirects.substring(0, this.urlAfterRedirects.indexOf('?') !== -1
          ? this.urlAfterRedirects.indexOf('?') : this.urlAfterRedirects.length).split('/');
        primarySegments = primarySegments.filter(v => v);
        this.indexBuilder(this.routes, primarySegments);
        this.copyofarrayindicies = [...this.ArrayOfIndicesOfMatchedSegments];
        this.setNextPath();
        this.ArrayOfIndicesOfMatchedSegments = this.copyofarrayindicies;
        this.setPreviousPath();
      }
    });
    this.subscription = this.aus.getParamData().subscribe(paramData => {
      this.clientUId = paramData.clientUID;
      this.deliveryConstructUID = paramData.deliveryConstructUId;
    });
  }

  private indexBuilder(routes, primarySegments) {

    const matchedObject = this.matchSegment(routes, primarySegments[0]);
    primarySegments.shift();

    if (matchedObject !== null && primarySegments.length !== 0 && matchedObject.hasOwnProperty('children')) {
      this.indexBuilder(matchedObject.children, primarySegments);
    }
  }

  private matchSegment(routes, segment) {

    const length = routes.length - 1;
    this.isFirst = false;
    this.isLast = false;
    for (let i = 0; i <= length; i++) {
      if (routes[i].path === segment) {
        this.ArrayOfIndicesOfMatchedSegments.push(i);
        if (length === i) {
          this.isLast = true;
        }
        if (i === 1) {
          this.isFirst = true;
        }

        return routes[i];
      }
    }
    return null;
  }

  private setPreviousPath() {

    if (this.isFirst) {
      this.ArrayOfIndicesOfMatchedSegments[this.ArrayOfIndicesOfMatchedSegments.length - 1] = 0;
      this.ArrayOfIndicesOfMatchedSegments[this.ArrayOfIndicesOfMatchedSegments.length - 2]--;
    } else if (this.ArrayOfIndicesOfMatchedSegments.length !== 0) {
      this.ArrayOfIndicesOfMatchedSegments[this.ArrayOfIndicesOfMatchedSegments.length - 1]--;
    }
    this.pathFetcher(this.ArrayOfIndicesOfMatchedSegments[0], this.routes);

    this.previousUrlSegmentGroup = this.urlSegmengGroup;
    if (this.router.url.includes('usecasedefinition')) {
      this.previousUrlSegmentGroup.delete('cascadeModels');
    }
    this.previousUrl = Array.from(this.previousUrlSegmentGroup).join('/');

    this.urlSegmengGroup = new Set();
  }

  private setNextPath() {

    if (this.isLast) {
      this.ArrayOfIndicesOfMatchedSegments[this.ArrayOfIndicesOfMatchedSegments.length - 2]++;
      this.ArrayOfIndicesOfMatchedSegments[this.ArrayOfIndicesOfMatchedSegments.length - 1] = 0;
    } else if (this.ArrayOfIndicesOfMatchedSegments.length !== 0) {
      this.ArrayOfIndicesOfMatchedSegments[this.ArrayOfIndicesOfMatchedSegments.length - 1]++;
    }

    this.pathFetcher(this.ArrayOfIndicesOfMatchedSegments[0], this.routes);

    this.nextUrlSegmentGroup = this.urlSegmengGroup;
    this.nextUrl = Array.from(this.nextUrlSegmentGroup).join('/');
    this.urlSegmengGroup = new Set();

  }

  private pathFetcher(index, routes): any {

    if (routes) {
      if (routes.path === '' && routes.hasOwnProperty('redirectTo')) {
        this.pathToredirect = routes['redirectTo'];
        this.urlSegmengGroup.add(this.pathToredirect);
      } else if (routes.path === undefined || routes.path === '') {

      } else {
        this.pathToredirect = routes.path;
        this.urlSegmengGroup.add(this.pathToredirect);
      }
    }

    if (this.ArrayOfIndicesOfMatchedSegments.length !== 0) {
      this.ArrayOfIndicesOfMatchedSegments.splice(0, 1);
      if (routes) {
        if (routes.hasOwnProperty('children')) {

          this.pathFetcher(this.ArrayOfIndicesOfMatchedSegments[0], routes.children[index]);
        } else {
          this.pathFetcher(this.ArrayOfIndicesOfMatchedSegments[0], routes[index]);
        }
      }
    }
  }

  public getNextUrl() {
    return this.nextUrl;
  }

  public getPreviousUrl() {
    return this.previousUrl;
  }

  public redirectToNext(queryParams?) {
    if (this.nextUrl === undefined) {
      this.nextUrlSegmentGroup = new Set();
      this.urlSegmengGroup = new Set();
      this.previousUrlSegmentGroup = new Set();
      this.ArrayOfIndicesOfMatchedSegments = [];
      this.urlAfterRedirects = this.router.url;
      let primarySegments = this.urlAfterRedirects.substring(0, this.urlAfterRedirects.indexOf('?') !== -1
        ? this.urlAfterRedirects.indexOf('?') : this.urlAfterRedirects.length).split('/');
      primarySegments = primarySegments.filter(v => v);
      this.indexBuilder(this.routes, primarySegments);
      this.copyofarrayindicies = [...this.ArrayOfIndicesOfMatchedSegments];
      this.setNextPath();
      this.ArrayOfIndicesOfMatchedSegments = this.copyofarrayindicies;
      this.setPreviousPath();
    }
    if (this.nextUrl.includes('dashboard/preprocessdata') || this.nextUrl.includes('dashboard/FeatureSelection') || this.nextUrl.includes('dashboard/TeachAndTest')) {
      this.aus.loadingStarted();
      this.ValidateInput(this.ls.correlationId).subscribe(data => {
        if (data['Status'] === 'C') {
          this.router.navigateByUrl(this.nextUrl);
          this.aus.loadingImmediateEnded();
        } else {
          this.notificationService.error(data['Message']);
          this.aus.loadingImmediateEnded();
        }
      });
    } else if (this.nextUrl.includes('anomaly-detection/FeatureSelection')) {
      this.aus.loadingStarted();
      this.router.navigateByUrl(this.nextUrl);
      this.aus.loadingImmediateEnded();

    } else {
      this.aus.loadingStarted();
      this.router.navigateByUrl(this.nextUrl);
      this.aus.loadingImmediateEnded();
    }
  }

  ValidateInput(correlationId: string, isSave?) {
    let pageInfo;
    this.userCookie = this.aus.getCookies();
    this.userId = this.userCookie.UserId;
    if (this.nextUrl !== undefined) {
      if (this.nextUrl.includes('preprocessdata')) {
        pageInfo = 'DataCleanup';
      } else if (this.nextUrl.includes('FeatureSelection')) {
        pageInfo = 'DataTransform';
      } else if (this.nextUrl.includes('TeachAndTest')) {
        pageInfo = 'RecommendedAI';
      }
      if (pageInfo !== undefined) {
        return this.api.get('ValidateInput', {
          'correlationId': correlationId,
          'pageInfo': pageInfo/* ,
          'userId': this.userId,
          'deliveryConstructUID': this.deliveryConstructUID,
          'clientUId': this.clientUId,
          'isTemplateModel': false */
        });
      }
    }
  }

  public redirectToPrevious() {
    if (this.previousUrl === undefined) {
      this.nextUrlSegmentGroup = new Set();
      this.urlSegmengGroup = new Set();
      this.previousUrlSegmentGroup = new Set();
      this.ArrayOfIndicesOfMatchedSegments = [];
      this.urlAfterRedirects = this.router.url;
      let primarySegments = this.urlAfterRedirects.substring(0, this.urlAfterRedirects.indexOf('?') !== -1
        ? this.urlAfterRedirects.indexOf('?') : this.urlAfterRedirects.length).split('/');
      primarySegments = primarySegments.filter(v => v);
      this.indexBuilder(this.routes, primarySegments);
      this.copyofarrayindicies = [...this.ArrayOfIndicesOfMatchedSegments];
      this.setNextPath();
      this.ArrayOfIndicesOfMatchedSegments = this.copyofarrayindicies;
      this.setPreviousPath();
    }
    this.router.navigateByUrl(this.previousUrl);
  }
}
