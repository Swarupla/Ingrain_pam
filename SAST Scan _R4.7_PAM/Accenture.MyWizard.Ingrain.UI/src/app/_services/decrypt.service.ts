import { Injectable } from '@angular/core';
import * as aesjs from 'aes-js';
import { ApiService } from './api.service';

@Injectable({
  providedIn: 'root'
})
export class DecryptService {

  constructor(private api: ApiService) { }

  decrypt(encodedtext: string) {

    const keystring = '2k88uYguOpY0UthpU4dhtD1PQvQn45jCzeOjbYIxiso=';
    const vectorstring = 'ptBAVIsxNYyHZk3CRai8Yg==';
    const tokenstring = encodedtext;

    const base64decodedkey = window.atob(keystring);
    const base64decodedvector = window.atob(vectorstring);
    const base64decodedtoken = window.atob(tokenstring);


    const key = this.str2ab(base64decodedkey);
    const iv = this.str2ab(base64decodedvector);
    const token = this.str2ab(base64decodedtoken);

    const aesCbc = new aesjs.ModeOfOperation.cbc(key, iv);
    const decryptedBytes = aesCbc.decrypt(token);

    // Convert our bytes back into text
    const decryptedText = aesjs.utils.utf8.fromBytes(decryptedBytes);
    console.log(decryptedText);

    return decryptedText;
  }


  str2ab(str) {
    const buf = new ArrayBuffer(str.length);
    const bufView = new Uint8Array(buf);
    for (let i = 0, strLen = str.length; i < strLen; i++) {
      bufView[i] = str.charCodeAt(i);
    }
    return bufView;
  }

  marketPlaceFileEncrypt(fileDetails, filePath) {
    const frmData = new FormData();

    // frmData.append('excelfile', fileDetails[0].name);
    if (fileDetails.fileUpload.hasOwnProperty('filepath')) {
      const files = fileDetails.fileUpload.filepath;
      if (files.length > 0) {
        for (let j = 0; j < files.length; j++) {
          frmData.append('excelfile', files[j]);
        }
      }
    }

    return this.api.post('EncryptMarketplaceFile', frmData, filePath); // artefacts EncryptMarketplaceFile
  }

  marketPlaceFileDecrypt(filepath: any) {
    return this.api.get('DecryptMarketplaceFile', { filepath: filepath });
  }

}
