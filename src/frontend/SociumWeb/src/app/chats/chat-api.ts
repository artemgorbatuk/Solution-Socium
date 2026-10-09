import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiSuccessResponse } from '../shared/api/api-response';
import {
  ChatCreateRequest,
  ChatDeletePageResponse,
  ChatDeleteResponse,
  ChatListPageResponse,
  ChatUpdateRequest,
} from './chat.models';

@Injectable({ providedIn: 'root' })
export class ChatApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/chat';

  getList(roomId: string): Observable<ApiSuccessResponse<ChatListPageResponse>> {
    return this.http.get<ApiSuccessResponse<ChatListPageResponse>>(this.baseUrl, { params: { roomId } });
  }

  create(request: ChatCreateRequest): Observable<ApiSuccessResponse<null>> {
    return this.http.post<ApiSuccessResponse<null>>(this.baseUrl, request);
  }

  update(request: ChatUpdateRequest): Observable<ApiSuccessResponse<null>> {
    return this.http.put<ApiSuccessResponse<null>>(this.baseUrl, request);
  }

  getDeletePage(id: string): Observable<ApiSuccessResponse<ChatDeletePageResponse>> {
    return this.http.get<ApiSuccessResponse<ChatDeletePageResponse>>(`${this.baseUrl}/delete`, { params: { id } });
  }

  delete(id: string): Observable<ApiSuccessResponse<ChatDeleteResponse>> {
    return this.http.delete<ApiSuccessResponse<ChatDeleteResponse>>(this.baseUrl, { params: { id } });
  }
}
