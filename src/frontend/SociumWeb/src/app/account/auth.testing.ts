import { TestBed } from '@angular/core/testing';
import { AccountTokenResponse } from './account.models';
import { Auth } from './auth';

export function tokenResponse(id: string, name = `Пользователь ${id}`, accessToken = `token-${id}`): AccountTokenResponse {
  return {
    accessToken,
    accessTokenExpiresAt: '2026-10-10T12:00:00Z',
    user: { id, login: `login-${id}`, email: `${id}@example.com`, name },
  };
}

/** Начинает сессию пользователя `id` без запросов к API. */
export function signInAs(id: string, name?: string): void {
  TestBed.inject(Auth).start(tokenResponse(id, name));
}

export function signOut(): void {
  TestBed.inject(Auth).clear();
}
