import { Injectable } from '@angular/core';
import { ApiService } from 'src/app/_services/api.service';
import { NotificationService } from 'src/app/_services/notification-service.service';


@Injectable({
    providedIn: 'root'
})
export class AddFeatureService {

    constructor(private api: ApiService, private ns: NotificationService) {

    }

    PostAddFeature(data) {
        return this.api.post('PostAddFeatures', data);
    }

    shiftingOperatorData(data) {
        const totalOperation = Object.keys(data);
        if (totalOperation.includes('value_check')) {
            totalOperation.splice(totalOperation.length - 2, 2);
        }
        for (let operation = 0; operation < totalOperation.length - 1; operation++) {
            if (data[operation].hasOwnProperty('Operator') && Object.keys(data[operation]).length === 1) {
                delete data[operation];
            } else {
                // if ( operation === totalOperation.length - 1) {
                if (data[operation].hasOwnProperty('Operator') && !this.operatorTypeIsComparing(data[operation]['Operator'])) {
                    data[operation]['Operator'] = data[operation + 1]['Operator'];
                    // delete data[operation]['Operator'];
                } else {
                    data[operation]['Operator'] = data[operation + 1]['Operator'];
                }
            }
        }

        let i = 0;
        const modifiedData = {};
        for (const key in data) {
            if (data[key] && key !== 'value_check' && key !== 'value') {
                modifiedData[i++] = data[key];
            } else {
                if (key === 'value_check') { modifiedData[key] = data[key].toString(); }
                if (key === 'value') { modifiedData[key] = data[key]; }
            }
        }
        return JSON.stringify(modifiedData);
    }


    verifyRequiredFields(formData) {
        const data = formData;
        const featurename = Object.keys(data)[0];
        const operationData = formData[featurename];
        const numberOfOperation = Object.keys(operationData);
        let singleOperation = {};
        for (let i = 0; i < numberOfOperation.length; i++) {
            singleOperation = operationData[i];
            if (singleOperation['OperationType'] === '') {
                this.ns.error('Please select required fields');
                return 0;
            }
            if (singleOperation['columnDropdown'] === '') {
                this.ns.error('Please select required fields');
                return 0;
            }


        }
        return 1;
    }

    // To check operator is other than (Multiply, Divide , Add , Substract)
    operatorTypeIsComparing(value: string) {
        return (value !== 'multiply' && value !== 'divide' && value !== 'add' && value !== 'subtract'
            && value !== 'Business Days Between' && value !== 'Months Between' && value !== 'Years Between' && value !== 'Hours Between'
            && value !== 'Minutes Between' && value !== 'Seconds Between' && value !== 'days Between'
        );
    }


    public IsJsonString(str) {
        try {
            JSON.parse(str);
        } catch (e) {
            return false;
        }
        return true;
    }


    public isTextEnteredOrNot(data): boolean {

        if ((data['OperationType'] === 'replace' || data['OperationType'] === 'substring') ) {
            if ( !(data.hasOwnProperty('Value') && data.hasOwnProperty('Value2') )) {
            return true;
            }
        } else {
            if ( !(data.hasOwnProperty('Value'))) {
                return true;
             }
        }

        const value = (data['OperationType'] === 'replace' || data['OperationType'] === 'substring') ?
            ( data['Value'] === '') || ( data['Value2'] === '') :
            ( data['Value'] === '');
        return value;
    }

    public isTextEnteredOrNotSubstring(data): boolean {
        if (( data['OperationType'] === 'substring') ) {
            if ( !(data.hasOwnProperty('Value') && (data.hasOwnProperty('Value2') ))) {
            return true;
            }
        }

        if (( data['OperationType'] === 'substring') ) {
        const value = 
            (( data['Value'] === '') || data['Value2'] === '')
            ||  data['Value'] < 0 || data['Value2'] < 0 ||  ( data['Value'] >= data['Value2']);
        return value;
        } else {
        return false;    
        }
    }
 
    public combineOperationSet() {
        
    }
}
