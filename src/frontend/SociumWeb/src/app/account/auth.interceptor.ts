import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { accountBaseUrl } from './account-api';
import { Auth } from './auth';
import { loginUrlTree } from './login-redirect';

/** Эти запросы работают по cookie или паролю: токен им не нужен, а их `401` не повод обновлять токен. */
const sessionUrls = [`${accountBaseUrl}/login`, `${accountBaseUrl}/refresh`, `${accountBaseUrl}/logout`];

function withToken(request: HttpRequest<unknown>, token: string | null): HttpRequest<unknown> {
  return token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request;
}

function isUnauthorized(error: unknown): boolean {
  return error instanceof HttpErrorResponse && error.status === 401;
}

/**
 * Добавляет access-токен к запросам `/api`; на `401` один раз обновляет токен и повторяет запрос.
 * Не удалось обновить или повтор снова получил `401` — сессия завершена, открывается экран входа.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  if (!request.url.startsWith('/api/') || sessionUrls.includes(request.url)) {
    return next(request);
  }

  const auth = inject(Auth);
  const router = inject(Router);
  const toLogin = (error: unknown) => {
    auth.clear();
    void router.navigateByUrl(loginUrlTree(router, router.url));
    return throwError(() => error);
  };

  return next(withToken(request, auth.accessToken())).pipe(
    catchError((error: unknown) => {
      if (!isUnauthorized(error)) {
        return throwError(() => error);
      }
      return auth.refresh().pipe(
        switchMap((refreshed) =>
          refreshed
            ? next(withToken(request, auth.accessToken())).pipe(
                catchError((retryError: unknown) => (isUnauthorized(retryError) ? toLogin(retryError) : throwError(() => retryError))),
              )
            : toLogin(error),
        ),
      );
    }),
  );
};
