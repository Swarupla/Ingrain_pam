import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'objectToArray'
})
export class ObjectToArrayPipe implements PipeTransform {

  transform(value: any, args?: any): any {
    const keys = [];
    // tslint:disable-next-line: forin
    for (const key in value) {
      keys.push({ key: key, value: value[key] });
    }
    return keys;
  }
}
