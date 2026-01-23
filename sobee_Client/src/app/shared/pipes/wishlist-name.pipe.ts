import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'wishlistName',
  pure: true,
  standalone: true
})
export class WishlistNamePipe implements PipeTransform {
  transform(value: string | null | undefined, fallback: string = 'Unnamed product'): string {
    const trimmed = value?.trim();
    return trimmed ? trimmed : fallback;
  }
}
