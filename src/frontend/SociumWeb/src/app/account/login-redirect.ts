import { Router, UrlTree } from '@angular/router';

export const loginPath = '/login';

/** Только адреса этого приложения: `//host` и абсолютные ссылки не принимаются. */
export function safeReturnUrl(url: string | null | undefined): string {
  return url && url.startsWith('/') && !url.startsWith('//') && !url.startsWith(loginPath) ? url : '/';
}

/** Экран входа с возвратом на `returnUrl` после входа; корень приложения в адрес не пишется. */
export function loginUrlTree(router: Router, returnUrl: string): UrlTree {
  const target = safeReturnUrl(returnUrl);
  return router.createUrlTree([loginPath], target === '/' ? {} : { queryParams: { returnUrl: target } });
}
