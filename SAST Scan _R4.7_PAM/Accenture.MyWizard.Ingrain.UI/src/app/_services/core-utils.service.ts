import { Injectable } from '@angular/core';
import { LoginResultModel } from '../_models/login-result-model';
import { NotificationService } from './notification-service.service';

@Injectable({
  providedIn: 'root'
})
export class CoreUtilsService {
 public allTabs = [
    {
      'mainTab': 'Problem Statement', 'status': 'active', 'routerLink': 'problemstatement/usecasedefinition', 'tabIndex': 0, 'subTab': [
        { 'childTab': 'Use Case Definition', 'status': 'active', 'routerLink': 'problemstatement/usecasedefinition', 'breadcrumbStatus': 'active' }
      ]
    },
    {
      'mainTab': 'Data Engineering', 'status': 'disabled', 'routerLink': 'dataengineering/datacleanup', 'tabIndex': 1, 'subTab': [
        { 'childTab': 'Data Curation', 'status': 'active', 'routerLink': 'dataengineering/datacleanup', 'breadcrumbStatus': 'active' },
        { 'childTab': 'Data Transformation', 'status': 'disabled', 'routerLink': 'dataengineering/preprocessdata', 'breadcrumbStatus': 'disabled' }
      ]
    },
    {
      'mainTab': 'Model Engineering', 'status': 'disabled', 'routerLink': 'modelengineering/FeatureSelection', 'tabIndex': 2, 'subTab': [
        { 'childTab': 'Feature Selection', 'status': 'active', 'routerLink': 'modelengineering/FeatureSelection', 'breadcrumbStatus': 'active' },
        { 'childTab': 'Recommended AI', 'status': 'active', 'routerLink': 'modelengineering/RecommendedAI', 'breadcrumbStatus': 'disabled' },
        { 'childTab': 'Teach and Test', 'status': 'disabled', 'routerLink': 'modelengineering/TeachAndTest', 'breadcrumbStatus': 'disabled' },
        { 'childTab': 'Compare Test Scenarios', 'status': 'disabled', 'routerLink': 'modelengineering/CompareModels', 'breadcrumbStatus': 'disabled' }
      ]
    },
    {
      'mainTab': 'Deploy Model', 'status': 'disabled', 'routerLink': 'deploymodel/publishmodel', 'tabIndex': 3, 'subTab': [
        { 'childTab': 'Publish Model', 'status': 'active', 'routerLink': 'deploymodel/publishmodel', 'breadcrumbStatus': 'active' },
        { 'childTab': 'Deployed Model', 'status': 'disabled', 'routerLink': 'deploymodel/deployedmodel', 'breadcrumbStatus': 'disabled' }
      ]
    }
  ];

  public allADTabs = [
    {
      'mainTab': 'Problem Statement', 'status': 'active', 'routerLink': 'problemstatement/usecasedefinition', 'tabIndex': 0, 'subTab': [
        { 'childTab': 'Use Case Definition', 'status': 'active', 'routerLink': 'problemstatement/usecasedefinition', 'breadcrumbStatus': 'active' }
      ]
    },
    {
      'mainTab': 'Model Engineering', 'status': 'disabled', 'routerLink': 'modelengineering/FeatureSelection', 'tabIndex': 1, 'subTab': [
        { 'childTab': 'Feature Selection', 'status': 'active', 'routerLink': 'modelengineering/FeatureSelection', 'breadcrumbStatus': 'active' },
      ]
    },
    {
      'mainTab': 'Deploy Model', 'status': 'disabled', 'routerLink': 'deploymodel/publishmodel', 'tabIndex': 2, 'subTab': [
        { 'childTab': 'Publish Model', 'status': 'active', 'routerLink': 'deploymodel/publishmodel', 'breadcrumbStatus': 'active' },
        { 'childTab': 'Deployed Model', 'status': 'disabled', 'routerLink': 'deploymodel/deployedmodel', 'breadcrumbStatus': 'disabled' }
      ]
    }
  ];
  constructor(private ns: NotificationService) {
  }

  public isCascadedModel = {'redirected': false};

  isNil(val) {
    return val === null || val === undefined || val === '';
  }

  isNumeric(val) {
    return val !== null && !isNaN(val);
  }
  isIE() {
    const userAgent = navigator.userAgent;
    return userAgent.indexOf('MSIE ') > -1 || userAgent.indexOf('Trident/') > -1;
  }

  isEmptyObject(obj) {
    for (const key in obj) {
      if (obj.hasOwnProperty(key)) {
        return false;
      }
    }
    return JSON.stringify(obj) === JSON.stringify({});
  }

  isSpecialCharacter(input: string) {
    const regex = /^[A-Za-z0-9 ]+$/;
    if (input && input.length > 0) {
      const isValid = regex.test(input);
      if (!isValid) {
        this.ns.warning('No special characters allowed.');
        return 0; // Return 0 , if input string contains special character
      } else {
        return 1; // Return 1 , if input string does not contains special character
      }
    }
  }

    disableTabs(tabIndex, breadcrumbIndex?, previous?, next?) {
    let isNewModel = sessionStorage.getItem('isNewModel');
    let isModelDeployed = sessionStorage.getItem('isModelDeployed');
    let isModelTrained = sessionStorage.getItem('isModelTrained');
    let isApplyTrue = sessionStorage.getItem('applyFlag');
    if (next === undefined || previous === undefined) {
      this.allTabs.forEach((tab, i) => {
        if (tabIndex === i) {
          this.allTabs[i].status = 'active';
          this.allTabs[i].subTab.forEach((subtab, j) => {
            if (breadcrumbIndex === j) {
              this.allTabs[i].subTab[j].breadcrumbStatus = 'active';
              if (isModelTrained) {
                if (i === 2 && j === 1 && isModelTrained === 'true') { //Recommended AI - training completed
                  this.allTabs[2].subTab[2].breadcrumbStatus = ''; // Enable Teach n test page
                  this.allTabs[3].status = ''; // Enable Deploy Model Tab
                }
              } 
            } else {
              if (j < breadcrumbIndex) {
                this.allTabs[i].subTab[j].breadcrumbStatus = 'completed';
              } else {
                if (isModelTrained && tabIndex < 3) {
                  if (isModelTrained === 'true') {
                    this.allTabs[i].subTab[j].breadcrumbStatus = '';
                  }
                } if (isApplyTrue && tabIndex <= 1) {
                  if (isApplyTrue === 'true') {
                    if (this.allTabs[i].subTab[j].breadcrumbStatus !== 'disabled') {
                      this.allTabs[i].subTab[j].breadcrumbStatus = '';
                    }
                  }
                } if (isModelTrained != 'true' && isApplyTrue != 'true') {
                  if (this.allTabs[i].subTab[j].breadcrumbStatus !== 'disabled') {
                    this.allTabs[i].subTab[j].breadcrumbStatus = '';
                  }
                } if (isModelDeployed && tabIndex === 3) {
                  if (isModelDeployed === 'true') {
                    this.allTabs[i].subTab[j].breadcrumbStatus = '';
                  }
                }
              }
            }
          });
        } else {
          if (i > tabIndex) {
            if (isNewModel) {
              if (isNewModel === 'true') {
                if (this.allTabs[i].status != 'disabled') {
                  this.allTabs[i].status = 'disabled';
                }
              }
            }
            if (isApplyTrue) {
              if (isApplyTrue == 'true') {
                // Enable Problem Statement and Data Engineering tabs
                if (i <= 1) {
                  this.allTabs[i].status = '';
                  this.allTabs[1].subTab[0].breadcrumbStatus = '';
                  this.allTabs[1].subTab[1].breadcrumbStatus = '';
                } else {
                  if (this.allTabs[i].status !== 'disabled') {
                    this.allTabs[i].status = '';
                  }
                }
              }
            }
            if (isModelTrained) {
              if (isModelTrained == 'true') {
                if (i <= 2) {
                  this.allTabs[i].status = '';
                  // Enabling Feature Selection and RecommendedAI
                  this.allTabs[2].subTab[0].breadcrumbStatus = '';
                  this.allTabs[2].subTab[1].breadcrumbStatus = '';
                } else {
                  if (this.allTabs[i].status !== 'disabled') {
                    this.allTabs[i].status = '';
                  }
                  if (tabIndex == 2 && breadcrumbIndex >= 1) {
                    this.allTabs[3].status = ''; // Enable Deploy model
                  }
                }
              } else {
                if (this.allTabs[i].status !== 'disabled') {
                  this.allTabs[i].status = '';
                }
              }
            }
            if (isModelDeployed) {
              if (isModelDeployed == 'true') {
                this.allTabs[i].status = '';
                this.allTabs[3].subTab[1].breadcrumbStatus = '';
              }
            }
          } else if (i < tabIndex) {
            this.allTabs[i].status = '';
            if (isApplyTrue) {
              if (isApplyTrue == 'true') {
                if (i <= 1) {
                  // Enable Use case definition
                  this.allTabs[1].subTab[0].breadcrumbStatus = '';
                }
              }
            }
          }
        }
      });
    } else {
      if (previous) {
        if(isModelDeployed == 'true' || isModelTrained == 'true') {
          if (breadcrumbIndex === 0) {
            this.allTabs[tabIndex-1].subTab.forEach((subtab, j) => {
              this.allTabs[tabIndex-1].subTab[j].breadcrumbStatus = '';
            });
          } else {
            this.allTabs[tabIndex].subTab[breadcrumbIndex - 1].breadcrumbStatus = '';
          }
        }
        if (breadcrumbIndex === 0) {
          this.allTabs[tabIndex].status = '';
          this.allTabs[tabIndex - 1].status = 'active';
        } else {
          this.allTabs[tabIndex].status = 'active';
          this.allTabs[tabIndex].subTab[breadcrumbIndex].breadcrumbStatus = '';
          this.allTabs[tabIndex].subTab[breadcrumbIndex - 1].breadcrumbStatus = 'active';
        }
      } else if (next) {
        const lastBreadCrumbIndex = this.allTabs[tabIndex].subTab.length - 1;
        if(isModelDeployed == 'true' || isModelTrained == 'true') {
          if (breadcrumbIndex === lastBreadCrumbIndex) {
            if(isModelTrained == 'true' && isModelDeployed == 'false') {
              this.allTabs[tabIndex+1].subTab.forEach((subtab, j) => {
                if(tabIndex<=2 && j<2) {
                  this.allTabs[tabIndex+1].subTab[j].breadcrumbStatus = '';
                }
              });
            } else {
              this.allTabs[tabIndex+1].subTab.forEach((subtab, j) => {
                this.allTabs[tabIndex+1].subTab[j].breadcrumbStatus = '';
              });
            }
          } else {
            this.allTabs[tabIndex].subTab[breadcrumbIndex + 1].breadcrumbStatus = 'active';
          }
        }
        if (breadcrumbIndex === lastBreadCrumbIndex) {
          this.allTabs[tabIndex].status = '';
          this.allTabs[tabIndex + 1].status = 'active';
          this.allTabs[tabIndex + 1].subTab[0].breadcrumbStatus = 'active';
        } else {
          this.allTabs[tabIndex].status = 'active';
          this.allTabs[tabIndex].subTab[breadcrumbIndex].breadcrumbStatus = 'completed';
          this.allTabs[tabIndex].subTab[breadcrumbIndex + 1].breadcrumbStatus = 'active';
        }
      }
    }
  }


  compareTwoObjectDifference( a1: Array<any>, a2: Array<any> ) {
    let withAllArrayItems = a1;
    let withSomeArrayItems = a2;
    let difference = [];

    // if ( a1.length > a2.length) {
    //   withAllArrayItems = a1; 
    //   withSomeArrayItems = a2;  
    // } else if ( a2.length > a1.length) {
    //   withSomeArrayItems = a2;
    //   withAllArrayItems = a1;
    // }

     difference = withAllArrayItems.filter(function(o1){
      return !withSomeArrayItems.some(function(o2){
          return JSON.stringify(o1) === JSON.stringify(o2);     
      });
    });

    return difference;
  }


  // method to disable Anomaly Detection tabs
  disableADTabs(tabIndex, breadcrumbIndex?, previous?, next?) {
    let isNewModel = sessionStorage.getItem('isNewModel');
    let isModelDeployed = sessionStorage.getItem('isModelDeployed');
    let isModelTrained = sessionStorage.getItem('isModelTrained');
    let isApplyTrue = sessionStorage.getItem('applyFlag');
    if (next === undefined || previous === undefined) {
      this.allADTabs.forEach((tab, i) => {
        if (tabIndex === i) {
          this.allADTabs[i].status = 'active';
          this.allADTabs[i].subTab.forEach((subtab, j) => {
            if (breadcrumbIndex === j) {
              this.allADTabs[i].subTab[j].breadcrumbStatus = 'active';
              if (isModelTrained) {
                if (i === 2 && j === 1 && isModelTrained === 'true') { //Publish model page
                  this.allADTabs[i].subTab[j].breadcrumbStatus = 'active'; 
                }
              } 
            } else {
              if (j < breadcrumbIndex) {
                this.allADTabs[i].subTab[j].breadcrumbStatus = 'completed';
              } else {
                if (isModelTrained && tabIndex < 2) {
                  if (isModelTrained === 'true') {
                    this.allADTabs[i].subTab[j].breadcrumbStatus = '';
                  }
                } if (isApplyTrue && tabIndex <= 1) {
                  if (isApplyTrue === 'true') {
                    if (this.allADTabs[i].subTab[j].breadcrumbStatus !== 'disabled') {
                      this.allADTabs[i].subTab[j].breadcrumbStatus = '';
                    }
                  }
                } if (isModelTrained != 'true' && isApplyTrue != 'true') {
                  if (this.allADTabs[i].subTab[j].breadcrumbStatus !== 'disabled') {
                    this.allADTabs[i].subTab[j].breadcrumbStatus = '';
                  }
                } if (isModelDeployed && tabIndex === 2) {
                  if (isModelDeployed === 'true') {
                    this.allADTabs[i].subTab[j].breadcrumbStatus = '';
                  }
                }
              }
            }
          });
        } else {
          if (i > tabIndex) {
            if (isNewModel) {
              if (isNewModel === 'true') {
                if (this.allADTabs[i].status != 'disabled') {
                  this.allADTabs[i].status = 'disabled';
                }
              }
            }
            if (isApplyTrue) {
              if (isApplyTrue == 'true') {
                // Enable Problem Statement and Data Engineering tabs
                if (i <= 1) {
                  this.allADTabs[i].status = '';
                  this.allADTabs[1].subTab[0].breadcrumbStatus = '';
                  this.allADTabs[1].subTab[1].breadcrumbStatus = '';
                } else {
                  if (this.allADTabs[i].status !== 'disabled') {
                    this.allADTabs[i].status = '';
                  }
                }
              }
            }
            if (isModelTrained) {
              if (isModelTrained == 'true') {
                if (i <= 1) {
                  this.allADTabs[i].status = '';
                  // Enabling Feature Selection and RecommendedAI
                  this.allADTabs[1].subTab[0].breadcrumbStatus = '';
                  //this.allADTabs[1].subTab[1].breadcrumbStatus = '';
                } else {
                  if (this.allADTabs[i].status !== 'disabled') {
                    this.allADTabs[i].status = '';
                  }
                  if (tabIndex == 2 && breadcrumbIndex >= 1) {
                    this.allADTabs[2].status = ''; // Enable Deploy model
                  }
                }
              } else {
                if (this.allADTabs[i].status !== 'disabled') {
                  this.allADTabs[i].status = '';
                }
              }
            }
            if (isModelDeployed) {
              if (isModelDeployed == 'true') {
                this.allADTabs[i].status = '';
                this.allADTabs[2].subTab[1].breadcrumbStatus = '';
              }
            }
          } else if (i < tabIndex) {
            this.allADTabs[i].status = '';
            if (isApplyTrue) {
              if (isApplyTrue == 'true') {
                if (i <= 1) {
                  // Enable Use case definition
                  this.allADTabs[0].subTab[0].breadcrumbStatus = '';
                }
              }
            }
          }
        }
      });
    } else {
      if (previous) {
        if(isModelDeployed == 'true' || isModelTrained == 'true') {
          if (breadcrumbIndex === 0) {
            this.allADTabs[tabIndex-1].subTab.forEach((subtab, j) => {
              this.allADTabs[tabIndex-1].subTab[j].breadcrumbStatus = '';
            });
          } else {
            this.allADTabs[tabIndex].subTab[breadcrumbIndex - 1].breadcrumbStatus = '';
          }
        }
        if (breadcrumbIndex === 0) {
          this.allADTabs[tabIndex].status = '';
          this.allADTabs[tabIndex - 1].status = 'active';
        } else {
          this.allADTabs[tabIndex].status = 'active';
          this.allADTabs[tabIndex].subTab[breadcrumbIndex].breadcrumbStatus = '';
          this.allADTabs[tabIndex].subTab[breadcrumbIndex - 1].breadcrumbStatus = 'active';
        }
      } else if (next) {
        const lastBreadCrumbIndex = this.allADTabs[tabIndex].subTab.length - 1;
        if(isModelDeployed == 'true' || isModelTrained == 'true') {
          if (breadcrumbIndex === lastBreadCrumbIndex) {
            if(isModelTrained == 'true' && isModelDeployed == 'false') {
              this.allADTabs[tabIndex+1].subTab.forEach((subtab, j) => {
                if(tabIndex<=2 && j<2) {
                  this.allADTabs[tabIndex+1].subTab[j].breadcrumbStatus = '';
                }
              });
            } else {
              this.allADTabs[tabIndex+1].subTab.forEach((subtab, j) => {
                this.allADTabs[tabIndex+1].subTab[j].breadcrumbStatus = '';
              });
            }
          } else {
            this.allADTabs[tabIndex].subTab[breadcrumbIndex + 1].breadcrumbStatus = 'active';
          }
        }
        if (breadcrumbIndex === lastBreadCrumbIndex) {
          this.allADTabs[tabIndex].status = '';
          this.allADTabs[tabIndex + 1].status = 'active';
          this.allADTabs[tabIndex + 1].subTab[0].breadcrumbStatus = 'active';
        } else {
          this.allADTabs[tabIndex].status = 'active';
          this.allADTabs[tabIndex].subTab[breadcrumbIndex].breadcrumbStatus = 'completed';
          this.allADTabs[tabIndex].subTab[breadcrumbIndex + 1].breadcrumbStatus = 'active';
        }
      }
    }
  }


}
