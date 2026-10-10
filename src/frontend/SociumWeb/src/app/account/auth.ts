import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, finalize, map, of, shareReplay, tap } from 'rxjs';
import { AccountApi } from './account-api';
import { AccountLoginRequest, AccountProfile, AccountTokenResponse } from './account.models';

/**
 * Сессия текущей вкладки. Access-токен живёт только в памяти; после перезагрузки страницы
 * сессия восстанавливается тихим обновлением по refresh-cookie.
 */
@Injectable({ providedIn: 'root' })
export class Auth {
  private readonly api = inject(AccountApi);

  private readonly token = signal<string | null>(null);
  private readonly profile = signal<AccountProfile | null>(null);
  private restored = false;
  private refreshing: Observable<boolean> | null = null;

  readonly accessToken = this.token.asReadonly();
  readonly user = this.profile.asReadonly();
  readonly isAuthenticated = computed(() => this.profile() !== null);

  /** Тихое обновление — один раз за загрузку страницы; дальше отвечает по текущему состоянию. */
  restore(): Observable<boolean> {
    if (this.restored || this.isAuthenticated()) {
      return of(this.isAuthenticated());
    }
    return this.refresh().pipe(tap(() => (this.restored = true)));
  }

  login(request: AccountLoginRequest): Observable<AccountProfile> {
    return this.api.login(request).pipe(map((body) => this.start(body.response!)));
  }

  /** Одно обновление на все параллельные запросы; `false` — сессии нет. */
  refresh(): Observable<boolean> {
    this.refreshing ??= this.api.refresh().pipe(
      map((body) => {
        this.start(body.response!);
        return true;
      }),
      catchError(() => {
        this.clear();
        return of(false);
      }),
      finalize(() => (this.refreshing = null)),
      shareReplay(1),
    );
    return this.refreshing;
  }

  /** Сессия на устройстве завершается, даже если сервер не ответил. */
  logout(): Observable<void> {
    return this.api.logout().pipe(
      catchError(() => of(null)),
      map(() => this.clear()),
    );
  }

  /** Начинает сессию по ответу входа или обновления. */
  start(tokens: AccountTokenResponse): AccountProfile {
    this.restored = true;
    this.token.set(tokens.accessToken);
    this.profile.set(tokens.user);
    return tokens.user;
  }

  updateProfile(profile: AccountProfile): void {
    this.profile.set(profile);
  }

  clear(): void {
    this.restored = true;
    this.token.set(null);
    this.profile.set(null);
  }
}
