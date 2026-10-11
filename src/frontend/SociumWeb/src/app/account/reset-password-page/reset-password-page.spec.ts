import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { successBody } from '../../shared/api/api-response.testing';
import { ResetPasswordPage } from './reset-password-page';

describe('ResetPasswordPage', () => {
  let fixture: ComponentFixture<ResetPasswordPage>;
  let http: HttpTestingController;
  let element: HTMLElement;
  let navigate: ReturnType<typeof vi.spyOn>;

  function render(): void {
    fixture.detectChanges();
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ResetPasswordPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    http = TestBed.inject(HttpTestingController);
    navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    fixture = TestBed.createComponent(ResetPasswordPage);
    element = fixture.nativeElement;
  });

  afterEach(() => http.verify());

  it('Submit_WithTokenAndPassword_ShouldResetAndOpenLogin', () => {
    fixture.componentRef.setInput('token', 'reset-token');
    render();

    const field = element.querySelector<HTMLInputElement>('[name=password]')!;
    field.value = 'Password-456';
    field.dispatchEvent(new Event('input'));
    render();

    element.querySelector('form')!.dispatchEvent(new Event('submit'));
    render();
    const request = http.expectOne({ method: 'POST', url: '/api/account/reset-password' });
    expect(request.request.body).toEqual({ token: 'reset-token', password: 'Password-456' });
    request.flush(successBody({ isReset: true }));

    expect(navigate).toHaveBeenCalledWith(['/login'], { queryParams: { reset: '1' } });
  });
});
