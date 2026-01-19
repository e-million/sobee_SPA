import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreateReplyRequest, CreateReviewRequest, ReviewsResponse } from '../models';

/**
 * Review service for product review CRUD actions.
 */
@Injectable({
  providedIn: 'root'
})
export class ReviewService {
  private readonly apiUrl = `${environment.apiUrl}/reviews`;

  constructor(private http: HttpClient) {}

  /**
   * Fetch paginated reviews for a product.
   * @param productId - Product identifier.
   * @param page - Page number (1-based).
   * @param pageSize - Page size.
   * @returns Observable of ReviewsResponse.
   */
  getReviews(productId: number, page = 1, pageSize = 100): Observable<ReviewsResponse> {
    return this.http.get<ReviewsResponse>(`${this.apiUrl}/product/${productId}`, {
      params: {
        page,
        pageSize
      }
    });
  }

  /**
   * Create a new review for a product.
   * @param productId - Product identifier.
   * @param request - Review payload.
   * @returns Observable from the API.
   */
  createReview(productId: number, request: CreateReviewRequest): Observable<{
    message: string;
    reviewId?: number;
    productId?: number;
  }> {
    return this.http.post<{
      message: string;
      reviewId?: number;
      productId?: number;
    }>(`${this.apiUrl}/product/${productId}`, request);
  }

  /**
   * Create a reply to an existing review.
   * @param reviewId - Review identifier.
   * @param request - Reply payload.
   * @returns Observable from the API.
   */
  createReply(reviewId: number, request: CreateReplyRequest): Observable<{
    message: string;
    replyId?: number;
    reviewId?: number;
  }> {
    return this.http.post<{
      message: string;
      replyId?: number;
      reviewId?: number;
    }>(`${this.apiUrl}/${reviewId}/reply`, request);
  }

  /**
   * Delete a review by ID.
   * @param reviewId - Review identifier.
   * @returns Observable from the API.
   */
  deleteReview(reviewId: number): Observable<{ message: string; reviewId?: number }> {
    return this.http.delete<{ message: string; reviewId?: number }>(`${this.apiUrl}/${reviewId}`);
  }

  /**
   * Delete a reply by ID.
   * @param replyId - Reply identifier.
   * @returns Observable from the API.
   */
  deleteReply(replyId: number): Observable<{ message: string; replyId?: number }> {
    return this.http.delete<{ message: string; replyId?: number }>(`${this.apiUrl}/replies/${replyId}`);
  }
}
