import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'starRating',
  pure: true,
  standalone: true
})
export class StarRatingPipe implements PipeTransform {
  /**
   * Convert a numeric rating into a 5-star array for templates.
   * @param rating - Rating value (0-5).
   * @returns Array of 1/0 values representing filled stars.
   */
  transform(rating: number | null | undefined): number[] {
    const starRating = rating || 0;
    return Array(5).fill(0).map((_, i) => i < Math.round(starRating) ? 1 : 0);
  }
}
