import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiSuccessResponse } from '../shared/api/api-response';
import { UserCreateRequest, UserSearchResponse } from './user.models';

@Injectable({ providedIn: 'root' })
export class UserApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/user';

  create(request: UserCreateRequest): Observable<ApiSuccessResponse<null>> {
    return this.http.post<ApiSuccessResponse<null>>(this.baseUrl, request);
  }

  search(login: string): Observable<ApiSuccessResponse<UserSearchResponse>> {
    return this.http.get<ApiSuccessResponse<UserSearchResponse>>(`${this.baseUrl}/search`, {
      params: { login },
    });
  }
}
