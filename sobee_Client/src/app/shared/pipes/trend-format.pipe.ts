import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'trendFormat',
  pure: true,
  standalone: true
})
export class TrendFormatPipe implements PipeTransform {
  transform(value: number | null | undefined): string {
    if (value === null || value === undefined || Number.isNaN(value)) {
      return '0%';
    }

    const sign = value > 0 ? '+' : '';
    return `${sign}${value.toFixed(2)}%`;
  }
}
