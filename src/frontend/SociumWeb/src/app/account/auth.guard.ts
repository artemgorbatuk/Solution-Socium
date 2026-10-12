import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { Auth } from './auth';
import { loginUrlTree } from './login-redirect';

/** Без входа — на экран входа с возвратом на запрошенный адрес. */
export const authGuard: CanActivateFn = (_route, state) => {
  const router = inject(Router);
  return inject(Auth)
    .restore()
    .pipe(map((authenticated) => authenticated || loginUrlTree(router, state.url)));
};

/** Страницы пользователей — только админу приложения; остальным — на главную. Сервер всё равно отвечает `403`. */
export const usersGuard: CanActivateFn = () => inject(Auth).user()?.canManageUsers === true || inject(Router).parseUrl('/');

/** Экран входа вошедшему не нужен — сразу в приложение. */
export const guestGuard: CanActivateFn = () => {
  const router = inject(Router);
  return inject(Auth)
    .restore()
    .pipe(map((authenticated) => !authenticated || router.parseUrl('/')));
};
