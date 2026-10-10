import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { App } from './app';
import { routes } from './app.routes';

describe('App', () => {
  afterEach(() => delete document.documentElement.dataset['theme']);

  beforeEach(async () => {
    localStorage.clear();
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter(routes)],
    }).compileComponents();
  });

  it('App_Start_WithoutSession_ShouldShowOnlyLoginScreen', async () => {
    const fixture = TestBed.createComponent(App);
    const navigation = TestBed.inject(Router).navigateByUrl('/chat/c1');
    await new Promise((resolve) => setTimeout(resolve));
    TestBed.inject(HttpTestingController)
      .expectOne('/api/account/refresh')
      .flush({ detail: 'Сессия истекла, войдите снова.' }, { status: 401, statusText: 'Unauthorized' });
    await navigation;
    fixture.detectChanges();
    const element: HTMLElement = fixture.nativeElement;

    expect(TestBed.inject(Router).url).toBe('/login?returnUrl=%2Fchat%2Fc1');
    expect(element.querySelector('app-login-page form')).not.toBeNull();
    expect(element.querySelector('app-sidebar')).toBeNull();
  });
});
