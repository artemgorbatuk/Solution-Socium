import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { successBody } from '../../shared/api/api-response.testing';
import { ConfirmEmailPage } from './confirm-email-page';

describe('ConfirmEmailPage', () => {
  let fixture: ComponentFixture<ConfirmEmailPage>;
  let http: HttpTestingController;
  let navigate: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ConfirmEmailPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    http = TestBed.inject(HttpTestingController);
    navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    fixture = TestBed.createComponent(ConfirmEmailPage);
  });

  afterEach(() => http.verify());

  it('Init_WithToken_ShouldConfirmAndOpenLoginWithConfirmed', () => {
    fixture.componentRef.setInput('token', 'plain-token');
    fixture.detectChanges();

    const request = http.expectOne({ method: 'POST', url: '/api/account/confirm-email' });
    expect(request.request.body).toEqual({ token: 'plain-token' });
    request.flush(successBody({ isConfirmed: true }));

    expect(navigate).toHaveBeenCalledWith(['/login'], { queryParams: { confirmed: '1' } });
  });

  it('Init_WithoutToken_ShouldShowErrorWithoutRequest', () => {
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[role=alert]')!.textContent).toContain('недействительна');
    http.expectNone('/api/account/confirm-email');
  });
});
