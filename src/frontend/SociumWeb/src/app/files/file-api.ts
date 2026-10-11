import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiSuccessResponse } from '../shared/api/api-response';
import { FileUploadResponse } from './file.models';

@Injectable({ providedIn: 'root' })
export class FileApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/file';

  upload(file: File): Observable<FileUploadResponse> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http
      .post<ApiSuccessResponse<FileUploadResponse>>(this.baseUrl, form)
      .pipe(map((body) => body.response!));
  }

  downloadBlob(id: string): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/${id}`, { responseType: 'blob' });
  }
}
