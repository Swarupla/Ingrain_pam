import { Injectable } from '@angular/core';
import { ValidatorFn, AbstractControl } from '@angular/forms';
import { FormGroup } from '@angular/forms';

@Injectable({
  providedIn: 'root'
})
export class SimulationValidatorsService {

  constructor() { }

  decimalValidator(): ValidatorFn {
    return (control: AbstractControl): { [key: string]: any } => {
      if (!control.value) {
        return null;
      }
      // const regex = new RegExp(/^-?(0|[1-9]\d*)?(\.\d+)?(?<=\d)$/);
      const valid = Number(control.value);
      return valid ? null : { invalidInput: true };
    };
  }
}
