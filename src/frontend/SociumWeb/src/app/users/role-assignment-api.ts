import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiSuccessResponse } from '../shared/api/api-response';
import { RoleAssignmentListPageResponse, RoleAssignmentRequest } from './user-access.models';

/** Роли комнат и чатов пользователя со страницы `/users/:id` (`UserRead` / `UserAccessManage`). */
@Injectable({ providedIn: 'root' })
export class RoleAssignmentApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/roleAssignment';

  getList(userId: string): Observable<ApiSuccessResponse<RoleAssignmentListPageResponse>> {
    return this.http.get<ApiSuccessResponse<RoleAssignmentListPageResponse>>(this.baseUrl, { params: { userId } });
  }

  create(request: RoleAssignmentRequest): Observable<ApiSuccessResponse<unknown>> {
    return this.http.post<ApiSuccessResponse<unknown>>(this.baseUrl, request);
  }

  delete(request: RoleAssignmentRequest): Observable<ApiSuccessResponse<unknown>> {
    return this.http.delete<ApiSuccessResponse<unknown>>(this.baseUrl, { params: toParams(request) });
  }
}

function toParams(request: RoleAssignmentRequest): Record<string, string> {
  const params: Record<string, string> = { userId: request.userId, roleName: request.roleName };
  if (request.roomId) {
    params['roomId'] = request.roomId;
  }
  if (request.chatId) {
    params['chatId'] = request.chatId;
  }
  return params;
}
