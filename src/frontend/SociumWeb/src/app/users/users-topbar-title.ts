import { DestroyRef, inject } from '@angular/core';
import { TopbarTitle } from '../layout/topbar/topbar-title';

export const usersTopbarTitle = 'Пользователи';
export const usersCloseLabel = 'Закрыть раздел пользователей';

/** Заголовок раздела `/users` в верхней полосе с крестиком на главную; вызывать в конструкторе страницы. */
export function showUsersTopbarTitle(): void {
  const title = inject(TopbarTitle);
  title.show(usersTopbarTitle, usersCloseLabel);
  inject(DestroyRef).onDestroy(() => title.clear());
}
