import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiSuccessResponse } from '../shared/api/api-response';
import { AccountLoginRequest, AccountProfile, AccountProfileUpdateRequest, AccountTokenResponse } from './account.models';

export const accountBaseUrl = '/api/account';

export interface AccountConfirmEmailRequest {
  token: string;
}

export interface AccountResendConfirmationRequest {
  email: string;
}

export interface AccountForgotPasswordRequest {
  email: string;
}

export interface AccountResetPasswordRequest {
  token: string;
  password: string;
}

@Injectable({ providedIn: 'root' })
export class AccountApi {
  private readonly http = inject(HttpClient);

  login(request: AccountLoginRequest): Observable<ApiSuccessResponse<AccountTokenResponse>> {
    return this.http.post<ApiSuccessResponse<AccountTokenResponse>>(`${accountBaseUrl}/login`, request);
  }

  /** Refresh-токен браузер отправляет сам — в cookie. */
  refresh(): Observable<ApiSuccessResponse<AccountTokenResponse>> {
    return this.http.post<ApiSuccessResponse<AccountTokenResponse>>(`${accountBaseUrl}/refresh`, null);
  }

  logout(): Observable<ApiSuccessResponse<unknown>> {
    return this.http.post<ApiSuccessResponse<unknown>>(`${accountBaseUrl}/logout`, null);
  }

  getProfile(): Observable<ApiSuccessResponse<AccountProfile>> {
    return this.http.get<ApiSuccessResponse<AccountProfile>>(`${accountBaseUrl}/profile`);
  }

  updateProfile(request: AccountProfileUpdateRequest): Observable<ApiSuccessResponse<AccountProfile>> {
    return this.http.put<ApiSuccessResponse<AccountProfile>>(`${accountBaseUrl}/profile`, request);
  }

  confirmEmail(request: AccountConfirmEmailRequest): Observable<ApiSuccessResponse<{ isConfirmed: boolean }>> {
    return this.http.post<ApiSuccessResponse<{ isConfirmed: boolean }>>(`${accountBaseUrl}/confirm-email`, request);
  }

  resendConfirmation(request: AccountResendConfirmationRequest): Observable<ApiSuccessResponse<{ isSent: boolean }>> {
    return this.http.post<ApiSuccessResponse<{ isSent: boolean }>>(`${accountBaseUrl}/resend-confirmation`, request);
  }

  forgotPassword(request: AccountForgotPasswordRequest): Observable<ApiSuccessResponse<{ isSent: boolean }>> {
    return this.http.post<ApiSuccessResponse<{ isSent: boolean }>>(`${accountBaseUrl}/forgot-password`, request);
  }

  resetPassword(request: AccountResetPasswordRequest): Observable<ApiSuccessResponse<{ isReset: boolean }>> {
    return this.http.post<ApiSuccessResponse<{ isReset: boolean }>>(`${accountBaseUrl}/reset-password`, request);
  }
}
