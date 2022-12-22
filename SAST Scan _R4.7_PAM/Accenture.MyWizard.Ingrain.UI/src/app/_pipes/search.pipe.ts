import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'search'
})
export class SearchPipe implements PipeTransform {

  transform(args: string[], value: string, options: Object): any {
    value = value ? value.toLowerCase() : '';

    if (value === '' || value === null || value === undefined) {
      return args;
    }
    const arrayofArgs = Array.from(args);

    const filteredargs = arrayofArgs.filter(arg => {
      if (arg.constructor === String) {
        return arg.toLowerCase().includes(value);
      } else if (arg.constructor === Object && options.hasOwnProperty('filterBy')) {
        return arg[options['filterBy']].toLowerCase().includes(value);
      }
    });
    return filteredargs;
  }
}
