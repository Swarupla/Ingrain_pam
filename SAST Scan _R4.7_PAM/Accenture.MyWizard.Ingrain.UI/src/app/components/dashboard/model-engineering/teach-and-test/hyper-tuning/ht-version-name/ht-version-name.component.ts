import { Component, OnInit } from '@angular/core';
import { DialogRef } from 'src/app/dialog/dialog-ref';
import { DialogConfig } from 'src/app/dialog/dialog-config';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { HyperTuningService } from 'src/app/_services/hyper-tuning.service';
import { throwError } from 'rxjs';

@Component({
  selector: 'app-ht-version-name.component',
  templateUrl: './ht-version-name.component.html',
  styleUrls: ['./ht-version-name.component.scss']
})
export class HtVersionNameComponent implements OnInit {
  versionName: string;
  title: string;
  constructor(private dr: DialogRef, private dc: DialogConfig, private ns: NotificationService,
    private hts: HyperTuningService) { }

  ngOnInit() {
    this.title = this.dc.data ? (this.dc.data['title'] ? this.dc.data['title'] : 'Save Hyper Tuning Setting') : 'Save Hyper Tuning Setting';
  }

  onClose() {
    this.dr.close();
  }

  onSave(versionName) {

    if (versionName.valid) {

      this.dr.close(this.versionName);
    }
    if (versionName.invalid && versionName.touched) {
      throwError('Enter Model Name');
      this.ns.error('Enter Model Name');
    }
    if (versionName.pristine) {
      throwError('Enter Model Name');
      this.ns.error('Enter Model Name');
    }
  }

}
