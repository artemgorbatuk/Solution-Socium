import { Injectable, computed, inject } from '@angular/core';
import { Auth } from '../account/auth';

/** Вошедший пользователь для экранов чатов; `null` — не вошёл. */
@Injectable({ providedIn: 'root' })
export class CurrentUser {
  private readonly auth = inject(Auth);

  readonly id = computed(() => this.auth.user()?.id ?? null);
  /** Меняется после правки своего имени: экраны с подписями отправителей и участников перечитывают данные. */
  readonly name = computed(() => this.auth.user()?.name ?? null);
}
