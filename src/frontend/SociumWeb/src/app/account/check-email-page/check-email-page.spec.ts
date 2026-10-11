import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { successBody } from '../../shared/api/api-response.testing';
import { CheckEmailPage } from './check-email-page';

describe('CheckEmailPage', () => {
  let fixture: ComponentFixture<CheckEmailPage>;
  let http: HttpTestingController;
  let element: HTMLElement;

  function render(): void {
    fixture.detectChanges();
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [CheckEmailPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(CheckEmailPage);
    element = fixture.nativeElement;
  });

  afterEach(() => http.verify());

  it('Form_Render_WithEmailQuery_ShouldPrefillAndAllowResend', () => {
    fixture.componentRef.setInput('email', 'anna@example.com');
    render();

    expect(element.querySelector('form')!.getAttribute('aria-label')).toBe('Проверьте почту');
    expect(element.querySelector<HTMLInputElement>('[name=email]')!.value).toBe('anna@example.com');
    expect(element.textContent).toContain('Выслать ещё раз');

    element.querySelector('form')!.dispatchEvent(new Event('submit'));
    render();
    const request = http.expectOne({ method: 'POST', url: '/api/account/resend-confirmation' });
    expect(request.request.body).toEqual({ email: 'anna@example.com' });
    request.flush(successBody({ isSent: true }));
    render();

    expect(element.querySelector('[role=status]')!.textContent).toContain('Если аккаунт существует, письмо отправлено');
  });

  it('Form_Render_WithoutEmailQuery_ShouldKeepSubmitDisabledWithoutThrowing', () => {
    fixture.componentRef.setInput('email', undefined);
    render();

    expect(element.querySelector<HTMLInputElement>('[name=email]')!.value).toBe('');
    expect(element.querySelector<HTMLButtonElement>('button[type=submit]')!.disabled).toBe(true);
  });
});
