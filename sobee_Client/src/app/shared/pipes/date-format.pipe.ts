import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'dateFormat',
  pure: true,
  standalone: true
})
export class DateFormatPipe implements PipeTransform {
  transform(value: string | null | undefined, options?: Intl.DateTimeFormatOptions): string {
    if (!value) {
      return '';
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return '';
    }

    const formatOptions = options ?? { month: 'short', day: 'numeric' };
    return date.toLocaleDateString('en-US', formatOptions);
  }
}
