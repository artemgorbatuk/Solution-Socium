import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ApiSuccessResponse } from '../../shared/api/api-response';
import { successBody } from '../../shared/api/api-response.testing';
import { currentUserStorageKey } from '../current-user';
import { UserListModel, UserListPageResponse } from '../user.models';
import { UserPicker } from './user-picker';

function listBody(rows: UserListModel[]): ApiSuccessResponse<UserListPageResponse> {
  return successBody({ rowExists: rows.length > 0, rowCount: rows.length, rows });
}

describe('UserPicker', () => {
  let fixture: ComponentFixture<UserPicker>;
  let http: HttpTestingController;
  let element: HTMLElement;

  const users: UserListModel[] = [
    { id: '1', login: 'ivan', name: 'Иван' },
    { id: '2', login: 'petr', name: 'Пётр' },
  ];

  function render(): void {
    fixture.detectChanges();
  }

  function create(savedUserId?: string): void {
    if (savedUserId) {
      localStorage.setItem(currentUserStorageKey, savedUserId);
    }
    fixture = TestBed.createComponent(UserPicker);
    element = fixture.nativeElement;
    render();
  }

  function trigger(): HTMLButtonElement {
    return element.querySelector<HTMLButtonElement>('.trigger')!;
  }

  function button(text: string): HTMLButtonElement {
    return Array.from(element.querySelectorAll('button')).find((item) => item.textContent!.includes(text))!;
  }

  function input(selector: string, value: string): void {
    const field = element.querySelector<HTMLInputElement>(selector)!;
    field.value = value;
    field.dispatchEvent(new Event('input'));
    render();
  }

  function openWith(rows: UserListModel[], savedUserId?: string): void {
    create(savedUserId);
    http.expectOne('/api/user').flush(listBody(rows));
    trigger().click();
    render();
  }

  function pressEscape(): void {
    element.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    render();
  }

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      imports: [UserPicker],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    localStorage.clear();
  });

  it('Trigger_Load_WithSavedUser_ShouldShowLoadingThenUserName', () => {
    create('2');
    expect(trigger().textContent).toContain('Загрузка…');

    http.expectOne('/api/user').flush(listBody(users));
    render();

    expect(trigger().textContent).toContain('Пётр');
    expect(element.querySelector('.trigger .avatar')!.textContent).toBe('П');
  });

  it('Trigger_Load_WithSavedUserMissingFromList_ShouldForgetUser', () => {
    create('deleted');

    http.expectOne('/api/user').flush(listBody(users));
    render();

    expect(trigger().textContent).toContain('Выберите пользователя');
    expect(localStorage.getItem(currentUserStorageKey)).toBeNull();
  });

  it('Panel_Open_WithUsers_ShouldListUsersAndMarkCurrent', () => {
    openWith(users, '1');

    expect(trigger().getAttribute('aria-expanded')).toBe('true');
    const rows = Array.from(element.querySelectorAll<HTMLButtonElement>('.panel .user'));
    expect(rows.map((row) => row.querySelector('.name')!.textContent)).toEqual(['Иван', 'Пётр']);
    expect(rows.map((row) => row.getAttribute('aria-current'))).toEqual(['true', null]);
  });

  it('Panel_Open_WithNoUsers_ShouldShowPlaceholder', () => {
    openWith([]);

    expect(element.querySelector('.panel')!.textContent).toContain('Пользователей пока нет');
  });

  it('Panel_Open_WithServerError_ShouldShowProblem', () => {
    create();
    http.expectOne('/api/user').flush({ detail: 'Не удалось отобразить страницу списка пользователей' }, { status: 500, statusText: 'Error' });
    trigger().click();
    render();

    expect(element.querySelector('[role=alert]')!.textContent).toContain('Не удалось отобразить страницу списка пользователей');
  });

  it('User_Click_WithOtherUser_ShouldSelectSaveAndClosePanel', () => {
    openWith(users, '1');

    button('Пётр').click();
    render();

    expect(localStorage.getItem(currentUserStorageKey)).toBe('2');
    expect(element.querySelector('.panel')).toBeNull();
    expect(trigger().textContent).toContain('Пётр');
  });

  it('AddForm_Open_WithPanel_ShouldKeepPanelOpenAndPutCancelFirst', () => {
    openWith(users);

    button('Добавить пользователя').click();
    render();

    expect(element.querySelector('.panel')).not.toBeNull();
    const buttons = Array.from(element.querySelectorAll('.add-form .buttons button')).map((item) => item.textContent!.trim());
    expect(buttons).toEqual(['Отмена', 'Создать']);
    expect(button('Создать').disabled).toBe(true);
  });

  it('AddForm_Input_WithBlankName_ShouldDisableCreate', () => {
    openWith(users);
    button('Добавить пользователя').click();
    render();

    input('[name=login]', 'anna');
    input('[name=name]', '   ');

    expect(button('Создать').disabled).toBe(true);
  });

  it('AddForm_Submit_WithPaddedFields_ShouldCreateTrimmedReloadAndSelectNewUser', () => {
    openWith(users, '1');
    button('Добавить пользователя').click();
    render();
    input('[name=login]', '  Anna  ');
    input('[name=name]', '  Анна  ');

    element.querySelector('form')!.dispatchEvent(new Event('submit'));

    const created = http.expectOne({ method: 'POST', url: '/api/user' });
    expect(created.request.body).toEqual({ login: 'Anna', name: 'Анна' });
    created.flush(successBody(null, 2));
    http.expectOne({ method: 'GET', url: '/api/user' }).flush(listBody([{ id: '3', login: 'anna', name: 'Анна' }, ...users]));
    render();

    expect(localStorage.getItem(currentUserStorageKey)).toBe('3');
    expect(element.querySelector('.panel')).toBeNull();
    expect(trigger().textContent).toContain('Анна');
  });

  it('AddForm_Submit_WithDuplicateLogin_ShouldShowProblemAndKeepForm', () => {
    openWith(users);
    button('Добавить пользователя').click();
    render();
    input('[name=login]', 'ivan');
    input('[name=name]', 'Другой Иван');

    element.querySelector('form')!.dispatchEvent(new Event('submit'));
    http
      .expectOne({ method: 'POST', url: '/api/user' })
      .flush({ detail: 'Пользователь с таким логином уже существует.' }, { status: 422, statusText: 'Unprocessable Entity' });
    render();

    expect(element.querySelector('.add-form')).not.toBeNull();
    expect(element.querySelector('[role=alert]')!.textContent).toContain('Пользователь с таким логином уже существует.');
  });

  it('Escape_Press_WithAddForm_ShouldCancelFormThenClosePanel', () => {
    openWith(users);
    button('Добавить пользователя').click();
    render();

    pressEscape();
    expect(element.querySelector('.add-form')).toBeNull();
    expect(element.querySelector('.panel')).not.toBeNull();

    pressEscape();
    expect(element.querySelector('.panel')).toBeNull();
  });

  it('Document_Click_OutsidePicker_ShouldClosePanel', () => {
    openWith(users);

    document.body.click();
    render();

    expect(element.querySelector('.panel')).toBeNull();
  });
});
