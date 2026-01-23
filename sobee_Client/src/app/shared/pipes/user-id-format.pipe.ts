import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'userIdFormat',
  pure: true,
  standalone: true
})
export class UserIdFormatPipe implements PipeTransform {
  transform(value: string | null | undefined, fallback: string = 'User'): string {
    if (!value) {
      return fallback;
    }

    return value.length > 8 ? `${value.slice(0, 8)}...` : value;
  }
}
