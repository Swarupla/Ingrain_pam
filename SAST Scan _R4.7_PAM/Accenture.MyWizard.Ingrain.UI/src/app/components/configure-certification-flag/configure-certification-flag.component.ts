import { Component, OnInit } from '@angular/core';
import { ApiService } from 'src/app/_services/api.service';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { ProblemStatementService } from 'src/app/_services/problem-statement.service';

@Component({
  selector: 'app-configure-certification-flag',
  templateUrl: './configure-certification-flag.component.html',
  styleUrls: ['./configure-certification-flag.component.css']
})
export class ConfigureCertificationFlagComponent implements OnInit {

  checkFlag: boolean;

  constructor(private _apiService: ApiService, private _problemStatementService: ProblemStatementService,
    private _notificationService: NotificationService) { }

  ngOnInit() {
  }

  submit(providedFlag) {
    this.checkFlag = providedFlag.toLowerCase();
    this._problemStatementService.insertCertificationFlag(this.checkFlag).subscribe(data => {
      if (data) {
        if (data === 'success') {
          this._notificationService.success('Flag configured Successfully');
        }
      }
    }, error => {
      this._notificationService.error('There is come backend error');
    });
  }
}
