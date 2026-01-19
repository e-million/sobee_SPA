import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { UpdatePasswordRequest, UserProfile } from '../models';

/**
 * User profile service for account details and password changes.
 */
@Injectable({
  providedIn: 'root'
})
export class UserService {
  private readonly apiUrl = `${environment.apiUrl}/users`;

  constructor(private http: HttpClient) {}

  /**
   * Fetch the current user's profile.
   * @returns Observable of UserProfile.
   */
  getProfile(): Observable<UserProfile> {
    return this.http.get<UserProfile>(`${this.apiUrl}/profile`);
  }

  /**
   * Update the current user's profile.
   * @param request - Updated profile data.
   * @returns Observable of UserProfile.
   */
  updateProfile(request: UserProfile): Observable<UserProfile> {
    return this.http.put<UserProfile>(`${this.apiUrl}/profile`, request);
  }

  /**
   * Update the current user's password.
   * @param request - Password change payload.
   * @returns Observable containing a success flag.
   */
  changePassword(request: UpdatePasswordRequest): Observable<{ success: boolean }> {
    return this.http.put<{ success: boolean }>(`${this.apiUrl}/password`, request);
  }
}
