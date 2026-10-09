import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiSuccessResponse } from '../shared/api/api-response';
import {
  RoomCreateRequest,
  RoomDeletePageResponse,
  RoomDeleteResponse,
  RoomListPageResponse,
  RoomUpdateRequest,
} from './room.models';

@Injectable({ providedIn: 'root' })
export class RoomApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/room';

  getList(): Observable<ApiSuccessResponse<RoomListPageResponse>> {
    return this.http.get<ApiSuccessResponse<RoomListPageResponse>>(this.baseUrl);
  }

  create(request: RoomCreateRequest): Observable<ApiSuccessResponse<null>> {
    return this.http.post<ApiSuccessResponse<null>>(this.baseUrl, request);
  }

  update(request: RoomUpdateRequest): Observable<ApiSuccessResponse<null>> {
    return this.http.put<ApiSuccessResponse<null>>(this.baseUrl, request);
  }

  getDeletePage(id: string): Observable<ApiSuccessResponse<RoomDeletePageResponse>> {
    return this.http.get<ApiSuccessResponse<RoomDeletePageResponse>>(`${this.baseUrl}/delete`, { params: { id } });
  }

  delete(id: string): Observable<ApiSuccessResponse<RoomDeleteResponse>> {
    return this.http.delete<ApiSuccessResponse<RoomDeleteResponse>>(this.baseUrl, { params: { id } });
  }
}
