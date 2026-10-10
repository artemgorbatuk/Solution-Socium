import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { successBody } from '../../shared/api/api-response.testing';
import { Auth } from '../auth';
import { signInAs } from '../auth.testing';
import { AccountMenu } from './account-menu';

describe('AccountMenu', () => {
  let fixture: ComponentFixture<AccountMenu>;
  let http: HttpTestingController;
  let element: HTMLElement;

  function render(): void {
    fixture.detectChanges();
  }

  function trigger(): HTMLButtonElement {
    return element.querySelector<HTMLButtonElement>('.trigger')!;
  }

  function button(text: string): HTMLButtonElement {
    return Array.from(element.querySelectorAll('button')).find((item) => item.textContent!.trim() === text)!;
  }

  function openPanel(): void {
    trigger().click();
    render();
  }

  function startEdit(): void {
    openPanel();
    button('Изменить имя').click();
    render();
  }

  function inputName(value: string): void {
    const field = element.querySelector<HTMLInputElement>('[name=name]')!;
    field.value = value;
    field.dispatchEvent(new Event('input'));
    render();
  }

  function pressEscape(): void {
    element.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    render();
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [AccountMenu],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    http = TestBed.inject(HttpTestingController);
    signInAs('u1', 'Иван');
    fixture = TestBed.createComponent(AccountMenu);
    element = fixture.nativeElement;
    render();
  });

  afterEach(() => http.verify());

  it('Trigger_Render_WithSession_ShouldShowNameAndInitial', () => {
    expect(trigger().textContent).toContain('Иван');
    expect(trigger().getAttribute('aria-expanded')).toBe('false');
    expect(element.querySelector('.trigger .avatar')!.textContent).toBe('И');
    expect(element.querySelector('.panel')).toBeNull();
  });

  it('Panel_Open_WithSession_ShouldShowProfileEditAndLogout', () => {
    openPanel();

    expect(trigger().getAttribute('aria-expanded')).toBe('true');
    expect(element.querySelector('.profile .name')!.textContent).toBe('Иван');
    expect(element.querySelector('.profile .login')!.textContent).toBe('login-u1');
    const items = Array.from(element.querySelectorAll('.panel > button')).map((item) => item.textContent!.trim());
    expect(items).toEqual(['Изменить имя', 'Выйти']);
  });

  it('EditForm_Open_WithCurrentName_ShouldPrefillNameAndPutCancelFirst', () => {
    startEdit();

    expect(element.querySelector<HTMLInputElement>('[name=name]')!.value).toBe('Иван');
    const buttons = Array.from(element.querySelectorAll('.edit-form .buttons button')).map((item) => item.textContent!.trim());
    expect(buttons).toEqual(['Отмена', 'Сохранить']);
  });

  it('EditForm_Input_WithBlankName_ShouldDisableSave', () => {
    startEdit();

    inputName('   ');

    expect(button('Сохранить').disabled).toBe(true);
  });

  it('EditForm_Submit_WithPaddedName_ShouldSaveTrimmedNameAndUpdateSession', () => {
    startEdit();
    inputName('  Иван Петров  ');

    element.querySelector('form')!.dispatchEvent(new Event('submit'));
    const request = http.expectOne({ method: 'PUT', url: '/api/account/profile' });
    expect(request.request.body).toEqual({ name: 'Иван Петров' });
    request.flush(successBody({ id: 'u1', login: 'login-u1', name: 'Иван Петров' }, 2));
    render();

    expect(TestBed.inject(Auth).user()!.name).toBe('Иван Петров');
    expect(trigger().textContent).toContain('Иван Петров');
    expect(element.querySelector('.edit-form')).toBeNull();
  });

  it('EditForm_Submit_WithServerProblem_ShouldShowProblemAndKeepForm', () => {
    startEdit();
    inputName('Иван Петров');

    element.querySelector('form')!.dispatchEvent(new Event('submit'));
    http.expectOne('/api/account/profile').flush({ detail: 'Имя пользователя не может превышать 128 символов.' }, { status: 422, statusText: 'Unprocessable Entity' });
    render();

    expect(element.querySelector('[role=alert]')!.textContent).toContain('Имя пользователя не может превышать 128 символов.');
    expect(element.querySelector('.edit-form')).not.toBeNull();
  });

  it('Logout_Click_WithSession_ShouldEndSessionAndOpenLogin', () => {
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    openPanel();

    button('Выйти').click();
    http.expectOne({ method: 'POST', url: '/api/account/logout' }).flush(successBody({ isLoggedOut: true }, 2));

    expect(TestBed.inject(Auth).isAuthenticated()).toBe(false);
    expect(navigate).toHaveBeenCalledWith('/login');
  });

  it('Escape_Press_WithEditForm_ShouldCancelFormThenClosePanel', () => {
    startEdit();

    pressEscape();
    expect(element.querySelector('.edit-form')).toBeNull();
    expect(element.querySelector('.panel')).not.toBeNull();

    pressEscape();
    expect(element.querySelector('.panel')).toBeNull();
  });

  it('Document_Click_OutsideMenu_ShouldClosePanel', () => {
    openPanel();

    document.body.click();
    render();

    expect(element.querySelector('.panel')).toBeNull();
  });
});
