import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiSuccessResponse } from '../shared/api/api-response';
import {
  JoinRequestAcceptResponse,
  JoinRequestCreateResponse,
  JoinRequestPendingListPageResponse,
  JoinRequestRejectResponse,
} from './join-request.models';

@Injectable({ providedIn: 'root' })
export class JoinRequestApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/joinRequest';

  create(chatId: string): Observable<ApiSuccessResponse<JoinRequestCreateResponse>> {
    return this.http.post<ApiSuccessResponse<JoinRequestCreateResponse>>(this.baseUrl, { chatId });
  }

  accept(id: string): Observable<ApiSuccessResponse<JoinRequestAcceptResponse>> {
    return this.http.post<ApiSuccessResponse<JoinRequestAcceptResponse>>(`${this.baseUrl}/accept`, { id });
  }

  reject(id: string): Observable<ApiSuccessResponse<JoinRequestRejectResponse>> {
    return this.http.post<ApiSuccessResponse<JoinRequestRejectResponse>>(`${this.baseUrl}/reject`, { id });
  }

  getPendingList(chatId: string): Observable<ApiSuccessResponse<JoinRequestPendingListPageResponse>> {
    return this.http.get<ApiSuccessResponse<JoinRequestPendingListPageResponse>>(`${this.baseUrl}/pending`, {
      params: { chatId },
    });
  }
}
