import { Component, OnInit, NgZone } from '@angular/core';
import { Router } from '@angular/router';
import { AuthenticationService } from 'src/app/msal-authentication/msal-authentication.service';
import { EnvironmentService } from 'src/app/_services/EnvironmentService';



@Component({
  selector: 'login',
  template: `
    <h4 class="modal-title">Authorization denied.</h4>
    <p>You are not authorized to access this application. Please contact your administrator.</p>`,
})

export class LoginComponent implements OnInit {
  model: any = {};
  loading = false;
  returnUrl: string;
  hasChild: string;
  loginFailure = false;
  errorMessage: string;
  closeResult: string;
  logoutUrl: string;
  idleHandler: any;
  constructor(private router: Router,
    private environmentService: EnvironmentService,
    private authService: AuthenticationService,
    private ngZone: NgZone,) {

  }

  async ngOnInit(): Promise<void> {

    if (this.environmentService.environment.authProvider === 'AzureADAuthProvider') {
      if (this.authService.getAccount() === null) {
        console.log('LoginComponent is not  authenticated. (msal2..ng12)');
        await this.authService.login();
      } else {
        console.log('LoginComponent is authenticated true. (msal2..ng12)');
        this.ngZone.run((): void => {
          void this.router.navigate['Signout']
        });

      }
    }

  }
}
