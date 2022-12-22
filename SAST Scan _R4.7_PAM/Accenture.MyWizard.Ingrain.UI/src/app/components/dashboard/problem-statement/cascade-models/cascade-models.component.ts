import { Component, OnInit, OnDestroy } from '@angular/core';
import { NavigationStart, Router } from '@angular/router';
import { filter } from 'rxjs/operators';
import { Subscription } from 'rxjs';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';

@Component({
  selector: 'app-cascade-models',
  templateUrl: './cascade-models.component.html',
  styleUrls: ['./cascade-models.component.scss']
})
export class CascadeModelsComponent implements OnInit, OnDestroy {

  url = '';
  pageStatus = 'chooseCascade';
  subscription: Subscription;
  modelName = '';
  cascadedid;
  deliverytype;
  cascadeBreadCrumb;

  constructor(public router: Router, private cus: CoreUtilsService) {
    this.cascadeBreadCrumb = [
      {
        'mainTab': 'Choose Model', 'breadcrumbStatus': 'active', 'routerLink': 'chooseCascadeModels', 'tabIndex': 0
      },
      {
        'mainTab': 'Mapping', 'breadcrumbStatus': 'disabled', 'routerLink': 'mapCascadeModels', 'tabIndex': 1
      },
      {
        'mainTab': 'Publish Model', 'breadcrumbStatus': 'disabled', 'routerLink': 'publishCascadeModels', 'tabIndex': 2
      },
      {
        'mainTab': 'Deploy Model', 'breadcrumbStatus': 'disabled', 'routerLink': 'deployCascadeModels', 'tabIndex': 3
      }
    ];
    this.subscription =  router.events.pipe(
    filter(event => event instanceof NavigationStart))
    .subscribe((event: NavigationStart) => {
      // You only receive NavigationStart events
      this.url = event.url;
      const cascadedId = sessionStorage.getItem('cascadedId');
      if (this.url !== '' && cascadedId !== undefined) {
        if (this.url.includes('chooseCascadeModels')) {
          this.pageStatus = 'chooseCascade';
          this.panelClick(0);
        } else if (this.url.includes('mapCascadeModels')) {
          this.pageStatus = 'mapCascade';
          this.panelClick(1);
         // this.cascadeBreadCrumb[0].breadcrumbStatus = 'completed';
        } else if (this.url.includes('publishCascadeModels')) {
          this.pageStatus = 'publishCascade';
          this.panelClick(2);
          /* this.cascadeBreadCrumb[0].breadcrumbStatus = 'completed';
          this.cascadeBreadCrumb[1].breadcrumbStatus = 'completed'; */
        } else if (this.url.includes('deployCascadeModels')) {
          this.pageStatus = 'deployCascade';
          this.panelClick(3);
          /* this.cascadeBreadCrumb[0].breadcrumbStatus = 'completed';
          this.cascadeBreadCrumb[1].breadcrumbStatus = 'completed';
          this.cascadeBreadCrumb[2].breadcrumbStatus = 'completed'; */
        } /* else if (this.url.includes('teachTestCascadeModels')) {
          this.pageStatus = 'teachTestCascade';
        } */
      }
    });
    
  }

  ngOnInit() {
    this.cus.isCascadedModel.redirected = true;
    if (this.router.url.includes('chooseCascadeModels')) {
      this.pageStatus = 'chooseCascade';
      this.panelClick(0);
    } else if (this.router.url.includes('mapCascadeModels')) {
      this.pageStatus = 'mapCascade';
      this.panelClick(1);
    } else if (this.router.url.includes('publishCascadeModels')) {
      this.pageStatus = 'publishCascade';
      this.panelClick(2);
    } else if (this.router.url.includes('deployCascadeModels')) {
      this.pageStatus = 'deployCascade';
      this.panelClick(3);
    }
  }

  onActivate(componentReference) {
    // Below will subscribe to the searchItem emitter
    if (componentReference.hasOwnProperty('modelname')) {
      componentReference.modelname.subscribe((data) => {
        this.modelName = data;
     });
    }
    if (componentReference.hasOwnProperty('deliverytype')) {
      componentReference.deliverytype.subscribe((data) => {
        this.deliverytype = data;
     });
    }
    if (componentReference.hasOwnProperty('cascadedid')) {
      componentReference.cascadedid.subscribe((data) => {
        this.cascadedid = data;
     });
    }
    if (componentReference['router']['url'].includes('chooseCascadeModels')) {
      this.pageStatus = 'chooseCascade';
      this.panelClick(0);
    } else if (componentReference['router']['url'].includes('mapCascadeModels')) {
      this.pageStatus = 'mapCascade';
      this.panelClick(1);
    } else if (componentReference['router']['url'].includes('publishCascadeModels')) {
      this.pageStatus = 'publishCascade';
      this.panelClick(2);
    } else if (componentReference['router']['url'].includes('deployCascadeModels')) {
      this.pageStatus = 'deployCascade';
      this.panelClick(3);
    }
 }
  ngOnDestroy() {
    sessionStorage.removeItem('cascadedId');
    this.subscription.unsubscribe();
    this.cascadeBreadCrumb = [
      {
        'mainTab': 'Choose Model', 'breadcrumbStatus': 'active', 'routerLink': 'chooseCascadeModels', 'tabIndex': 0
      },
      {
        'mainTab': 'Mapping', 'breadcrumbStatus': 'disabled', 'routerLink': 'mapCascadeModels', 'tabIndex': 1
      },
      {
        'mainTab': 'Publish Model', 'breadcrumbStatus': 'disabled', 'routerLink': 'publishCascadeModels', 'tabIndex': 2
      },
      {
        'mainTab': 'Deploy Model', 'breadcrumbStatus': 'disabled', 'routerLink': 'deployCascadeModels', 'tabIndex': 3
      }
    ];
  }

  panelClick(index, previousNext?) {
    this.cascadeBreadCrumb.forEach((tab, i) => {
      if(index === i) {
        tab.breadcrumbStatus = 'active';
      } else if (i < index) {
        tab.breadcrumbStatus = 'completed';
      } else if (i > index) {
        if (tab.breadcrumbStatus !== 'disabled') {
          tab.breadcrumbStatus = '';
        }
      }
    });
    if(previousNext === true) {
      this.navigateToComponents(index);
    }
  }

  navigateToComponents(index) {
    if(index === 0) {
      this.router.navigate(['dashboard/problemstatement/cascadeModels/chooseCascadeModels'],
      {
        queryParams: { 'cascadedId': sessionStorage.getItem('cascadedId'), 'category': sessionStorage.getItem('cascadedCategory') }
      });
    } else if(index === 1) {
      this.router.navigate(['/dashboard/problemstatement/cascadeModels/mapCascadeModels'],
      {
        queryParams: {
          'modelName': this.modelName,
          'cascadedId': sessionStorage.getItem('cascadedId')
        }
      });
    } else if(index === 2) {
      this.router.navigate(['/dashboard/problemstatement/cascadeModels/publishCascadeModels'],{});
    } else if(index === 3) {
      this.router.navigate(['/dashboard/problemstatement/cascadeModels/deployCascadeModels'],{});
    }
  }

  navigateToFocusAreaPage() {
    this.router.navigate(['choosefocusarea']);
  }

}
