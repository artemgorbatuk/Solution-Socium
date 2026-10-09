import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { CurrentUser } from './current-user';

/** Совпадает с `CurrentUserFromHeader.HeaderName` в WebApi. */
export const currentUserHeader = 'X-User-Id';

export const currentUserInterceptor: HttpInterceptorFn = (request, next) => {
  const id = inject(CurrentUser).id();
  if (!id || !request.url.startsWith('/api/')) {
    return next(request);
  }
  return next(request.clone({ setHeaders: { [currentUserHeader]: id } }));
};
