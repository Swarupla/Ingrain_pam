import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import * as FileSaver from 'file-saver';
import { map } from 'rxjs/operators';
import * as XLSX from 'xlsx';
import { ApiService } from './api.service';
import { NotificationService } from './notification-service.service';

const EXCEL_TYPE = 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet;charset=UTF-8';
const EXCEL_EXTENSION = '.xlsx';
const TEXT_EXTENSION = '.txt';

@Injectable({
    providedIn: 'root'
})
export class ExcelService {

    constructor(private api: ApiService, private http: HttpClient, private notificationService: NotificationService) { }

    public exportAsExcelFileWithSheets(json: any[], excelFileName: string) {
        const Sheets = {};
        const sheetNames = [];
        for (const key in json) {
            if (json.hasOwnProperty(key)) {
                if (json[key].length > 0) {
                    sheetNames.push(key);
                }
            }
        }

        sheetNames.forEach(sheet => {
            const worksheet: XLSX.WorkSheet = XLSX.utils.json_to_sheet(json[sheet]);
            Sheets[sheet] = worksheet;
        });

        const workbook: XLSX.WorkBook = { Sheets: Sheets, SheetNames: sheetNames };
        const excelBuffer: any = XLSX.write(workbook, { bookType: 'xlsx', type: 'array' });
        this.saveAsExcelFile(excelBuffer, excelFileName);
    }

    public exportAsPasswordProtectedExcelFileWithSheets(json: any[], excelFileName: string) {
        const Sheets = {};
        const sheetNames = [];
        for (const key in json) {
            if (json.hasOwnProperty(key)) {
                if (json[key].length > 0) {
                    sheetNames.push(key);
                }
            }
        }

        sheetNames.forEach(sheet => {
            const worksheet: XLSX.WorkSheet = XLSX.utils.json_to_sheet(json[sheet]);
            Sheets[sheet] = worksheet;
        });

        const workbook: XLSX.WorkBook = { Sheets: Sheets, SheetNames: sheetNames };
        const excelBuffer: any = XLSX.write(workbook, { bookType: 'xlsx', type: 'array' });
        return this.saveAsPasswordProtectedExcelFile(excelBuffer, excelFileName);
    }

    public exportAsExcelFile(json: any[], excelFileName: string) {
        const worksheet: XLSX.WorkSheet = XLSX.utils.json_to_sheet(json);
        const workbook: XLSX.WorkBook = { Sheets: { 'data': worksheet }, SheetNames: ['data'] };
        const excelBuffer: any = XLSX.write(workbook, { bookType: 'xlsx', type: 'array' });
        return this.saveAsExcelFile(excelBuffer, excelFileName);
    }

    public exportAsPasswordProtectedExcelFile(json: any[], excelFileName: string) {
        const worksheet: XLSX.WorkSheet = XLSX.utils.json_to_sheet(json);
        const workbook: XLSX.WorkBook = { Sheets: { 'data': worksheet }, SheetNames: ['data'] };
        const excelBuffer: any = XLSX.write(workbook, { bookType: 'xlsx', type: 'array' });
        return this.saveAsPasswordProtectedExcelFile(excelBuffer, excelFileName);
    }

    private saveAsPasswordProtectedExcelFile(buffer: any, fileName: string) {
        const data: Blob = new Blob([buffer], { type: EXCEL_TYPE });
        // FileSaver.saveAs(data, fileName + '_export_' + new Date().getTime() + '.xlsx');
        return this.downloadPasswordProtectedZIP(data, fileName);
    }

    private saveAsExcelFile(buffer: any, fileName: string) {
        const data: Blob = new Blob([buffer], { type: EXCEL_TYPE });
        FileSaver.saveAs(data, fileName + '_export_' + new Date().getTime() + '.xlsx');
        this.notificationService.success('File Downloaded Succesfully');
        //return this.downloadPasswordProtectedZIP(data, fileName);
    }

    private downloadPasswordProtectedZIP(buffer, fileName) {
        let httpheaders: HttpHeaders = new HttpHeaders();
        httpheaders = httpheaders.append('Content-Type', 'application/json');
        const options = <any>{
            responseType: 'arraybuffer',
            headers: httpheaders
        };
        const URL = this.api.phoenixApiBaseURL + '/v1/GetNonPdfDocByByteArrayForSingleUser?fileName=' + encodeURIComponent(fileName + EXCEL_EXTENSION) + '&clientUId=' + sessionStorage.getItem('clientID') + '&deliveryConstructUId=' + sessionStorage.getItem('dcID');
        return this.http.post(URL, buffer, options).pipe(map((response) => {
            return response;
        }));
    }

    public downloadPasswordProtectedZIPJSON(buffer, fileName) {
        let httpheaders: HttpHeaders = new HttpHeaders();
        httpheaders = httpheaders.append('Content-Type', 'application/json');
        const options = <any>{
            responseType: 'arraybuffer',
            headers: httpheaders
        };
        const URL = this.api.phoenixApiBaseURL + '/v1/GetNonPdfDocByByteArrayForSingleUser?fileName=' + encodeURIComponent(fileName) + '&clientUId=' + sessionStorage.getItem('clientID') + '&deliveryConstructUId=' + sessionStorage.getItem('dcID');
        return this.http.post(URL, buffer, options).pipe(map((response) => {
            return response;
        }));
    }

    public downloadZIPJSON(buffer, fileName) {
        const data: Blob = new Blob([buffer], { type: TEXT_EXTENSION });
        FileSaver.saveAs(data, fileName);
        this.notificationService.success('File Downloaded Succesfully');
    }
}
