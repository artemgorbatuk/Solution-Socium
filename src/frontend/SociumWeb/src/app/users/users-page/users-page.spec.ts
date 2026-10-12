import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Subject } from 'rxjs';
import { signInAsAppAdmin } from '../../account/auth.testing';
import { TopbarTitle, chatCloseLabel } from '../../layout/topbar/topbar-title';
import { ChatHub } from '../../realtime/chat-hub';
import { UserAccessChangedNotify, UserChangedNotify } from '../../realtime/chat-hub.models';
import { successBody } from '../../shared/api/api-response.testing';
import { UserAdminListModel } from '../user.models';
import { usersCloseLabel, usersTopbarTitle } from '../users-topbar-title';
import { UsersPage, userAccessReloadDebounceMs } from './users-page';

function userRow(id: string, name: string, overrides: Partial<UserAdminListModel> = {}): UserAdminListModel {
  return {
    id,
    login: `login-${id}`,
    name,
    email: `${id}@example.com`,
    isEmailConfirmed: true,
    isDeleted: false,
    isAppAdmin: false,
    canGrantAppAdmin: true,
    canRevokeAppAdmin: false,
    ...overrides,
  };
}

describe('UsersPage', () => {
  let fixture: ComponentFixture<UsersPage>;
  let http: HttpTestingController;
  let element: HTMLElement;
  let userAccessChanged$: Subject<UserAccessChangedNotify>;
  let userChanged$: Subject<UserChangedNotify>;

  const rows = [
    userRow('u1', 'Админ', { isAppAdmin: true, canGrantAppAdmin: false }),
    userRow('u2', 'Анна Петрова', { isEmailConfirmed: false }),
    userRow('u3', 'Борис', { isDeleted: true, canGrantAppAdmin: false }),
  ];

  function render(): void {
    fixture.detectChanges();
  }

  function names(): string[] {
    return Array.from(element.querySelectorAll('tbody .name')).map((item) => item.textContent!.trim());
  }

  function flushList(list = rows): void {
    http
      .expectOne(
        (request) =>
          request.method === 'GET' && request.url === '/api/user/admin' && request.params.get('includeDeleted') === 'true',
      )
      .flush(successBody({ rowExists: list.length > 0, rowCount: list.length, rows: list }));
    render();
  }

  beforeEach(() => {
    userAccessChanged$ = new Subject<UserAccessChangedNotify>();
    userChanged$ = new Subject<UserChangedNotify>();
    TestBed.configureTestingModule({
      imports: [UsersPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ChatHub, useValue: { ensureConnected: () => Promise.resolve(), userAccessChanged$, userChanged$ } },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    signInAsAppAdmin('u1');
  });

  afterEach(() => {
    vi.useRealTimers();
    http.verify();
  });

  function create(): void {
    fixture = TestBed.createComponent(UsersPage);
    element = fixture.nativeElement;
    render();
  }

  it('List_Load_WithUsers_ShouldShowActiveUsersWithBadgesAndLinks', () => {
    create();
    flushList();

    expect(names()).toEqual(['Админ', 'Анна Петрова']);
    expect(Array.from(element.querySelectorAll('thead th')).map((th) => th.textContent)).toEqual([
      'Имя',
      'Логин',
      'Email',
      'Подтверждение',
      'Роли',
    ]);
    expect(element.querySelector('tbody tr')!.textContent).toContain('админ приложения');
    expect(element.querySelector('tbody tr')!.textContent).toContain('(вы)');
    expect(element.querySelectorAll('tbody tr')[1].querySelectorAll('td')[3].textContent).toContain('не подтверждён');
    expect(element.querySelectorAll('tbody tr')[1].querySelectorAll('td')[2].textContent).not.toContain('не подтверждён');
    expect(element.querySelector<HTMLAnchorElement>('tbody .name')!.getAttribute('href')).toBe('/users/u1');
  });

  it('ShowDeleted_Toggle_WithDeletedUser_ShouldShowDeletedMark', () => {
    create();
    flushList();

    const toggle = element.querySelector<HTMLInputElement>('.toggle input')!;
    toggle.checked = true;
    toggle.dispatchEvent(new Event('change'));
    render();

    expect(names()).toEqual(['Админ', 'Анна Петрова', 'Борис']);
    expect(element.querySelector('tr.deleted')!.textContent).toContain('удалён');
  });

  it('Search_Input_WithLoginOrName_ShouldFilterIgnoringCase', () => {
    create();
    flushList();

    const search = element.querySelector<HTMLInputElement>('.search')!;
    search.value = 'АННА';
    search.dispatchEvent(new Event('input'));
    render();
    expect(names()).toEqual(['Анна Петрова']);

    search.value = 'login-u1';
    search.dispatchEvent(new Event('input'));
    render();
    expect(names()).toEqual(['Админ']);
  });

  it('List_Load_WithServerError_ShouldShowProblem', () => {
    create();
    http
      .expectOne((request) => request.url === '/api/user/admin')
      .flush({ detail: 'Недостаточно прав для просмотра пользователей.' }, { status: 403, statusText: 'Forbidden' });
    render();

    expect(element.querySelector('[role=alert]')!.textContent).toContain('Недостаточно прав');
  });

  it('Page_CreateAndDestroy_ShouldShowUsersTopbarTitleThenClear', () => {
    create();
    flushList();
    const title = TestBed.inject(TopbarTitle);

    expect(title.text()).toBe(usersTopbarTitle);
    expect(title.closeLabel()).toBe(usersCloseLabel);

    fixture.destroy();

    expect(title.text()).toBeNull();
    expect(title.closeLabel()).toBe(chatCloseLabel);
  });

  it('UserAccessChanged_Burst_ShouldReloadListOnce', () => {
    vi.useFakeTimers();
    create();
    flushList();

    userAccessChanged$.next({ userId: 'u2' });
    userChanged$.next({ userId: 'u2', name: 'Анна' });
    userAccessChanged$.next({ userId: 'u4' });
    vi.advanceTimersByTime(userAccessReloadDebounceMs);

    flushList([...rows, userRow('u4', 'Вера')]);
    expect(names()).toEqual(['Админ', 'Анна Петрова', 'Вера']);
  });
});
