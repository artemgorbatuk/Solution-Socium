import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { successBody } from '../../shared/api/api-response.testing';
import { ForgotPasswordPage } from './forgot-password-page';

describe('ForgotPasswordPage', () => {
  let fixture: ComponentFixture<ForgotPasswordPage>;
  let http: HttpTestingController;
  let element: HTMLElement;

  function render(): void {
    fixture.detectChanges();
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ForgotPasswordPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(ForgotPasswordPage);
    element = fixture.nativeElement;
    render();
  });

  afterEach(() => http.verify());

  it('Submit_WithEmail_ShouldRequestResetLink', () => {
    const field = element.querySelector<HTMLInputElement>('[name=email]')!;
    field.value = 'anna@example.com';
    field.dispatchEvent(new Event('input'));
    render();

    element.querySelector('form')!.dispatchEvent(new Event('submit'));
    render();
    const request = http.expectOne({ method: 'POST', url: '/api/account/forgot-password' });
    expect(request.request.body).toEqual({ email: 'anna@example.com' });
    request.flush(successBody({ isSent: true }));
    render();

    expect(element.querySelector('[role=status]')!.textContent).toContain('Если аккаунт существует, письмо отправлено');
  });
});
