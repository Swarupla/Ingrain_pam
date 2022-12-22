import { Injectable } from '@angular/core';
import { HttpRequest, HttpEventType, HttpEvent } from '@angular/common/http';
import { map, tap, last } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class UploadService {
  http: any;

  constructor() { }

  private getEventMessage(event, file: File) {
    switch (event.type) {
      case HttpEventType.Sent:
        return `Uploading file "${file.name}" of size ${file.size}.`;

      case HttpEventType.UploadProgress:
        // ddnnd
        // Compute and show the % done:
        const percentDone = Math.round(100 * event.loaded / event.total);
        return `File "${file.name}" is ${percentDone}% uploaded.`;

      case HttpEventType.Response:
        return `File "${file.name}" was completely uploaded!`;

      default:
        return `File "${file.name}" surprising upload event: ${event.type}.`;
    }
  }

  upload(url, file) {
    const req = new HttpRequest('POST', url, file, {
      reportProgress: true
    });
    return this.http.request(req).pipe(
      map(event => this.getEventMessage(event, file)),
      last(),

    );
  }
}
