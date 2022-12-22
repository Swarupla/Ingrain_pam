import { Component, OnInit, ElementRef, Output, EventEmitter, AfterViewInit } from '@angular/core';
import { Validators, FormBuilder, FormGroup } from '@angular/forms';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { DialogRef } from 'src/app/dialog/dialog-ref';
import { DialogConfig } from 'src/app/dialog/dialog-config';
import { ProblemStatementService } from '../../_services/problem-statement.service';
import { CoreUtilsService } from '../../_services/core-utils.service';
import { throwError, of, timer, EMPTY } from 'rxjs';

@Component({
  selector: 'app-utility-upload',
  templateUrl: './utility-upload.component.html',
  styleUrls: ['./utility-upload.component.scss']
})
export class UtilityUploadComponent implements OnInit {
  sourceTotalLength: number;
  allowedExtensions = ['.xlsx', '.csv'];
  files = [];
  uploadFilesCount = 0;
  deliveryTypeName;
  clientUId;
  datasetName='';
  IsPrivate: boolean = true;
  PIConfirmation;

  constructor(private ns: NotificationService, private formBuilder: FormBuilder,
    private coreUtilsService: CoreUtilsService,
    public dialog: DialogRef, private dialogConfig: DialogConfig, private problemStatementService: ProblemStatementService) { }

  ngOnInit() {
    if (this.dialogConfig.hasOwnProperty('data')){
      this.deliveryTypeName = this.dialogConfig.data.deliveryTypeName;
      this.clientUId = this.dialogConfig.data.clientUID;
    }
  }

  getFileDetails(e) {   
    let validFileExtensionFlag = true;
    let validFileNameFlag = true;
    let validFileSize = true;
    const resourceCount = e.target.files.length;
          const files = e.target.files;
      let fileSize = 0;
     // for (let i = 0; i < e.target.files.length; i++) {
        const fileName = files[0].name;
        const dots = fileName.split('.');
        const fileType = '.' + dots[dots.length - 1];
        if (!fileName) {
          validFileNameFlag = false;
        }
        if (this.allowedExtensions.indexOf(fileType) !== -1) {
          fileSize = fileSize + e.target.files[0].size;
          const index = this.files.findIndex(x => (x.name === e.target.files[0].name));
          validFileNameFlag = true;
          validFileExtensionFlag = true;
          if (index < 0) {
            this.files = [];
              this.files.push(e.target.files[0]);
            }
            this.uploadFilesCount = this.files.length;
        } else {
          validFileExtensionFlag = false;
        }
    //  }
    
        validFileSize = true;
     
      if (validFileNameFlag === false) {
         this.ns.error('Kindly upload a file with valid name.');
      }
      if (validFileExtensionFlag === false) {
        this.ns.error('Kindly upload .xlsx or .csv file.');
      }
   
      this.sourceTotalLength = this.files.length;
  }

  onSubmit(DatasetName) {
    const valid = this.coreUtilsService.isSpecialCharacter(this.datasetName);
    if (valid === 0) {
      return 0;
     } else {
      this.datasetName = DatasetName.model;
      if(DatasetName.valid) {
         if (this.sourceTotalLength >= 1) {
        const filePayload = { 'fileUpload': {} };
        if (this.PIConfirmation === undefined) {
           return this.ns.error('Kindly select the PII data confirmation options.');
        }
        const payload = {
          'source': undefined, 'sourceTotalLength': this.sourceTotalLength,
          'category': 'null',
          'datasetname': DatasetName.model,
          'IsPrivate' : this.IsPrivate,
          'Encryption' : this.PIConfirmation
        };
        const finalPayload = [];
        
        if (this.files.length > 0) { // Payload for File
          payload.source = 'File';  
          filePayload.fileUpload['filepath'] = this.files;         
        }               
          payload.sourceTotalLength = 1;
          finalPayload.push(payload);      
          finalPayload.push(filePayload);          
          this.dialog.close(finalPayload);
        }  else {
          this.ns.error('Please upload the file');
        }
      }
      if (DatasetName.invalid && DatasetName.touched) {
        throwError('Enter Dataset Name');
        this.ns.error('Enter Dataset Name');
      }
      if (DatasetName.pristine) {
        throwError('Enter Dataset Name');
        this.ns.error('Enter Dataset Name');
      }
    }
  }

  onPIConfirmation(optionValue) {
    this.PIConfirmation = optionValue;
    if (this.PIConfirmation) {
      this.ns.success('Personal Identification Information is available in the data provided for analysis. Data will be encrypted and stored by the platform.')
    }
  }

  onClose() {
    this.dialog.close();
  }

  getpublicflag() {
    this.IsPrivate = false;
}

getprivateflag() {
    this.IsPrivate = true;
}

  public removeFile(fileAtIndex: number, fileName: string, fileInput) {
    this.files.splice(fileAtIndex, 1);   
    this.sourceTotalLength = this.files.length;
    fileInput.value = '';
  }

  // onSwitchChanged(elementRef) {
  //   if (elementRef.checked === true) {
  //      this.IsPrivate = true;
  //   }
  //   if (elementRef.checked === false) {
  //     this.IsPrivate = false;
  //   }
  // }


}
