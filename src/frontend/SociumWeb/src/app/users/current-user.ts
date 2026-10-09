import { Injectable, signal } from '@angular/core';

export const currentUserStorageKey = 'socium.currentUserId';

/** Временный выбор пользователя без пароля (task-0010); при настоящем входе меняется только источник `id`. */
@Injectable({ providedIn: 'root' })
export class CurrentUser {
  private readonly selectedId = signal<string | null>(localStorage.getItem(currentUserStorageKey));

  readonly id = this.selectedId.asReadonly();

  select(id: string): void {
    this.selectedId.set(id);
    localStorage.setItem(currentUserStorageKey, id);
  }

  clear(): void {
    this.selectedId.set(null);
    localStorage.removeItem(currentUserStorageKey);
  }
}
