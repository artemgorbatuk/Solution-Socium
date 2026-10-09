import { HttpErrorResponse } from '@angular/common/http';

export interface MessageInfo {
  messageType: number;
  messageText: string;
}

/** Тело ответа 200 от `ApiControllerBase.FromResponse` (WebApi/Controllers/Shared/ApiSuccessResponse.cs). */
export interface ApiSuccessResponse<T> {
  messageInfo: MessageInfo;
  response: T | null;
  messageTypeDescription: string;
  messageAlert: string;
  messageTextColor: string;
  messageBorderColor: string;
  messageBackgroundColor: string;
  messageIcon: string;
}

/** Текст ошибки из Problem Details (`detail`) или запасной текст. */
export function problemDetail(error: unknown, fallback: string): string {
  if (error instanceof HttpErrorResponse && typeof error.error?.detail === 'string') {
    return error.error.detail;
  }
  return fallback;
}
