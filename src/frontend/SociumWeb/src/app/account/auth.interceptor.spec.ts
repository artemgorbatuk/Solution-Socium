import { HttpClient, HttpErrorResponse, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { successBody } from '../shared/api/api-response.testing';
import { Auth } from './auth';
import { authInterceptor } from './auth.interceptor';
import { signInAs, tokenResponse } from './auth.testing';

describe('authInterceptor', () => {
  let http: HttpTestingController;
  let client: HttpClient;
  let navigate: ReturnType<typeof vi.spyOn>;

  function unauthorized() {
    return [{ detail: 'Требуется вход в систему.' }, { status: 401, statusText: 'Unauthorized' }] as const;
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(withInterceptors([authInterceptor])), provideHttpClientTesting(), provideRouter([{ path: '**', children: [] }])],
    });
    http = TestBed.inject(HttpTestingController);
    client = TestBed.inject(HttpClient);
  });

  /** После реальной навигации теста: дальше переходы только записываются. */
  function spyNavigate(): void {
    navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
  }

  afterEach(() => http.verify());

  it('Request_Send_WithSession_ShouldAddBearerToken', () => {
    signInAs('u1');

    client.get('/api/room').subscribe();

    expect(http.expectOne('/api/room').request.headers.get('Authorization')).toBe('Bearer token-u1');
  });

  it('Request_Send_WithoutSessionOrOutsideApi_ShouldNotAddToken', () => {
    client.get('/api/user/create').subscribe();
    signInAs('u1');
    client.get('/assets/config.json').subscribe();

    expect(http.expectOne('/api/user/create').request.headers.has('Authorization')).toBe(false);
    expect(http.expectOne('/assets/config.json').request.headers.has('Authorization')).toBe(false);
  });

  it('SessionRequests_Send_WithSession_ShouldNotAddTokenNorRefreshOn401', () => {
    signInAs('u1');
    let error: HttpErrorResponse | undefined;

    client.post('/api/account/login', {}).subscribe({ error: (value) => (error = value) });
    const login = http.expectOne('/api/account/login');
    expect(login.request.headers.has('Authorization')).toBe(false);
    login.flush(...unauthorized());

    expect(error!.status).toBe(401);
    http.expectNone('/api/account/refresh');
  });

  it('Response_401_WithParallelRequests_ShouldRefreshOnceAndRetryWithNewToken', () => {
    signInAs('u1');
    const results: unknown[] = [];

    client.get('/api/room').subscribe((value) => results.push(value));
    client.get('/api/chat').subscribe((value) => results.push(value));
    http.expectOne('/api/room').flush(...unauthorized());
    http.expectOne('/api/chat').flush(...unauthorized());
    http.expectOne({ method: 'POST', url: '/api/account/refresh' }).flush(successBody(tokenResponse('u1', 'Иван', 'fresh')));

    const retries = [http.expectOne('/api/room'), http.expectOne('/api/chat')];
    expect(retries.map((retry) => retry.request.headers.get('Authorization'))).toEqual(['Bearer fresh', 'Bearer fresh']);
    retries.forEach((retry) => retry.flush({ ok: true }));
    expect(results).toEqual([{ ok: true }, { ok: true }]);
  });

  it('Response_401_WithRejectedRefresh_ShouldClearSessionAndOpenLoginWithReturnUrl', async () => {
    signInAs('u1');
    await TestBed.inject(Router).navigateByUrl('/chat/c1');
    spyNavigate();
    let error: HttpErrorResponse | undefined;

    client.get('/api/room').subscribe({ error: (value) => (error = value) });
    http.expectOne('/api/room').flush(...unauthorized());
    http.expectOne('/api/account/refresh').flush(...unauthorized());

    expect(error!.status).toBe(401);
    expect(TestBed.inject(Auth).isAuthenticated()).toBe(false);
    expect(navigate.mock.calls[0][0].toString()).toBe('/login?returnUrl=%2Fchat%2Fc1');
  });

  it('Response_401_WithRetryRejectedAgain_ShouldClearSessionAndOpenLogin', () => {
    signInAs('u1');
    spyNavigate();

    client.get('/api/room').subscribe({ error: () => undefined });
    http.expectOne('/api/room').flush(...unauthorized());
    http.expectOne('/api/account/refresh').flush(successBody(tokenResponse('u1')));
    http.expectOne('/api/room').flush(...unauthorized());

    expect(TestBed.inject(Auth).isAuthenticated()).toBe(false);
    expect(navigate.mock.calls[0][0].toString()).toBe('/login');
  });

  it('Response_OtherError_WithSession_ShouldPassErrorWithoutRefresh', () => {
    signInAs('u1');
    let error: HttpErrorResponse | undefined;

    client.get('/api/room').subscribe({ error: (value) => (error = value) });
    http.expectOne('/api/room').flush({ detail: 'Ошибка' }, { status: 500, statusText: 'Server Error' });

    expect(error!.status).toBe(500);
    http.expectNone('/api/account/refresh');
    expect(TestBed.inject(Auth).isAuthenticated()).toBe(true);
  });
});
