import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { successBody } from '../shared/api/api-response.testing';
import { Auth } from './auth';
import { authGuard, guestGuard, usersGuard } from './auth.guard';
import { tokenResponse } from './auth.testing';
import { safeReturnUrl } from './login-redirect';

@Component({ template: '' })
class Blank {}

describe('authGuard and guestGuard', () => {
  let router: Router;
  let http: HttpTestingController;

  function navigate(url: string): Promise<boolean> {
    return router.navigateByUrl(url);
  }

  /** Guard вызывается не сразу: навигация доходит до него через несколько микрозадач. */
  async function respondRefresh(signedIn: boolean): Promise<void> {
    await new Promise((resolve) => setTimeout(resolve));
    const request = http.expectOne({ method: 'POST', url: '/api/account/refresh' });
    if (signedIn) {
      request.flush(successBody(tokenResponse('u1')));
    } else {
      request.flush({ detail: 'Сессия истекла, войдите снова.' }, { status: 401, statusText: 'Unauthorized' });
    }
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([
          { path: 'login', component: Blank, canActivate: [guestGuard] },
          {
            path: '',
            canActivate: [authGuard],
            children: [
              { path: '', component: Blank },
              { path: 'chat/:id', component: Blank },
              { path: 'users', component: Blank, canActivate: [usersGuard] },
            ],
          },
        ]),
      ],
    });
    router = TestBed.inject(Router);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('AuthGuard_Navigate_WithRefreshCookie_ShouldRestoreSessionAndOpenPage', async () => {
    const navigation = navigate('/chat/c1');
    await respondRefresh(true);
    await navigation;

    expect(router.url).toBe('/chat/c1');
  });

  it('AuthGuard_Navigate_WithoutSession_ShouldOpenLoginWithReturnUrl', async () => {
    const navigation = navigate('/chat/c1');
    await respondRefresh(false);
    await navigation;

    expect(router.url).toBe('/login?returnUrl=%2Fchat%2Fc1');
  });

  it('AuthGuard_Navigate_WithoutSessionToRoot_ShouldOpenLoginWithoutReturnUrl', async () => {
    const navigation = navigate('/');
    await respondRefresh(false);
    await navigation;

    expect(router.url).toBe('/login');
  });

  it('GuestGuard_Navigate_WithSession_ShouldOpenApp', async () => {
    const navigation = navigate('/login');
    await respondRefresh(true);
    await navigation;

    expect(router.url).toBe('/');
  });

  it('Guards_Navigate_AfterFirstRestore_ShouldNotRefreshAgain', async () => {
    const first = navigate('/login');
    await respondRefresh(false);
    await first;

    await navigate('/chat/c1');

    expect(router.url).toBe('/login?returnUrl=%2Fchat%2Fc1');
    http.expectNone('/api/account/refresh');
  });

  it('UsersGuard_Navigate_WithOrdinaryUser_ShouldOpenRoot', async () => {
    TestBed.inject(Auth).start(tokenResponse('u1'));

    await navigate('/users');

    expect(router.url).toBe('/');
  });

  it('UsersGuard_Navigate_WithAppAdmin_ShouldOpenUsers', async () => {
    TestBed.inject(Auth).start(tokenResponse('u1', undefined, undefined, true));

    await navigate('/users');

    expect(router.url).toBe('/users');
  });

  it('SafeReturnUrl_Check_WithForeignOrLoginAddress_ShouldFallBackToRoot', () => {
    expect(safeReturnUrl('/chat/c1')).toBe('/chat/c1');
    expect(safeReturnUrl('//evil.example')).toBe('/');
    expect(safeReturnUrl('https://evil.example')).toBe('/');
    expect(safeReturnUrl('/login?returnUrl=%2F')).toBe('/');
    expect(safeReturnUrl(undefined)).toBe('/');
  });
});
