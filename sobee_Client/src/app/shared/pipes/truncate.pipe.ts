import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'truncate',
  pure: true,
  standalone: true
})
export class TruncatePipe implements PipeTransform {
  transform(value: string | null | undefined, maxLength: number = 120): string {
    if (!value) {
      return '';
    }

    if (value.length <= maxLength) {
      return value;
    }

    return `${value.slice(0, maxLength).trim()}...`;
  }
}
