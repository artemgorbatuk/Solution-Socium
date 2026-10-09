import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { CurrentUser } from './current-user';
import { currentUserHeader, currentUserInterceptor } from './current-user.interceptor';

describe('currentUserInterceptor', () => {
  let http: HttpClient;
  let httpTesting: HttpTestingController;
  let currentUser: CurrentUser;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(withInterceptors([currentUserInterceptor])), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpClient);
    httpTesting = TestBed.inject(HttpTestingController);
    currentUser = TestBed.inject(CurrentUser);
  });

  afterEach(() => {
    httpTesting.verify();
    localStorage.clear();
  });

  it('Request_Send_WithSelectedUser_ShouldAddUserIdHeaderToApi', () => {
    currentUser.select('user-1');

    http.get('/api/room').subscribe();

    expect(httpTesting.expectOne('/api/room').request.headers.get(currentUserHeader)).toBe('user-1');
  });

  it('Request_Send_WithNoSelectedUser_ShouldNotAddHeader', () => {
    http.get('/api/room').subscribe();

    expect(httpTesting.expectOne('/api/room').request.headers.has(currentUserHeader)).toBe(false);
  });

  it('Request_Send_WithSelectedUserToOtherUrl_ShouldNotAddHeader', () => {
    currentUser.select('user-1');

    http.get('/assets/config.json').subscribe();

    expect(httpTesting.expectOne('/assets/config.json').request.headers.has(currentUserHeader)).toBe(false);
  });

  it('Request_Send_AfterUserChanged_ShouldUseNewUserId', () => {
    currentUser.select('user-1');
    currentUser.select('user-2');

    http.post('/api/message', {}).subscribe();

    expect(httpTesting.expectOne('/api/message').request.headers.get(currentUserHeader)).toBe('user-2');
  });
});
