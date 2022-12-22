import { Injectable } from '@angular/core';
import { throwError } from 'rxjs';
import { ApiService } from 'src/app/_services/api.service';

@Injectable({
  providedIn: 'root'
})
export class UploadApiService {

  constructor(private ingrainApiService: ApiService) {

  }

  public uploadFile(url, fileData, params) {

    const frmData = new FormData();
    /* for (let i = 0; i < fileData.length; i++) {
      frmData.append('excelfile', fileData[i]);
    } */
    Object.entries(params).forEach(
      ([key, value]) => {
        if (key !== 'file') {
            frmData.append(key, params[key]);
        } else {
          frmData.append(key, fileData[0]);
        }
      }
    );
    if (fileData?.length) {
      const pattern = /[/\\?%*:|"<>]/g;
      const specialCharaterCheck = pattern.test(fileData[0].name);
      const fileExtensionCheck = fileData[0].name.split('.');
      if (specialCharaterCheck || fileExtensionCheck.length > 2) {
        return throwError({error: 'Invalid file name'});
      }
    }
    return this.ingrainApiService.post(url, frmData);

  }
}
