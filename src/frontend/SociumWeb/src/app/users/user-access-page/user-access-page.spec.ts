import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { Subject } from 'rxjs';
import { signInAsAppAdmin } from '../../account/auth.testing';
import { ChatHub } from '../../realtime/chat-hub';
import { successBody } from '../../shared/api/api-response.testing';
import { RoleAssignmentListPageResponse, roleNames } from '../user-access.models';
import { UserAdminListModel } from '../user.models';
import { userAccessReloadDebounceMs } from '../users-page/users-page';
import { UserAccessPage, userAccessReasons } from './user-access-page';

const anna: UserAdminListModel = {
  id: 'u2',
  login: 'anna',
  name: 'Анна Петрова',
  email: 'anna@example.com',
  isEmailConfirmed: true,
  isDeleted: false,
  isAppAdmin: false,
  canGrantAppAdmin: true,
  canRevokeAppAdmin: false,
};

function annaTree(overrides: Partial<RoleAssignmentListPageResponse> = {}): RoleAssignmentListPageResponse {
  return {
    userId: 'u2',
    canManage: true,
    rooms: [
      {
        roomId: 'r1',
        roomName: 'Тест 1',
        isRoomAdmin: true,
        isLastRoomAdmin: false,
        chats: [
          { chatId: 'c1', chatName: 'Чат 1', isParticipant: true, isChatAdmin: true, isLastChatAdmin: false },
          { chatId: 'c2', chatName: 'Чат 2', isParticipant: false, isChatAdmin: false, isLastChatAdmin: false },
        ],
      },
      {
        roomId: 'r2',
        roomName: 'Тест 2',
        isRoomAdmin: false,
        isLastRoomAdmin: false,
        chats: [{ chatId: 'c3', chatName: 'Чат 3', isParticipant: false, isChatAdmin: false, isLastChatAdmin: false }],
      },
    ],
    ...overrides,
  };
}

describe('UserAccessPage', () => {
  let fixture: ComponentFixture<UserAccessPage>;
  let http: HttpTestingController;
  let element: HTMLElement;
  let hub: Record<string, unknown>;

  function render(): void {
    fixture.detectChanges();
  }

  function flushLoad(user: UserAdminListModel | null = anna, tree = annaTree()): void {
    http
      .expectOne(
        (request) =>
          request.method === 'GET' &&
          request.url === '/api/user/admin' &&
          request.params.get('includeDeleted') === 'true' &&
          request.params.getAll('ids')?.join() === 'u2',
      )
      .flush(successBody({ rowExists: !!user, rowCount: user ? 1 : 0, rows: user ? [user] : [] }));
    http
      .expectOne((request) => request.method === 'GET' && request.url === '/api/roleAssignment' && request.params.get('userId') === 'u2')
      .flush(successBody(tree));
    render();
  }

  function checkbox(label: string): HTMLInputElement {
    return element.querySelector<HTMLInputElement>(`input[aria-label="${label}"]`)!;
  }

  function appAdminToggle(): HTMLInputElement {
    return element.querySelector<HTMLInputElement>('section[aria-label="Приложение"] input')!;
  }

  function click(input: HTMLInputElement): void {
    input.checked = !input.checked;
    input.dispatchEvent(new Event('change'));
    render();
  }

  function roomNames(): string[] {
    return Array.from(element.querySelectorAll('.room-name')).map((item) => item.textContent!.trim());
  }

  function chatNames(): string[] {
    return Array.from(element.querySelectorAll('.chat-name')).map((item) => item.textContent!.trim());
  }

  beforeEach(() => {
    hub = {
      ensureConnected: () => Promise.resolve(),
      userAccessChanged$: new Subject(),
      userChanged$: new Subject(),
      roomChanged$: new Subject(),
      chatChanged$: new Subject(),
    };
    TestBed.configureTestingModule({
      imports: [UserAccessPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([]), { provide: ChatHub, useValue: hub }],
    });
    http = TestBed.inject(HttpTestingController);
    signInAsAppAdmin('u1');
    fixture = TestBed.createComponent(UserAccessPage);
    fixture.componentRef.setInput('id', 'u2');
    element = fixture.nativeElement;
    render();
  });

  afterEach(() => {
    vi.useRealTimers();
    http.verify();
  });

  it('Tree_Load_WithAccess_ShouldShowHeaderAndOnlyNodesWithAccess', () => {
    flushLoad();

    expect(element.querySelector('h1')!.textContent).toContain('Анна Петрова');
    expect(element.querySelector('.profile')!.textContent).toContain('@anna');
    expect(element.querySelector('.profile')!.textContent).toContain('anna@example.com');
    expect(appAdminToggle().checked).toBe(false);
    expect(appAdminToggle().disabled).toBe(false);
    expect(roomNames()).toEqual(['Тест 1']);
    expect(chatNames()).toEqual(['# Чат 1']);
    expect(checkbox('Админ комнаты Тест 1').checked).toBe(true);
    expect(checkbox('Участник Чат 1').checked).toBe(true);
    expect(checkbox('Админ чата Чат 1').checked).toBe(true);
  });

  it('Filter_Toggle_WithoutOnlyWithAccess_ShouldShowAllRoomsAndChats', () => {
    flushLoad();

    click(element.querySelector<HTMLInputElement>('.filters .toggle input')!);

    expect(roomNames()).toEqual(['Тест 1', 'Тест 2']);
    expect(chatNames()).toEqual(['# Чат 1', '# Чат 2', '# Чат 3']);
    expect(checkbox('Админ чата Чат 2').disabled).toBe(true);
    expect(checkbox('Админ чата Чат 2').closest('label')!.title).toBe(userAccessReasons.notParticipant);
  });

  it('Search_Input_WithChatName_ShouldKeepRoomWithMatchingChat', () => {
    flushLoad();
    click(element.querySelector<HTMLInputElement>('.filters .toggle input')!);

    const search = element.querySelector<HTMLInputElement>('.search')!;
    search.value = 'чат 3';
    search.dispatchEvent(new Event('input'));
    render();

    expect(roomNames()).toEqual(['Тест 2']);
    expect(chatNames()).toEqual(['# Чат 3']);
  });

  it('Toggles_Render_WithOwnCard_ShouldDisableAllExceptOwnAppAdminRevoke', () => {
    flushLoad(
      { ...anna, id: 'u2', isAppAdmin: true, canGrantAppAdmin: false, canRevokeAppAdmin: true },
      annaTree({ canManage: false }),
    );
    signInAsAppAdmin('u2');
    render();

    expect(element.querySelector('h1')!.textContent).toContain('(вы)');
    expect(appAdminToggle().disabled).toBe(false);
    expect(checkbox('Админ комнаты Тест 1').disabled).toBe(true);
    expect(checkbox('Админ комнаты Тест 1').closest('label')!.title).toBe(userAccessReasons.self);
    expect(checkbox('Участник Чат 1').disabled).toBe(true);
  });

  it('Toggles_Render_WithLastAdmins_ShouldDisableRevokeWithReason', () => {
    const tree = annaTree();
    tree.rooms[0] = {
      ...tree.rooms[0],
      isLastRoomAdmin: true,
      chats: [{ ...tree.rooms[0].chats[0], isLastChatAdmin: true }],
    };
    flushLoad({ ...anna, isAppAdmin: true, canGrantAppAdmin: false, canRevokeAppAdmin: false }, tree);

    expect(appAdminToggle().disabled).toBe(true);
    expect(appAdminToggle().closest('label')!.title).toBe(userAccessReasons.lastAppAdmin);
    expect(checkbox('Админ комнаты Тест 1').closest('label')!.title).toBe(userAccessReasons.lastRoomAdmin);
    expect(checkbox('Участник Чат 1').disabled).toBe(true);
    expect(checkbox('Админ чата Чат 1').closest('label')!.title).toBe(userAccessReasons.lastChatAdmin);
  });

  it('Participant_Toggle_WithoutAccess_ShouldCreateAndReloadTree', () => {
    flushLoad();
    click(element.querySelector<HTMLInputElement>('.filters .toggle input')!);

    const participant = checkbox('Участник Чат 2');
    click(participant);

    expect(participant.checked).toBe(false);
    expect(participant.disabled).toBe(true);
    const create = http.expectOne({ method: 'POST', url: '/api/roleAssignment' });
    expect(create.request.body).toEqual({ userId: 'u2', roleName: roleNames.chatParticipant, chatId: 'c2' });
    create.flush(successBody(null, 2));

    const tree = annaTree();
    tree.rooms[0].chats[1] = { ...tree.rooms[0].chats[1], isParticipant: true };
    flushLoad(anna, tree);

    expect(checkbox('Участник Чат 2').checked).toBe(true);
    expect(checkbox('Админ чата Чат 2').disabled).toBe(false);
  });

  it('Participant_Uncheck_WithChatAdmin_ShouldConfirmThenDelete', () => {
    flushLoad();

    click(checkbox('Участник Чат 1'));
    http.expectNone({ method: 'DELETE' });
    expect(element.querySelector('.confirm')!.textContent).toContain('Исключить из чата «Чат 1»? Роль админа тоже будет снята');

    Array.from(element.querySelectorAll<HTMLButtonElement>('.confirm button'))
      .find((button) => button.textContent!.trim() === 'Исключить')!
      .click();
    render();

    const remove = http.expectOne((request) => request.method === 'DELETE' && request.url === '/api/roleAssignment');
    expect(remove.request.params.get('userId')).toBe('u2');
    expect(remove.request.params.get('roleName')).toBe(roleNames.chatParticipant);
    expect(remove.request.params.get('chatId')).toBe('c1');
    remove.flush(successBody(null, 2));
    flushLoad();
  });

  it('RoomAdmin_Uncheck_WithServerError_ShouldShowErrorUnderRowAndKeepState', () => {
    flushLoad();

    const roomAdmin = checkbox('Админ комнаты Тест 1');
    click(roomAdmin);
    http
      .expectOne((request) => request.method === 'DELETE' && request.url === '/api/roleAssignment')
      .flush({ detail: 'Нельзя снять последнего админа комнаты.' }, { status: 422, statusText: 'Unprocessable Entity' });
    render();

    expect(roomAdmin.checked).toBe(true);
    expect(roomAdmin.disabled).toBe(false);
    expect(element.querySelector('.room .row-error')!.textContent).toContain('Нельзя снять последнего админа комнаты.');
  });

  it('AppAdmin_Toggle_WithOtherUser_ShouldGrantViaAppRole', () => {
    flushLoad();

    click(appAdminToggle());

    const grant = http.expectOne({ method: 'POST', url: '/api/appRole' });
    expect(grant.request.body).toEqual({ userId: 'u2', roleName: roleNames.appAdmin });
    grant.flush(successBody(null, 2));
    flushLoad({ ...anna, isAppAdmin: true, canGrantAppAdmin: false, canRevokeAppAdmin: true });

    expect(appAdminToggle().checked).toBe(true);
  });

  it('Realtime_RoomChangedBurst_ShouldReloadTreeOnce', () => {
    vi.useFakeTimers();
    flushLoad();

    (hub['roomChanged$'] as Subject<unknown>).next({ kind: 'created', roomId: 'r3', name: 'Тест 3' });
    (hub['userAccessChanged$'] as Subject<unknown>).next({ userId: 'u5' });
    vi.advanceTimersByTime(userAccessReloadDebounceMs);

    flushLoad();
  });

  it('Reload_WithForbidden_ShouldNavigateHome', () => {
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);

    http.expectOne((request) => request.url === '/api/user/admin').flush({ detail: 'Нет прав' }, { status: 403, statusText: 'Forbidden' });
    http.match((request) => request.url === '/api/roleAssignment');

    expect(navigate).toHaveBeenCalledWith('/');
  });

  it('Load_WithUnknownUser_ShouldShowNotFound', () => {
    flushLoad(null);

    expect(element.textContent).toContain('Пользователь не найден');
  });
});
