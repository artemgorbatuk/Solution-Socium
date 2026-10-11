import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { successBody } from '../shared/api/api-response.testing';
import { Auth } from './auth';
import { tokenResponse } from './auth.testing';

describe('Auth', () => {
  let auth: Auth;
  let http: HttpTestingController;

  function unauthorized(url: string): void {
    http.expectOne({ method: 'POST', url }).flush({ detail: 'Сессия истекла, войдите снова.' }, { status: 401, statusText: 'Unauthorized' });
  }

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    auth = TestBed.inject(Auth);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('Login_Submit_WithValidCredentials_ShouldPostRequestAndStartSession', () => {
    let name: string | undefined;

    auth.login({ login: 'ivan', password: 'Password-123', rememberMe: true }).subscribe((profile) => (name = profile.name));
    const request = http.expectOne({ method: 'POST', url: '/api/account/login' });
    expect(request.request.body).toEqual({ login: 'ivan', password: 'Password-123', rememberMe: true });
    request.flush(successBody(tokenResponse('u1', 'Иван', 'access-1')));

    expect(name).toBe('Иван');
    expect(auth.accessToken()).toBe('access-1');
    expect(auth.user()!.id).toBe('u1');
    expect(auth.isAuthenticated()).toBe(true);
  });

  it('Restore_Load_WithRefreshCookie_ShouldRefreshOnceAndReuseResult', () => {
    const results: boolean[] = [];

    auth.restore().subscribe((value) => results.push(value));
    auth.restore().subscribe((value) => results.push(value));
    http.expectOne({ method: 'POST', url: '/api/account/refresh' }).flush(successBody(tokenResponse('u1')));
    auth.restore().subscribe((value) => results.push(value));

    expect(results).toEqual([true, true, true]);
    expect(auth.accessToken()).toBe('token-u1');
  });

  it('Restore_Load_WithoutSession_ShouldAnswerFalseWithoutSecondRequest', () => {
    const results: boolean[] = [];

    auth.restore().subscribe((value) => results.push(value));
    unauthorized('/api/account/refresh');
    auth.restore().subscribe((value) => results.push(value));

    expect(results).toEqual([false, false]);
    expect(auth.isAuthenticated()).toBe(false);
  });

  it('Refresh_Call_WithParallelCallers_ShouldSendSingleRequest', () => {
    auth.start(tokenResponse('u1', 'Иван', 'old'));
    const results: boolean[] = [];

    auth.refresh().subscribe((value) => results.push(value));
    auth.refresh().subscribe((value) => results.push(value));
    http.expectOne('/api/account/refresh').flush(successBody(tokenResponse('u1', 'Иван', 'new')));

    expect(results).toEqual([true, true]);
    expect(auth.accessToken()).toBe('new');
  });

  it('Refresh_Call_WithRejectedCookie_ShouldClearSession', () => {
    auth.start(tokenResponse('u1'));
    let result: boolean | undefined;

    auth.refresh().subscribe((value) => (result = value));
    unauthorized('/api/account/refresh');

    expect(result).toBe(false);
    expect(auth.accessToken()).toBeNull();
    expect(auth.user()).toBeNull();
  });

  it('Logout_Click_WithServerErrorOrSuccess_ShouldClearSessionBeforeEmitting', () => {
    auth.start(tokenResponse('u1'));
    let authenticatedOnEmit: boolean | undefined;

    auth.logout().subscribe(() => (authenticatedOnEmit = auth.isAuthenticated()));
    http.expectOne({ method: 'POST', url: '/api/account/logout' }).flush(null, { status: 500, statusText: 'Server Error' });

    expect(authenticatedOnEmit).toBe(false);
    expect(auth.accessToken()).toBeNull();
  });

  it('UpdateProfile_Call_WithNewName_ShouldReplaceUserAndKeepToken', () => {
    auth.start(tokenResponse('u1', 'Иван'));

    auth.updateProfile({ id: 'u1', login: 'login-u1', email: 'u1@example.com', name: 'Иван Петров' });

    expect(auth.user()!.name).toBe('Иван Петров');
    expect(auth.accessToken()).toBe('token-u1');
  });
});
