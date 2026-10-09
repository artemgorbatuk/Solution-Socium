import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiSuccessResponse } from '../shared/api/api-response';
import {
  ParticipantCreateResponse,
  ParticipantDeletePageResponse,
  ParticipantDeleteResponse,
  ParticipantListPageResponse,
  ParticipantUpdateRequest,
  ParticipantUpdateResponse,
} from './participant.models';

@Injectable({ providedIn: 'root' })
export class ParticipantApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/participant';

  getList(chatId: string): Observable<ApiSuccessResponse<ParticipantListPageResponse>> {
    return this.http.get<ApiSuccessResponse<ParticipantListPageResponse>>(this.baseUrl, { params: { chatId } });
  }

  /** Вступление текущего пользователя в чат. */
  join(chatId: string): Observable<ApiSuccessResponse<ParticipantCreateResponse>> {
    return this.http.post<ApiSuccessResponse<ParticipantCreateResponse>>(this.baseUrl, { chatId });
  }

  update(request: ParticipantUpdateRequest): Observable<ApiSuccessResponse<ParticipantUpdateResponse>> {
    return this.http.put<ApiSuccessResponse<ParticipantUpdateResponse>>(this.baseUrl, request);
  }

  /** Можно ли текущему пользователю покинуть чат и почему нельзя. */
  getLeavePage(chatId: string): Observable<ApiSuccessResponse<ParticipantDeletePageResponse>> {
    return this.http.get<ApiSuccessResponse<ParticipantDeletePageResponse>>(`${this.baseUrl}/delete`, { params: { chatId } });
  }

  leave(chatId: string): Observable<ApiSuccessResponse<ParticipantDeleteResponse>> {
    return this.http.delete<ApiSuccessResponse<ParticipantDeleteResponse>>(this.baseUrl, { params: { chatId } });
  }
}
