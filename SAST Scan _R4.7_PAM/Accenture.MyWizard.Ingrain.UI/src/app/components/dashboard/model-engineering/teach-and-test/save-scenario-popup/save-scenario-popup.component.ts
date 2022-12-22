import { Component, OnInit } from '@angular/core';
import { DialogRef } from 'src/app/dialog/dialog-ref';
import { DialogConfig } from 'src/app/dialog/dialog-config';
import { NotificationService } from 'src/app/_services/notification-service.service';

@Component({
  selector: 'app-save-scenario-popup',
  templateUrl: './save-scenario-popup.component.html',
  styleUrls: ['./save-scenario-popup.component.scss']
})
export class SaveScenarioPopupComponent implements OnInit {

  testScenarioName: string;
  constructor(private dr: DialogRef, private dc: DialogConfig, private ns: NotificationService) { }

  ngOnInit() {
  }

  onClose() {
    this.dr.close();
  }

  onEnter(testScenarioName) {

    if (testScenarioName.valid) {
      this.dr.close(this.testScenarioName);
    }
    if (testScenarioName.invalid && testScenarioName.touched) {
      this.ns.error('Enter Test Scenario Name');
    }
  }
}
