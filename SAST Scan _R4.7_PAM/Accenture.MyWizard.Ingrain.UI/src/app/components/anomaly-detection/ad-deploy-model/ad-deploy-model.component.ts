import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';
import { CustomRoutingService } from 'src/app/_services/custom-routing.service';

@Component({
  selector: 'app-ad-deploy-model',
  templateUrl: './ad-deploy-model.component.html',
  styleUrls: ['./ad-deploy-model.component.scss']
})
export class AdDeployModelComponent implements OnInit {
  showNextButton = false;
  currentIndex = 2;
  breadcrumbIndex = 0;

  constructor(private cus: CoreUtilsService, private customRouter: CustomRoutingService,
    private router: Router) { }

  ngOnInit(): void {
  }

  next() {
    this.cus.disableADTabs(this.currentIndex, this.breadcrumbIndex, null, true);
    this.customRouter.redirectToNext();
  }

  previous() {
    if (this.customRouter.urlAfterRedirects.startsWith('/anomaly-detection/deploymodel/deployedmodel')) {
      this.cus.disableADTabs(this.currentIndex, 1, true, null);
      this.router.navigate(['/anomaly-detection/deploymodel/publishmodel'],
        {
          queryParams: {
            'publisModelFlag': true
          }
        });
    }

    if (this.customRouter.urlAfterRedirects.startsWith('/anomaly-detection/deploymodel/publishmodel')) {
      this.customRouter.previousUrl = 'anomaly-detection/modelengineering';
      this.cus.disableADTabs(this.currentIndex, this.breadcrumbIndex, true, null);
      this.customRouter.redirectToPrevious();
    }
  }

  showPublishModel() {
    this.router.navigate(['/anomaly-detection/deploymodel/publishmodel'],
      {
        queryParams: {
          'publisModelFlag': true
        }
      });
  }

}
