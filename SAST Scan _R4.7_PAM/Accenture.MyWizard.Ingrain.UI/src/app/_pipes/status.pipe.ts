import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'status'
})
export class StatusPipe implements PipeTransform {

  transform(value: any, args?: any): any {
    if(value == 'C')
    return 'Completed';
    else if(value == 'P')
    return 'In progress';
    else if(value == 'E')
    return 'Error';
    else if(value == 'I')
    return 'Training Not Initiated';
    else if(value == 'null')
    return 'In progress';
  }

}
