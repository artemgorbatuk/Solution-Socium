import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiSuccessResponse } from '../shared/api/api-response';
import {
  MessageCreateRequest,
  MessageDeletePageResponse,
  MessageDeleteResponse,
  MessageListPageResponse,
  MessageUpdateRequest,
} from './message.models';

@Injectable({ providedIn: 'root' })
export class MessageApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/message';

  getList(chatId: string): Observable<ApiSuccessResponse<MessageListPageResponse>> {
    return this.http.get<ApiSuccessResponse<MessageListPageResponse>>(this.baseUrl, { params: { chatId } });
  }

  create(request: MessageCreateRequest): Observable<ApiSuccessResponse<null>> {
    return this.http.post<ApiSuccessResponse<null>>(this.baseUrl, request);
  }

  update(request: MessageUpdateRequest): Observable<ApiSuccessResponse<null>> {
    return this.http.put<ApiSuccessResponse<null>>(this.baseUrl, request);
  }

  getDeletePage(id: string): Observable<ApiSuccessResponse<MessageDeletePageResponse>> {
    return this.http.get<ApiSuccessResponse<MessageDeletePageResponse>>(`${this.baseUrl}/delete`, { params: { id } });
  }

  delete(id: string): Observable<ApiSuccessResponse<MessageDeleteResponse>> {
    return this.http.delete<ApiSuccessResponse<MessageDeleteResponse>>(this.baseUrl, { params: { id } });
  }
}
