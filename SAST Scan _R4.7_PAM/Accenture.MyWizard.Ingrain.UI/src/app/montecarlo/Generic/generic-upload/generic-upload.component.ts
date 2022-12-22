import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { UploadApiService } from '../../services/upload-api.service';
import { PayloadHelper } from '../../services/payload-helper.service';
import { AlertService } from '../../services/alert-service.service';
import { LoaderState } from '../../services/loader-state.service';
import { ApiService } from 'src/app/_services/api.service';
import { ApiCore } from '../../services/api-core.service';
import { EnvironmentService } from 'src/app/_services/EnvironmentService';
import { AuthenticationService } from 'src/app/msal-authentication/msal-authentication.service';

@Component({
  selector: 'app-generic-upload',
  templateUrl: './generic-upload.component.html',
  styleUrls: ['./generic-upload.component.scss']
})
export class GenericUploadComponent implements OnInit {
  public template = 'FileUpload';
  public file;
  public target = [];
  public unique = [];
  public selected = {
    'target': '',
    'unique': '',
    'templateId': ''
  };
  oldSelectedTarget;

  constructor(private router: Router, private fileApi: UploadApiService,
    private payloadGenerate: PayloadHelper, private loader: LoaderState, private ingrainApiService: ApiService,
    private apiCore: ApiCore,
    private message: AlertService, private envService: EnvironmentService, private msalAuthentication: AuthenticationService) { }

  ngOnInit() {
    if (this.envService.environment.authProvider.toLowerCase() === 'AzureAD'.toLowerCase()) {
      if (!this.msalAuthentication.msalService.instance.getAllAccounts().length) {
          this.msalAuthentication.login();
      } else {
          this.msalAuthentication.getToken().subscribe(data => {
              if (data) {
                  this.envService.setMsalToken(data);
                  this.template = 'FileUpload';
              }
              });
          }
  } else {
    this.template = 'FileUpload';
  }
}

  private setTemplate(name) {

    const requestPayload = this.payloadGenerate.getFileUploadPayload();
    const fileparams = {
      'file': this.file[0]
    };
    Object.assign(requestPayload, fileparams);

    this.loader.start();
    this.fileApi.uploadFile('GenericFileUpload', this.file, requestPayload).subscribe(
      (data) => {
        if (data.IsUploaded === true) {
          this.loader.stop();
          this.message.success('File data saved.');
          this.template = name;
          this.selected.templateId = data.TemplateId;
          this.target = Object.keys(data.TargetColumnList);
          this.unique = Object.keys(data.UniqueColumnList);
        }
        if (data.IsUploaded === false) {
          this.loader.stop();
          this.message.error(data.ErrorMessage);
          this.template = 'FileUpload';
        }
      },
      (error) => {
        this.loader.stop();
        this.template = 'FileUpload';
        const errorMessage = error?.error ? error?.error : 'Something went wrong.';
        this.message.error(errorMessage);
      }
    );
  }

  private setFileData(data) {
    this.file = data;
  }

  public checkedTarget(name) {
    if (this.oldSelectedTarget !== undefined) {
      if (this.oldSelectedTarget !== name) {
        this.unique.push(this.oldSelectedTarget);
      }
    }
    this.selected.target = name;
    if (this.unique.includes(this.selected.target)) {
      for (let i = 0; i < this.unique.length; i++) {
        if (this.unique[i] === this.selected.target) {
          this.unique.splice(i, 1);
        }
      }
    }
    this.oldSelectedTarget = name;
  }

  public checkedUnique(name) {
    this.selected.unique = name;
  }

  public setTargetAndUnique(template) {
    if (this.selected.target === '' || this.selected.unique === '') {
      this.message.warning('Select target, unique identifier');
    } else if (this.selected.target === this.selected.unique) {
      this.message.warning('Target and Unique ID cannot be same');
    } else {
      this.template = template;
    }
  }

  public createGeneric(name, description) {
    // this.selected.templateId {"Status": true, "Message": "success", "ErrorMessage": null }
    if (this.selected.target === '' || this.selected.unique === '') {
      this.message.warning('Select target, unique identifier');
      return 0;
    } else if (this.selected.target === this.selected.unique) {
      this.message.warning('Target and Unique ID cannot be same');
      return 0;
    }
    if (name.value.length === 0 || description.value.length === 0) {
      this.message.warning('Please enter name and description');
      return 0;
    }
    if (this.apiCore.isSpecialCharacter(name.value + '' + description.value) === 0) {
      return 0;
    }

    const payloadForIsUsecaseExists = this.checkDuplicateUseCase(name);
    this.loader.start();
    this.ingrainApiService.get('IsUseCaseExists', payloadForIsUsecaseExists).subscribe(responseData => {
      if (responseData === false) {

        const params = {
          TemplateID: this.selected.templateId,
          TargetColumn: this.selected.target,
          ProblemType: 'Generic',
          UniqueIdentifier: this.selected.unique,
          UseCaseName: name.value,
          UseCaseDescription: description.value
        };

        this.apiCore.paramData.vdsUsecaseName = name.value;

        // this.loader.start();
        this.ingrainApiService.get('GetGenericUpdate', params).subscribe(
          (data) => {
            if (data.Status) {
              this.message.success('Use case created');
              this.apiCore.paramData.selectInputId = this.selected.templateId;
              this.router.navigate(['generic/Input']);
            } else {
              this.message.error(data.ErrorMessage);
            }
            this.loader.stop();
          },
          (error) => {
            this.loader.stop();
          }
        );
      } else if (responseData) {
        this.loader.stop();
        this.message.error('Usecase name already exists. Choose a different name.');
      }
    }, error => {
      this.loader.stop();
      this.message.error('something went wrong in IsUseCaseExists');
    });
  }

  checkDuplicateUseCase(useCaseName) {
    const requestPayload = {
      UseCaseName: useCaseName.value,
      ClientUID: sessionStorage.getItem('clientID'),
      DCUID: sessionStorage.getItem('dcID')
    };
    return requestPayload;
  }


  remainingCharacter(name) {
    if (name.value.length <= 500) {
      name.value = name.value;
    } else {
      return null;
    }
  }
}
