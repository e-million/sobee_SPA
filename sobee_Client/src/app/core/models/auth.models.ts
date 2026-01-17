// Authentication Models

export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  billingAddress: string;
  shippingAddress: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  tokenType: string;
  accessToken: string;
  expiresIn: number;
  refreshToken: string;
}

export interface RegisterResponse {
  message?: string;
  email?: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  token: string;
  newPassword: string;
}

export interface UpdatePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface UserProfile {
  email: string;
  firstName: string;
  lastName: string;
  billingAddress: string;
  shippingAddress: string;
}
