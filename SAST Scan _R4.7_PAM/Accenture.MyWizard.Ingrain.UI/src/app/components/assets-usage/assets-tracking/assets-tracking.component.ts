import { Component, OnInit, ViewChild } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { FormGroup } from '@angular/forms';
import * as _ from 'lodash';
import { BehaviorSubject } from 'rxjs/internal/BehaviorSubject';
import { FlipCardComponent } from 'src/app/_reusables/flip-card/flip-card.component';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { AssetTrackingService } from './assets-tracking.service';

@Component({
  selector: 'app-assets-tracking',
  templateUrl: './assets-tracking.component.html',
  styleUrls: ['./assets-tracking.component.scss']
})
export class AssetsTrackingComponent implements OnInit {
  @ViewChild(FlipCardComponent, { static: false }) flipCardComponent: FlipCardComponent;

  public VDSAppUsingIngrainFeature;
  public AppUsingIngrainIntegrationFeature;
  public listOfAppsUsingIngrain;
  public AppsUsingIngrain;
  public selectOption;
  public data;
  public dateControl: FormGroup;
  startDate = new Date();

  public bubbleChartFlipBarChart = {};
  public vdsAppUsageChartFlipBarChart = {};
  public AIAppintergrationChartFlipBarChart = {};
  public bubbleChartFlipBarChartxlabel = '';
  public vdsAppUsageChartFlipBarChartxlabel = '';
  public AIAppintergrationChartFlipBarChartxlabel = '';
  //  = {
  //   'Effort (Hrs) Effort (Hrs) Effort (Hrs) Effort (Hrs)': 78.01839253750093,
  //   'Team Size Team Size Team Size': 81.84440979760743,
  //   'Schedule (Days) Schedule (Days) Schedule (Days)': 54.198352145208695
  // };
  public clientDCHeadings = ['Application name', 'Total Client', 'Total DC', 'Total Training', 'Total Prediction', 'Total Request'];
  public sampleAppDetails = {
    'AppName': 'SPA', 'totalClient': 50, 'totalDC': 40, 'totaltraining': 30, 'totalPrediction': 12,
    'totalRequest': 4
  };
  public assetsUsageTableData = {};
  public bubbleChartFlip = false;
  public AIAppintergrationChartFlip = false;
  public vdsAppUsageChartFlip = false;
  public showAssetsTable = false;
  public showClientlevelDetails = {};
  totalAssets: any;
  constructor(private assetTrackingService: AssetTrackingService, private formBuilder: FormBuilder,
    private ns: NotificationService, private appUtilService: AppUtilsService) { }

  ngOnInit() {
    this.dateControl = this.formBuilder.group({
      startDateControl: [''],
      endDateControl: [''],
    });
    this.dateControl.get('startDateControl').setValue(this.startDate);
    this.dateControl.get('endDateControl').setValue(this.startDate);
    this.getAssetUsageData();
  }

  getAssetUsageData = () => {
    ;
    this.appUtilService.loadingStarted();
    const startDate = this.dateControl.get('startDateControl').value.toLocaleDateString();
    const endDate = this.dateControl.get('endDateControl').value.toLocaleDateString();
    this.assetTrackingService.getAssetUsageDashBoard(startDate, endDate).subscribe(
      (result) => {
        this.setAssetUsageData(result);
        this.appUtilService.loadingEnded();
      },
      (error) => {
        this.ns.error('Something went wrong.')
        this.appUtilService.loadingEnded();
      }
    )
  }

  setAssetUsageData(dataFromResponse) {
    /* Sample data */
    this.data = {
      'VDSFeatureWise': {
        'InstaML - Regression': 0,
        'Automated WorkFlow': 0,
        'Simulation Analytics': 0,
        'InstaML - Timeseries': 0
      },
      'ApplicationWise': {
        'Release Planner': 5,
        'Testing': 3,
        'VDS(SI)': 5,
        'CMA': 32,
        'myWizard.ImpactAnalyzer': 5,
        'SPA(Velocity)': 4,
        'SPE': 1,
        'SPA': 9
      },
      'AppIntegrationWise': {
        'Release Planner': {
          'Release Planner - TimeSeries': 2,
          'SPA(Velocity)-Team Level Sprint Velocity Prediction-TimeSeries': 1,
          'Release Planner-TimeSeries': 2
        },
        'Testing': {
          'Testing - TimeSeries': 1,
          'Testing - Regression': 2
        },
        'VDS(SI)': {
          'InstaML - Regression': 2,
          'Simulation Analytics': 3
        },
        'CMA': {
          'CMA - SimilarityAnalytics': 1,
          'CMA-Change Request Priority Model-Classification': 3,
          'CMA-Similarity Analysis': 4,
          'Similarity Analysis': 1,
          '5354e9b8-0eb1-4275-b666-6eba15c12b2c-Similarity Analysis': 2,
          'CMA-CMA-Similarity Analysis': 20
        },
        'myWizard.ImpactAnalyzer': {
          'myWizard.ImpactAnalyzerSimilarity Analysis': 5
        },
        'SPA(Velocity)': {
          'SPA(Velocity)-Team Level Velocity Prediction Planned Efforts-Regression': 4
        },
        'SPE': {
          'SPE-Multi_Class': 1
        },
        'SPA': {
          'SPA-SPA-Similarity Analysis': 9
        }
      },
      'VDSClientWise': {
        'InstaML - Regression': {
          '00500000-0000-0000-0000-000000000000': 5
        },
        'Automated WorkFlow': {
          '00100000-0000-0000-0000-000000000000': 4
        },
        'Simulation Analytics': {
          '1': 3
        }
      },
      'AppClientWise': {
        'Release Planner': {
          '00500000-0000-0000-0000-000000000000': 3,
          '00100000-0000-0000-0000-000000000000': 2
        },
        'Testing': {
          '00500000-0000-0000-0000-000000000000': 3
        },
        'VDS(SI)': {
          '00500000-0000-0000-0000-000000000000': 2,
          '1': 3
        },
        'CMA': {
          '00500000-0000-0000-0000-000000000000': 18,
          '00100000-0000-0000-0000-000000000000': 14
        },
        'myWizard.ImpactAnalyzer': {
          '00100000-0000-0000-0000-000000000000': 5
        },
        'SPA(Velocity)': {
          '09900000000000000000000000000000': 3
        },
        'SPE': {
          '7313a7b5-37a5-e811-a6ad-8cdcd453ddff': 1
        },
        'SPA': {
          '23a1fc6f-1676-4083-89fc-6d73a5cbebae': 5,
          '00100000-0000-0000-0000-000000000000': 4
        }
      },
      'AppFeatureClientWise': {
        'Release Planner - TimeSeries': {
          '00500000-0000-0000-0000-000000000000': 2
        },
        'Testing - TimeSeries': {
          '00500000-0000-0000-0000-000000000000': 1
        },
        'Testing - Regression': {
          '00500000-0000-0000-0000-000000000000': 2
        },
        'CMA - SimilarityAnalytics': {
          '00500000-0000-0000-0000-000000000000': 1
        },
        'CMA-Change Request Priority Model-Classification': {
          '00100000-0000-0000-0000-000000000000': 3
        },
        'SPA(Velocity)-Team Level Sprint Velocity Prediction-TimeSeries': {
          '00500000-0000-0000-0000-000000000000': 1
        },
        'myWizard.ImpactAnalyzerSimilarity Analysis': {
          '00100000-0000-0000-0000-000000000000': 5
        },
        'CMA-Similarity Analysis': {
          '00500000-0000-0000-0000-000000000000': 4
        },
        'Similarity Analysis': {
          '00500000-0000-0000-0000-000000000000': 1
        },
        'Release Planner-TimeSeries': {
          '00100000-0000-0000-0000-000000000000': 2
        },
        'SPA(Velocity)-Team Level Velocity Prediction Planned Efforts-Regression': {
          '09900000000000000000000000000000': 3
        },
        'SPE-Multi_Class': {
          '7313a7b5-37a5-e811-a6ad-8cdcd453ddff': 1
        },
        '5354e9b8-0eb1-4275-b666-6eba15c12b2c-Similarity Analysis': {
          '00500000-0000-0000-0000-000000000000': 2
        },
        'CMA-CMA-Similarity Analysis': {
          '00500000-0000-0000-0000-000000000000': 10,
          '00100000-0000-0000-0000-000000000000': 10
        },
        '-Similarity Analysis': {
          '00500000-0000-0000-0000-000000000000': 1
        },
        'SPA-SPA-Similarity Analysis': {
          '23a1fc6f-1676-4083-89fc-6d73a5cbebae': 5,
          '00100000-0000-0000-0000-000000000000': 4
        }
      },
      'TrainingWise': {
        'Release Planner': 4,
        'Testing': 3,
        'VDS(SI)': 2,
        'CMA': 30,
        'myWizard.ImpactAnalyzer': 5,
        'SPA(Velocity)': 1,
        'SPE': 1,
        'SPA': 9
      },
      'PredictionWise': {
        'Release Planner': 1,
        'Testing': 0,
        'VDS(SI)': 0,
        'CMA': 2,
        'myWizard.ImpactAnalyzer': 0,
        'SPA(Velocity)': 3,
        'SPE': 0,
        'SPA': 0
      },
      'AppClientDCWise': {
        'Release Planner': {
          '00500000-0000-0000-0000-000000000000': {
            '7e57e9b7-816b-4cf5-b4b9-0685b53cdb0a': 3
          },
          '00100000-0000-0000-0000-000000000000': {
            '1e4272c7-d9e6-4df8-b3da-da3d056f35a1': 2
          }
        },
        'Testing': {
          '00500000-0000-0000-0000-000000000000': {
            '7e57e9b7-816b-4cf5-b4b9-0685b53cdb0a': 3
          }
        },
        'VDS(SI)': {
          '00500000-0000-0000-0000-000000000000': {
            '7e57e9b7-816b-4cf5-b4b9-0685b53cdb0a': 2
          },
          '1': {
            '11': 3
          }
        },
        'CMA': {
          '00500000-0000-0000-0000-000000000000': {
            'f76b41a5-5a5e-459b-b632-75228cbb83b7': 18
          },
          '00100000-0000-0000-0000-000000000000': {
            'ce63f45c-5688-e811-a9ca-00155da6d537': 4,
            '1e4272c7-d9e6-4df8-b3da-da3d056f35a1': 13
          }
        },
        'myWizard.ImpactAnalyzer': {
          '00100000-0000-0000-0000-000000000000': {
            'd95517f4-648b-45ff-ad53-fad81516285f': 1,
            '685a4c1c-bdb0-4a03-a220-f3c6d60e85e8': 1,
            'e12be6bb-132e-4b59-b8c7-6a2d4bc00ac5': 1,
            'e605f271-391f-4469-bcf7-f4970cdd8aec': 1,
            '4217e5d7-5588-e811-a9ca-00155da6d537': 1
          }
        },
        'SPA(Velocity)': {
          '09900000000000000000000000000000': {
            'BA39DB64381E442C8E987EAE656A6CF9': 1
          }
        },
        'SPE': {
          '7313a7b5-37a5-e811-a6ad-8cdcd453ddff': {
            'c1db9fe7-061d-4261-90cf-07fd8d16fe2c': 1
          }
        },
        'SPA': {
          '23a1fc6f-1676-4083-89fc-6d73a5cbebae': {
            'c79694f4-265d-4b01-a391-9ff8c6e77562': 5
          },
          '00100000-0000-0000-0000-000000000000': {
            '1e4272c7-d9e6-4df8-b3da-da3d056f35a1': 4
          }
        }
      },
      'AppTrainingDCWise': {
        'Release Planner': {
          '7e57e9b7-816b-4cf5-b4b9-0685b53cdb0a': 3,
          '1e4272c7-d9e6-4df8-b3da-da3d056f35a1': 1
        },
        'Testing': {
          '7e57e9b7-816b-4cf5-b4b9-0685b53cdb0a': 3
        },
        'VDS(SI)': {
          '7e57e9b7-816b-4cf5-b4b9-0685b53cdb0a': 2,
          '11': 0
        },
        'CMA': {
          'f76b41a5-5a5e-459b-b632-75228cbb83b7': 16,
          'ce63f45c-5688-e811-a9ca-00155da6d537': 4,
          '1e4272c7-d9e6-4df8-b3da-da3d056f35a1': 10
        },
        'myWizard.ImpactAnalyzer': {
          'd95517f4-648b-45ff-ad53-fad81516285f': 1,
          '685a4c1c-bdb0-4a03-a220-f3c6d60e85e8': 1,
          'e12be6bb-132e-4b59-b8c7-6a2d4bc00ac5': 1,
          'e605f271-391f-4469-bcf7-f4970cdd8aec': 1,
          '4217e5d7-5588-e811-a9ca-00155da6d537': 1
        },
        'SPA(Velocity)': {
          'BA39DB64381E442C8E987EAE656A6CF9': 1
        },
        'SPE': {
          'c1db9fe7-061d-4261-90cf-07fd8d16fe2c': 1
        },
        'SPA': {
          'c79694f4-265d-4b01-a391-9ff8c6e77562': 5,
          '1e4272c7-d9e6-4df8-b3da-da3d056f35a1': 4
        }
      },
      'AppPredictionDCWise': {
        'Release Planner': {
          '7e57e9b7-816b-4cf5-b4b9-0685b53cdb0a': 0,
          '1e4272c7-d9e6-4df8-b3da-da3d056f35a1': 1
        },
        'Testing': {
          '7e57e9b7-816b-4cf5-b4b9-0685b53cdb0a': 0
        },
        'VDS(SI)': {
          '7e57e9b7-816b-4cf5-b4b9-0685b53cdb0a': 0,
          '11': 0
        },
        'CMA': {
          'f76b41a5-5a5e-459b-b632-75228cbb83b7': 2,
          'ce63f45c-5688-e811-a9ca-00155da6d537': 0,
          '1e4272c7-d9e6-4df8-b3da-da3d056f35a1': 0
        },
        'myWizard.ImpactAnalyzer': {
          'd95517f4-648b-45ff-ad53-fad81516285f': 0,
          '685a4c1c-bdb0-4a03-a220-f3c6d60e85e8': 0,
          'e12be6bb-132e-4b59-b8c7-6a2d4bc00ac5': 0,
          'e605f271-391f-4469-bcf7-f4970cdd8aec': 0,
          '4217e5d7-5588-e811-a9ca-00155da6d537': 0
        },
        'SPA(Velocity)': {
          'BA39DB64381E442C8E987EAE656A6CF9': 0
        },
        'SPE': {
          'c1db9fe7-061d-4261-90cf-07fd8d16fe2c': 0
        },
        'SPA': {
          'c79694f4-265d-4b01-a391-9ff8c6e77562': 0,
          '1e4272c7-d9e6-4df8-b3da-da3d056f35a1': 0
        }
      }
    };

    this.data = dataFromResponse;
    this.VDSAppUsingIngrainFeature = this.convertIntoArrayForVDS(this.data.VDSFeatureWise);
    this.AppsUsingIngrain = { 'children': this.convertIntoArray(this.data.ApplicationWise) }

    // this.VDSAppUsingIngrainFeature = this.data.VDSFeatureWise;
    this.listOfAppsUsingIngrain = Object.keys(this.data.AppIntegrationWise);
    if (this.listOfAppsUsingIngrain.length > 0) {
      this.selectOption = this.listOfAppsUsingIngrain[0];
      this.AppUsingIngrainIntegrationFeature = this.data.AppIntegrationWise[this.listOfAppsUsingIngrain[0]];
    } else {
      this.AppUsingIngrainIntegrationFeature = null;
    }

    // Table
    this.assetsUsageTableData = this.data.AppClientDCWise;
    this.totalAssets = Object.keys(this.data.AppClientDCWise);

  }

  convertIntoArray(jsonFormat) {
    let s = {};
    let a = [];
    const ob = Object.keys(jsonFormat);
    for (let i = 0; i < ob.length; i++) {
      s = { 'Name': '', 'Count': 0 };
      s['Name'] = ob[i];
      s['Count'] = jsonFormat[ob[i]];
      a.push(s);
    }
    a = a.sort(function (x, y) { return x.Count - y.Count; });
    //  a = a.sort((e1, e2) => (e1.Count > e2.Count) ? 1 : ((e1.Count > e2.Count) ? -1 : 0));
    return (a.length > 0 ? a : null);
  }

  convertIntoArrayForVDS(jsonFormat) {
    let s = {};
    let a = [];
    let ob = Object.keys(jsonFormat);
    for (let i = 0; i < ob.length; i++) {
      s = { 'featurename': '', 'prcntvalue': 0 };
      s['featurename'] = ob[i];
      s['prcntvalue'] = jsonFormat[ob[i]];
      a.push(s);
    }
    return (a.length > 0 ? a : null);
  }

  selectedOptions(option) {
    this.selectOption = option;
    this.AppUsingIngrainIntegrationFeature = this.data.AppIntegrationWise[this.selectOption];
    this.AIAppintergrationChartFlip = false;
  }

  sendDataToBarGraph(data) {
    // console.log(data);    // data: {Name: 'SPA', Count: 2514}
    this.bubbleChartFlipBarChartxlabel = data.Name;
    if ( this.data.AppClientWise.hasOwnProperty(data.Name)) {
    this.bubbleChartFlipBarChart = this.data.AppClientWise[data.Name];
    this.bubbleChartFlip = true;
    } else {
     this.ns.error('No data found'); 
    }
  }

  sendDataToBarGraphAIAssets(data) {
    // console.log(data);    // data: {key: 'myWizard.ImpactAnalyzer-myWizard.ImpactAnalyzer-Predict SPI-Regression' value: 450 }
    this.AIAppintergrationChartFlipBarChartxlabel = data.key;
    if ( this.data.AppFeatureClientWise.hasOwnProperty(data.key)) {
    this.AIAppintergrationChartFlipBarChart = this.data.AppFeatureClientWise[data.key];
    this.AIAppintergrationChartFlip = true;
    } else {
    this.ns.error('No data found');
    }
  }

  sendDataToBarGraphVDSUsage(data) {
    // console.log(data); // data {featurename: 'InstaML - Timeseries', prcntvalue: 108}
    if ( this.data.VDSClientWise.hasOwnProperty(data.featurename)) {
    this.vdsAppUsageChartFlipBarChartxlabel = data.featurename;
    this.vdsAppUsageChartFlipBarChart = this.data.VDSClientWise[data.featurename];
    this.vdsAppUsageChartFlip = true;
    } else {
    this.ns.error('No data found');
    }
  }

  getClientCount(keyname) {
 const a =  this.assetsUsageTableData[keyname];
 return Object.keys(a).length;
  }

  getDCCount(keyname, dcid) {
    const a =  this.data.AppTrainingDCWise[keyname];
    return Object.keys(a).length;
  }

  getTotalTraining(keyname) {
   return this.data.TrainingWise[keyname];
  }

  getTotalPrediction(keyname) {
    const a = this.data.PredictionWise[keyname];
    return a;
  }

  getTotalTrainingClientLevel(keyname, clientId, dcId) {
    let total = 0;
    if (dcId) {
     total = this.data.AppTrainingDCWise[keyname][dcId];
    } else {
    const dcname = this.data.AppClientDCWise[keyname][clientId];
 
    for ( const keyname2 in dcname) {
      if ( dcname) {
       total += this.data.AppTrainingDCWise[keyname][keyname2];
      }
    }
    }
    return total;
   }

  getTotalPredictionClientLevel(keyname, clientId, dcId) {
    let total = 0;
    if ( dcId) {
       total = this.data.AppPredictionDCWise[keyname][dcId];
    } else {
    const dcname = this.data.AppClientDCWise[keyname][clientId];
   
    for ( const n in dcname) {
      if ( dcname) {
       total += this.data.AppPredictionDCWise[keyname][n];
      }
    }
   }
    return total;
   }
  getObjectKey( object) {
    return Object.keys(object);
  }

  changeView(){
    this.showAssetsTable = !this.showAssetsTable;
    if ( this.showClientlevelDetails) {
    this.showClientlevelDetails = {};
    }
  }
}

/*

'Release Planner': {
  '00500000-0000-0000-0000-000000000000': {
      '7e57e9b7-816b-4cf5-b4b9-0685b53cdb0a': 3
  },
  '00100000-0000-0000-0000-000000000000': {
      '1e4272c7-d9e6-4df8-b3da-da3d056f35a1': 2
  }
}

   'release planner' : [
     { 'clienta''00500000-0000-0000-0000-000000000000' : []
   ]

*/
