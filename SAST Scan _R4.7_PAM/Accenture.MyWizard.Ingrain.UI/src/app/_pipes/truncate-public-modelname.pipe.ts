import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'truncatePublicModelname'
})
export class TruncatePublicModelnamePipe implements PipeTransform {

  transform(value: string): string {
    const str: string = value;
    let s = str.substr(1);
    s = s.substring(0, s.indexOf(','));
    return s;
  }
}
