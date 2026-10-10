import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { successBody } from '../../shared/api/api-response.testing';
import { Auth } from '../auth';
import { tokenResponse } from '../auth.testing';
import { LoginPage } from './login-page';

describe('LoginPage', () => {
  let fixture: ComponentFixture<LoginPage>;
  let http: HttpTestingController;
  let element: HTMLElement;
  let navigate: ReturnType<typeof vi.spyOn>;

  function render(): void {
    fixture.detectChanges();
  }

  function input(name: string, value: string): void {
    const field = element.querySelector<HTMLInputElement>(`[name=${name}]`)!;
    field.value = value;
    field.dispatchEvent(new Event('input'));
    render();
  }

  function button(text: string): HTMLButtonElement {
    return Array.from(element.querySelectorAll('button')).find((item) => item.textContent!.trim() === text)!;
  }

  function submit(): void {
    element.querySelector('form')!.dispatchEvent(new Event('submit'));
    render();
  }

  function checkRememberMe(): void {
    const box = element.querySelector<HTMLInputElement>('[name=rememberMe]')!;
    box.checked = true;
    box.dispatchEvent(new Event('change'));
    render();
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [LoginPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    http = TestBed.inject(HttpTestingController);
    navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    fixture = TestBed.createComponent(LoginPage);
    element = fixture.nativeElement;
    render();
  });

  afterEach(() => http.verify());

  it('Form_Render_WithLoginMode_ShouldShowLoginPasswordRememberMeAndRegisterLink', () => {
    expect(element.querySelector('form')!.getAttribute('aria-label')).toBe('Вход');
    expect(element.querySelector('[name=login]')).not.toBeNull();
    expect(element.querySelector('[name=name]')).toBeNull();
    expect(element.querySelector<HTMLInputElement>('[name=password]')!.type).toBe('password');
    expect(element.textContent).toContain('Запомнить меня');
    expect(button('Войти').disabled).toBe(true);
    expect(button('Регистрация')).toBeDefined();
    expect(element.querySelector('app-theme-toggle')).not.toBeNull();
  });

  it('Login_Submit_WithCredentialsAndReturnUrl_ShouldStartSessionAndOpenReturnUrl', () => {
    fixture.componentRef.setInput('returnUrl', '/chat/c1');
    input('login', '  Ivan  ');
    input('password', ' Password-123 ');
    checkRememberMe();

    submit();
    const request = http.expectOne({ method: 'POST', url: '/api/account/login' });
    expect(request.request.body).toEqual({ login: 'Ivan', password: ' Password-123 ', rememberMe: true });
    request.flush(successBody(tokenResponse('u1')));

    expect(TestBed.inject(Auth).isAuthenticated()).toBe(true);
    expect(navigate).toHaveBeenCalledWith('/chat/c1');
  });

  it('Login_Submit_WithForeignReturnUrl_ShouldOpenRoot', () => {
    fixture.componentRef.setInput('returnUrl', '//evil.example');
    input('login', 'ivan');
    input('password', 'Password-123');

    submit();
    http.expectOne('/api/account/login').flush(successBody(tokenResponse('u1')));

    expect(navigate).toHaveBeenCalledWith('/');
  });

  it('Login_Submit_WithWrongPassword_ShouldShowGeneralProblemAndEnableForm', () => {
    input('login', 'ivan');
    input('password', 'wrong-password');

    submit();
    expect(button('Войти').disabled).toBe(true);
    http
      .expectOne('/api/account/login')
      .flush({ detail: 'Не удалось войти\nНеверный логин или пароль.' }, { status: 401, statusText: 'Unauthorized' });
    render();

    expect(element.querySelector('[role=alert]')!.textContent).toContain('Неверный логин или пароль.');
    expect(button('Войти').disabled).toBe(false);
    expect(navigate).not.toHaveBeenCalled();
  });

  it('Register_Submit_WithNewUser_ShouldCreateUserThenLogIn', () => {
    button('Регистрация').click();
    render();
    expect(element.querySelector('form')!.getAttribute('aria-label')).toBe('Регистрация');
    expect(element.querySelector<HTMLInputElement>('[name=password]')!.getAttribute('autocomplete')).toBe('new-password');
    input('login', 'anna');
    input('name', '  Анна  ');
    input('password', 'Password-123');

    submit();
    const created = http.expectOne({ method: 'POST', url: '/api/user' });
    expect(created.request.body).toEqual({ login: 'anna', name: 'Анна', password: 'Password-123' });
    created.flush(successBody(null, 2));
    const login = http.expectOne('/api/account/login');
    expect(login.request.body).toEqual({ login: 'anna', password: 'Password-123', rememberMe: false });
    login.flush(successBody(tokenResponse('u3', 'Анна')));

    expect(TestBed.inject(Auth).user()!.name).toBe('Анна');
    expect(navigate).toHaveBeenCalledWith('/');
  });

  it('Register_Submit_WithBlankName_ShouldKeepSubmitDisabled', () => {
    button('Регистрация').click();
    render();
    input('login', 'anna');
    input('name', '   ');
    input('password', 'Password-123');

    expect(button('Зарегистрироваться').disabled).toBe(true);
  });

  it('Register_Submit_WithShortPassword_ShouldShowProblemWithoutLogin', () => {
    button('Регистрация').click();
    render();
    input('login', 'anna');
    input('name', 'Анна');
    input('password', 'short');

    submit();
    http
      .expectOne('/api/user')
      .flush({ detail: 'Пароль должен содержать от 8 до 128 символов.' }, { status: 422, statusText: 'Unprocessable Entity' });
    render();

    expect(element.querySelector('[role=alert]')!.textContent).toContain('Пароль должен содержать от 8 до 128 символов.');
    http.expectNone('/api/account/login');
  });

  it('Mode_Switch_WithError_ShouldClearError', () => {
    input('login', 'ivan');
    input('password', 'wrong-password');
    submit();
    http.expectOne('/api/account/login').flush({ detail: 'Неверный логин или пароль.' }, { status: 401, statusText: 'Unauthorized' });
    render();

    button('Регистрация').click();
    render();

    expect(element.querySelector('[role=alert]')).toBeNull();
    button('Войти').click();
    render();
    expect(element.querySelector('form')!.getAttribute('aria-label')).toBe('Вход');
  });
});
