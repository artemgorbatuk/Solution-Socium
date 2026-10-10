import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiSuccessResponse } from '../shared/api/api-response';
import { AccountLoginRequest, AccountProfile, AccountProfileUpdateRequest, AccountTokenResponse } from './account.models';

export const accountBaseUrl = '/api/account';

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

  updateProfile(request: AccountProfileUpdateRequest): Observable<ApiSuccessResponse<AccountProfile>> {
    return this.http.put<ApiSuccessResponse<AccountProfile>>(`${accountBaseUrl}/profile`, request);
  }
}
