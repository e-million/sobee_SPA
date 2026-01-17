import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreateReplyRequest, CreateReviewRequest, ReviewsResponse } from '../models';

@Injectable({
  providedIn: 'root'
})
export class ReviewService {
  private readonly apiUrl = `${environment.apiUrl}/reviews`;

  constructor(private http: HttpClient) {}

  getReviews(productId: number): Observable<ReviewsResponse> {
    return this.http.get<ReviewsResponse>(`${this.apiUrl}/product/${productId}`);
  }

  createReview(productId: number, request: CreateReviewRequest): Observable<unknown> {
    return this.http.post(`${this.apiUrl}/product/${productId}`, request);
  }

  createReply(reviewId: number, request: CreateReplyRequest): Observable<unknown> {
    return this.http.post(`${this.apiUrl}/${reviewId}/reply`, request);
  }

  deleteReview(reviewId: number): Observable<unknown> {
    return this.http.delete(`${this.apiUrl}/${reviewId}`);
  }

  deleteReply(replyId: number): Observable<unknown> {
    return this.http.delete(`${this.apiUrl}/replies/${replyId}`);
  }
}
