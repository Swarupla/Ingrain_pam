import { Injectable } from '@angular/core';
import { EnvironmentService } from 'src/app/_services/EnvironmentService';
declare var userNotification: any;

@Injectable()
export class NotificationData {
 
  constructor(private envService: EnvironmentService) {}


    initializeUserNotificationContent() {
     // console.log('user notification');      
 
// myWizardAPIUrl
      const content = {
        DataSourceUId: '00100000-0020-0000-0000-000000000000',
        TemplateUId: '00200000-0010-0000-0000-000000000000',
        ServiceUrl: 'assets/data/UserNotification.json',
        ActiveLanguage: 'en-US',
        BaseUrl: this.envService.environment.myWizardAPIUrl,
        EndPointUrl: '/v1/UserNotificationMessages',
        Token: sessionStorage.getItem('pheonixToken'),
        appServiceUId : this.envService.environment.AppServiceUID
      };
       return content;
    }


    openDisclaimer()  {
    //  var notifi = new NotificationData();
      var content = this.initializeUserNotificationContent();
      userNotification.init(content);
      console.log(content);
    }

  }