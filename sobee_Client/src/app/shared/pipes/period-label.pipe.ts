import { Pipe, PipeTransform } from '@angular/core';

type PeriodGranularity = 'day' | 'week' | 'month';

@Pipe({
  name: 'periodLabel',
  pure: true,
  standalone: true
})
export class PeriodLabelPipe implements PipeTransform {
  transform(value: string | null | undefined, granularity: PeriodGranularity | null | undefined): string {
    if (!value) {
      return '';
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return '';
    }

    const mode = granularity ?? 'day';
    if (mode === 'month') {
      return date.toLocaleDateString('en-US', { month: 'short', year: 'numeric' });
    }

    if (mode === 'week') {
      return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
    }

    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
  }
}
