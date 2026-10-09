import { ApiSuccessResponse } from './api-response';

/** Тело успешного ответа WebApi для подмены через `HttpTestingController`. */
export function successBody<T>(response: T | null, messageType = 1): ApiSuccessResponse<T> {
  return {
    messageInfo: { messageType, messageText: '' },
    response,
    messageTypeDescription: '',
    messageAlert: '',
    messageTextColor: '',
    messageBorderColor: '',
    messageBackgroundColor: '',
    messageIcon: '',
  };
}
