import { Component, OnInit } from '@angular/core';
import { CustomRoutingService } from 'src/app/_services/custom-routing.service';
import { Router, ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-teach-test-cascade-models',
  templateUrl: './teach-test-cascade-models.component.html',
  styleUrls: ['./teach-test-cascade-models.component.scss']
})
export class TeachTestCascadeModelsComponent implements OnInit {

  constructor(private router: Router, private customRouter: CustomRoutingService) { }
  openFeatureValues = true;

  ngOnInit() {
  }

  /* next() {
    const data = {
      'DeployModels' : {
        'Accuracy' : .2,
        'CorrelationId': 'abc',
        'IsPrivate': true,
        'LinkedApps' : ['VDS'],
        'ModelName': 'Cascade',
        'ModelURL': 'abc',
        'WebServices': 'a',
        'ModelVersion': 2,
        'DeployedDate': '22/10/2020',
        'Status': 'Deployed',
        'InputSample': 'Sample'
      },
      'DataSource' : 'File',
      'BusinessProblem': 'NA',
    }
    this.router.navigate(['/dashboard/deploymodel/deployedmodel'],
      {
        queryParams: {
          'accuracy': data.DeployModels.Accuracy, 'CorrelationId': data.DeployModels.CorrelationId,
          'DataSource': data.DataSource, 'IsPrivate': data.DeployModels.IsPrivate, 'LinkedApps': data.DeployModels.LinkedApps,
          'ModelName': data.DeployModels.ModelName, 'ModelURL': data.DeployModels.ModelURL,
          'WebServices': data.DeployModels.WebServices, 'UseCase': data.BusinessProblem,
          'TrainedModelName': data.DeployModels.ModelVersion, 'DeployDate': data.DeployModels.DeployedDate,
          'Status': data.DeployModels.Status, 'pyloadForWebLink': data.DeployModels.InputSample,
        }
      });
  } */

  next() {
    this.customRouter.redirectToNext();
  }


  previous() {
    this.customRouter.redirectToPrevious();
  }

  collapseFeatureValues(isFromDropdownChange) {
    if (isFromDropdownChange === false) {
      this.openFeatureValues = this.openFeatureValues === true ? false : true;
    }
    return this.openFeatureValues;
  }


}
