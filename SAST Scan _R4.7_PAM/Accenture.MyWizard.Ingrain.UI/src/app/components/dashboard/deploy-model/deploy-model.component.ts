import { Component, OnInit, ViewChild, ElementRef, Inject } from '@angular/core';
import { CustomRoutingService } from 'src/app/_services/custom-routing.service';
import { Router } from '@angular/router';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';

@Component({
  selector: 'app-deploy-model',
  templateUrl: './deploy-model.component.html',
  styleUrls: ['./deploy-model.component.scss']
})
export class DeployModelComponent implements OnInit {
  showNextButton = false;
  currentIndex = 3;
  breadcrumbIndex = 0;


  constructor(@Inject(ElementRef) private eleRef: ElementRef,
    private customRouter: CustomRoutingService,
    private router: Router, private cus: CoreUtilsService) { }

  ngOnInit() {    
    if (this.router.url.includes('deployedmodel')) {
      this.breadcrumbIndex = 1;
    } else if (this.router.url.includes('publishmodel')) {
      this.breadcrumbIndex = 0;
    }
  }

  next() {
    this.cus.disableTabs(this.currentIndex, this.breadcrumbIndex, null, true);
    this.customRouter.redirectToNext();
  }

  previous() {
    if (this.customRouter.urlAfterRedirects.startsWith('/dashboard/deploymodel/deployedmodel')) {
      this.cus.disableTabs(this.currentIndex, 1, true, null);
      this.router.navigate(['/dashboard/deploymodel/publishmodel'],
        {
          queryParams: {
            'publisModelFlag': true
          }
        });
    }

    if (this.customRouter.urlAfterRedirects.startsWith('/dashboard/deploymodel/publishmodel')) {
      this.customRouter.previousUrl = 'dashboard/modelengineering/CompareModels';
      this.cus.disableTabs(this.currentIndex, this.breadcrumbIndex, true, null);
      this.customRouter.redirectToPrevious();
    }
  }

  showPublishModel() {
    this.router.navigate(['/dashboard/deploymodel/publishmodel'],
      {
        queryParams: {
          'publisModelFlag': true
        }
      });
  }

  // setClassOnElement(elementId: string, className: string) {
  //   this.eleRef.nativeElement.parentElement.querySelector('#' + elementId).className = className;
  // }
}
