import { Injectable } from '@angular/core';
import { ToastrService } from 'ngx-toastr';

@Injectable({
  providedIn: 'root'
})
export class AlertService {

  constructor(private ts: ToastrService) { }

  warning(warnMessage, title?) {
  this.ts.warning(warnMessage, title, { onActivateTick: true });
  }

  success(successMessage, title?) {
    this.ts.success(successMessage, title, { onActivateTick: true });
  }

  error(errorMessage, title?) {
    this.ts.error(errorMessage, title, { onActivateTick: true });
  }

  successWithHtml(html, title?) {
    this.ts.success(html, title, {
      enableHtml: true,
      onActivateTick: true
    });
  }

  errorWithHtml(html, title?) {
    this.ts.error(html, title, {
      enableHtml: true,
      onActivateTick: true
    });
  }

  successWithTimeout(successMessage, title?, timespan?) {
    this.ts.success(successMessage, title, {
      timeOut: timespan,
      onActivateTick: true
    });
  }

  errorWithTimeout(errorMessage, title?, timespan?) {
    this.ts.error(errorMessage, title, {
      timeOut: timespan,
      onActivateTick: true
    });
  }
}
