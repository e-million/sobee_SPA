import { HttpParams } from '@angular/common/http';

/**
 * Build HttpParams from a plain object, skipping null/undefined/empty values.
 * @param obj - Source object with query params.
 * @returns HttpParams instance with serialized values.
 */
export function buildHttpParams(obj: Record<string, unknown>): HttpParams {
  return Object.entries(obj).reduce((params, [key, value]) => {
    if (value !== null && value !== undefined && value !== '') {
      return params.set(key, String(value));
    }
    return params;
  }, new HttpParams());
}
