import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiSuccessResponse } from '../shared/api/api-response';
import { UserAdminListPageResponse, UserCreateRequest, UserSearchResponse } from './user.models';

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

  /** Все пользователи для админа приложения; `ids` — только эти (карточка `/users/:id`). */
  getAdminList(options: { includeDeleted?: boolean; ids?: string[] } = {}): Observable<ApiSuccessResponse<UserAdminListPageResponse>> {
    const params: Record<string, string | string[]> = { includeDeleted: String(options.includeDeleted ?? false) };
    if (options.ids?.length) {
      params['ids'] = options.ids;
    }
    return this.http.get<ApiSuccessResponse<UserAdminListPageResponse>>(`${this.baseUrl}/admin`, { params });
  }
}
