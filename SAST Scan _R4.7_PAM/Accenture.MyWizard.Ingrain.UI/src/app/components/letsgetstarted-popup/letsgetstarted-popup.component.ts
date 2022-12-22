import { Component, OnInit } from '@angular/core';
import { DialogRef } from 'src/app/dialog/dialog-ref';
import { DialogConfig } from 'src/app/dialog/dialog-config';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { ApiService } from 'src/app/_services/api.service';

@Component({
  selector: 'app-letsgetstarted-popup',
  templateUrl: './letsgetstarted-popup.component.html',
  styleUrls: ['./letsgetstarted-popup.component.scss']
})
export class LetsgetstartedPopupComponent implements OnInit {

  constructor(private _dialogRef: DialogRef, private _dialogConfig: DialogConfig,
    private _notificationService: NotificationService, private _apiService: ApiService) { }


  submitFlag: boolean;
  showFeatureBranchRedirection = false;
  ngOnInit() {
    if (this._apiService.ingrainappUrl.includes('https://devut-ai-mywizard-si.aiam-dh.com/ingrain')) {
      this.showFeatureBranchRedirection = true;
    }
  }

  onSkip() {
    this._dialogRef.close();
  }

  onConfirm() {
    this.submitFlag = true;
    this._dialogRef.close(this.submitFlag);
    // this._notificationService.success('Your Data will be downloaded shortly');
  }

  onClose() {
    this.submitFlag = false;
    this._dialogRef.close(this.submitFlag);
  }

}
