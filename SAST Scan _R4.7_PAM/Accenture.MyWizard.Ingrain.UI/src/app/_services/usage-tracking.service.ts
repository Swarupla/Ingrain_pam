import { Injectable } from '@angular/core';
import { ApiService } from './api.service';

@Injectable({
    providedIn: 'root'
})
export class UsageTrackingService {

    constructor(private api: ApiService) { }

    usageTracking(features, subFeatures) {
        this.usageTrackingService(features, subFeatures).subscribe(data => { }, error => { });
    }

    usageTrackingService(features, subFeatures) {
        const uniqueId = sessionStorage.getItem('uniqueId');
        const clientId = sessionStorage.getItem('clientID');
        const userId = sessionStorage.getItem('userId');
        const dcId = sessionStorage.getItem('dcID');
        const dcName = sessionStorage.getItem('dcName');
        const screenResolution = window.screen.width * window.devicePixelRatio + 'x' + window.screen.height * window.devicePixelRatio;
        const nAgt = navigator.userAgent;
        let browserName = navigator.appName;
        let nameOffset, verOffset;

        let e2eid = "";
        let environment = "";
        if (sessionStorage.getItem('Environment') === 'FDS') {
            e2eid = sessionStorage.getItem('End2EndId');
            environment = sessionStorage.getItem('Environment');
        }

        // In Opera, the true version is after "Opera" or after "Version"
        if ((verOffset = nAgt.indexOf('Opera')) !== -1) {
            browserName = 'Opera';
        } else if ((verOffset = nAgt.indexOf('MSIE')) !== -1) { // In MSIE, the true version is after "MSIE" in userAgent
            browserName = 'Microsoft Internet Explorer';
        } else if ((verOffset = nAgt.indexOf('Chrome')) !== -1) { // In Chrome, the true version is after "Chrome"
            browserName = 'Chrome';
        } else if ((verOffset = nAgt.indexOf('Safari')) !== -1) { // In Safari, the true version is after "Safari" or after "Version"
            browserName = 'Safari';
        } else if ((verOffset = nAgt.indexOf('Firefox')) !== -1) { // In Firefox, the true version is after "Firefox"
            browserName = 'Firefox';
        } else if ((nameOffset = nAgt.lastIndexOf(' ') + 1) < (verOffset = nAgt.lastIndexOf('/'))) {
            // In most other browsers, "name/version" is at the end of userAgent
            browserName = nAgt.substring(nameOffset, verOffset);
            if (browserName.toLowerCase() === browserName.toUpperCase()) {
                browserName = navigator.appName;
            }
        }
        const data = {
            'UserUniqueId': uniqueId,
            'clientID': clientId,
            'userId': userId,
            'dcID': dcId,
            'dcName': dcName,
            'End2EndId': e2eid,
            'features': features,
            'subFeatures': subFeatures,
            'ApplicationURL': window.location.href,
            'IPAddress': null,
            'Browser': browserName,
            'ScreenResolution': screenResolution,
            'Environment': (sessionStorage.getItem('Environment') != null) ? sessionStorage.getItem('Environment') : 'PAD'
        };
        return this.api.post('UsageTracking', data);
    }
}
