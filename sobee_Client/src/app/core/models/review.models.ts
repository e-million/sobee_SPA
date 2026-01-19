// Review Models

export interface ReviewReply {
  replyId: number;
  reviewId: number;
  content: string;
  created: string;
  userId: string | null;
}

export interface Review {
  reviewId: number;
  productId: number;
  rating: number;
  reviewText: string;
  created: string;
  userId: string | null;
  sessionId: string | null;
  replies: ReviewReply[];
}

export interface ReviewSummaryResponse {
  total: number;
  average: number;
  counts: number[];
}

export interface ReviewsResponse {
  productId: number;
  count: number;
  reviews: Review[];
  page?: number;
  pageSize?: number;
  totalCount?: number;
  summary?: ReviewSummaryResponse;
}

export interface CreateReviewRequest {
  rating: number;
  reviewText: string;
}

export interface CreateReplyRequest {
  content: string;
}
