import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiSuccessResponse } from '../shared/api/api-response';
import { roleNames } from './user-access.models';

/** Роль приложения `AppAdmin`: выдать — не себе; снять — можно у себя, но не у последнего. */
@Injectable({ providedIn: 'root' })
export class AppRoleApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/appRole';

  grantAppAdmin(userId: string): Observable<ApiSuccessResponse<unknown>> {
    return this.http.post<ApiSuccessResponse<unknown>>(this.baseUrl, { userId, roleName: roleNames.appAdmin });
  }

  revokeAppAdmin(userId: string): Observable<ApiSuccessResponse<unknown>> {
    return this.http.delete<ApiSuccessResponse<unknown>>(this.baseUrl, {
      params: { userId, roleName: roleNames.appAdmin },
    });
  }
}
