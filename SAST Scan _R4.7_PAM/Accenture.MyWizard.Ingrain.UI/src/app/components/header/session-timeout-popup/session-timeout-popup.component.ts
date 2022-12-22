import { Component, OnInit, Inject } from '@angular/core';
/* import { DialogRef } from 'src/app/dialog/dialog-ref';
import { DialogConfig } from 'src/app/dialog/dialog-config'; */
import { NotificationService } from 'src/app/_services/notification-service.service';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';

@Component({
  selector: 'app-session-timeout-popup',
  templateUrl: './session-timeout-popup.component.html',
  styleUrls: ['./session-timeout-popup.component.scss']
})
export class SessionTimeoutPopupComponent implements OnInit {

  isContinueDisabled = false;

  constructor(private ns: NotificationService,
    public dialogRef: MatDialogRef<SessionTimeoutPopupComponent>, @Inject(MAT_DIALOG_DATA) public data: any) { }

  ngOnInit() {
    /* if (this.dc.hasOwnProperty('data')) {
      if (this.dc.data.hasOwnProperty('disableContinue')) {
        if (this.dc.data.disableContinue === true) {
          this.isContinueDisabled = true;
        } else {
          this.isContinueDisabled = false;
        }
      }
    } */
    if (this.data) {
      if (this.data.hasOwnProperty('disableContinue')) {
        if (this.data.disableContinue === true) {
          this.isContinueDisabled = true;
        } else {
          this.isContinueDisabled = false;
        }
      }
    }
    console.log(this.data);
  }

  onClose() {
    this.dialogRef.close('closed');
  }

  onContinue() {
    this.dialogRef.close('continue');
  }

  signInUser() {
    this.dialogRef.close('signin');
  }

}
