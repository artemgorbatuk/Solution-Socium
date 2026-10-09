import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiSuccessResponse } from '../shared/api/api-response';
import { UserCreateRequest, UserListPageResponse } from './user.models';

@Injectable({ providedIn: 'root' })
export class UserApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/user';

  getList(): Observable<ApiSuccessResponse<UserListPageResponse>> {
    return this.http.get<ApiSuccessResponse<UserListPageResponse>>(this.baseUrl);
  }

  create(request: UserCreateRequest): Observable<ApiSuccessResponse<null>> {
    return this.http.post<ApiSuccessResponse<null>>(this.baseUrl, request);
  }
}
