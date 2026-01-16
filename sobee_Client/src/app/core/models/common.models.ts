// Common Models

export interface ApiErrorResponse {
  error: string;
  code?: string;
  details?: any;
}

export interface MessageResponse {
  message: string;
}

export interface GuestSession {
  sessionId: string;
  sessionSecret: string;
}
