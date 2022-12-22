import { Component, OnInit, ElementRef, Inject } from '@angular/core';
import { CustomRoutingService } from 'src/app/_services/custom-routing.service';
import { FeatureSelectionService } from 'src/app/_services/feature-selection.service';
import { CookieService } from 'ngx-cookie-service';
import { Router } from '@angular/router';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';
@Component({
  selector: 'app-model-engineering',
  templateUrl: './model-engineering.component.html',
  styleUrls: ['./model-engineering.component.scss']
})
export class ModelEngineeringComponent implements OnInit {

  isNextDisabled: boolean;
  modelType: string;
  currentIndex = 2;
  breadcrumbIndex = 0;

  constructor(@Inject(ElementRef) private eleRef: ElementRef, private customRouter: CustomRoutingService,
    private fs: FeatureSelectionService, private cookieService: CookieService,
    private router: Router, private cus: CoreUtilsService) { }

  ngOnInit() {
    /* const allLinks = this.eleRef.nativeElement.parentElement.children[0].querySelectorAll('a');
    if (allLinks.length > 0) {
      allLinks[3].className = 'active';
    } */
    if (this.router.url.includes('FeatureSelection')) {
      this.breadcrumbIndex = 0;
    } else if (this.router.url.includes('RecommendedAI')) {
      this.breadcrumbIndex = 1;
    } else if (this.router.url.includes('TeachAndTest')) {
      this.breadcrumbIndex = 2;
    } else if (this.router.url.includes('CompareModels')) {
      this.breadcrumbIndex = 3;
    }

    this.fs.applyFlag$.subscribe(
      bool => {
        this.isNextDisabled = bool;
      }
    );

  }

  next() {
    this.modelType = this.cookieService.get('ModelTypeForInstaML');
    if (this.modelType === 'TimeSeries'
      && this.router.url.indexOf('/dashboard/modelengineering/TeachAndTest/WhatIfAnalysis') > -1) {
     this.customRouter.nextUrl = 'dashboard/modelengineering/CompareModels';
     this.cus.disableTabs(this.currentIndex, 3);
    }
    if(this.router.url.includes('WhatIfAnalysis') === false) {
      this.cus.disableTabs(this.currentIndex, this.breadcrumbIndex, null, true);
    }
    this.customRouter.redirectToNext();
  }

  previous() {
    this.modelType = this.cookieService.get('ModelTypeForInstaML');
    if (this.modelType === 'TimeSeries'
      && this.router.url.indexOf('dashboard/modelengineering/CompareModels') > -1) {
        this.customRouter.previousUrl = '/dashboard/modelengineering/TeachAndTest/WhatIfAnalysis';
    }
    if (this.customRouter.urlAfterRedirects === '/dashboard/modelengineering/FeatureSelection') {
      this.customRouter.previousUrl = 'dashboard/dataengineering/preprocessdata';
    }
    if(this.router.url.includes('HyperTuning') === false) {
      this.cus.disableTabs(this.currentIndex, this.breadcrumbIndex, true, null);
    }
    this.customRouter.redirectToPrevious();
  }
}
