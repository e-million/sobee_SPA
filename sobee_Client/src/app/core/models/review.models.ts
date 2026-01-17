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

export interface ReviewsResponse {
  productId: number;
  count: number;
  reviews: Review[];
}

export interface CreateReviewRequest {
  rating: number;
  reviewText: string;
}

export interface CreateReplyRequest {
  content: string;
}
