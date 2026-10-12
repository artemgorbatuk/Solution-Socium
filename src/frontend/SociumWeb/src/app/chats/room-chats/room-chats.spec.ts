import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { Subject } from 'rxjs';
import { successBody } from '../../shared/api/api-response.testing';
import { signInAs } from '../../account/auth.testing';
import { ChatHub } from '../../realtime/chat-hub';
import { ChatChangedNotify, ChatListNotify, UserChangedNotify } from '../../realtime/chat-hub.models';
import { ChatChanges } from '../chat-changes';
import { ChatListModel } from '../chat.models';
import { RoomChats } from './room-chats';

describe('RoomChats', () => {
  let fixture: ComponentFixture<RoomChats>;
  let http: HttpTestingController;
  let element: HTMLElement;
  let chatListUpdated$: Subject<ChatListNotify>;
  let chatChanged$: Subject<ChatChangedNotify>;
  let userChanged$: Subject<UserChangedNotify>;

  function render(): void {
    fixture.detectChanges();
  }

  function row(partial: Partial<ChatListModel> & Pick<ChatListModel, 'id' | 'name'>): ChatListModel {
    return { canUpdate: true, canDelete: true, unreadCount: 0, ...partial };
  }

  function listBody(rows: ChatListModel[]) {
    return successBody({ rowExists: rows.length > 0, rowCount: rows.length, rows });
  }

  function expectList() {
    return http.expectOne((request) => request.method === 'GET' && request.url === '/api/chat' && request.params.get('roomId') === '1');
  }

  function button(text: string): HTMLButtonElement {
    return Array.from(element.querySelectorAll('button')).find((item) => item.textContent!.includes(text))!;
  }

  function chatNames(): string[] {
    return Array.from(element.querySelectorAll('.chat-name')).map((item) => item.textContent!.trim());
  }

  function create(creating: boolean): void {
    fixture = TestBed.createComponent(RoomChats);
    fixture.componentRef.setInput('roomId', '1');
    fixture.componentRef.setInput('roomName', 'Кухня');
    fixture.componentRef.setInput('creating', creating);
    element = fixture.nativeElement;
    render();
  }

  function typeNewChatName(value: string): void {
    const field = element.querySelector<HTMLInputElement>('.create-form input')!;
    field.value = value;
    field.dispatchEvent(new Event('input'));
    render();
  }

  beforeEach(() => {
    chatListUpdated$ = new Subject();
    chatChanged$ = new Subject();
    userChanged$ = new Subject();
    TestBed.configureTestingModule({
      imports: [RoomChats],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ChatHub,
          useValue: {
            ensureConnected: () => Promise.resolve(),
            chatListUpdated$,
            chatChanged$,
            userChanged$,
          },
        },
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('List_Load_WithRoomChats_ShouldShowChatsInOrder', () => {
    create(false);
    expect(element.textContent).toContain('Загрузка…');

    expectList().flush(listBody([row({ id: 'c1', name: 'Арт' }), row({ id: 'c2', name: 'Общий' })]));
    render();

    // Без lastMessageAt — сортировка по Id убыв. (c2 «Общий» выше c1 «Арт»).
    expect(chatNames()).toEqual(['Общий', 'Арт']);
    expect(element.querySelector('ul')!.getAttribute('aria-label')).toBe('Чаты комнаты Кухня');
  });

  it('List_Load_WithNoChats_ShouldShowEmptyHint', () => {
    create(false);

    expectList().flush(listBody([]));
    render();

    expect(element.textContent).toContain('Чатов пока нет');
  });

  it('List_Load_WithServerError_ShouldShowProblem', () => {
    create(false);

    expectList().flush({ detail: 'Комната не найдена по указанному идентификатору.' }, { status: 404, statusText: 'Not Found' });
    render();

    expect(element.querySelector('[role=alert]')!.textContent).toContain('Комната не найдена по указанному идентификатору.');
  });

  it('CreateForm_Submit_WithPaddedName_ShouldSendTrimmedNameOpenChatAndPatchViaChatChanged', () => {
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    create(true);
    expectList().flush(listBody([]));
    render();

    typeNewChatName('  Общий  ');
    element.querySelector('form')!.dispatchEvent(new Event('submit'));
    const request = http.expectOne({ method: 'POST', url: '/api/chat' });
    expect(request.request.body).toEqual({ roomId: '1', name: 'Общий' });
    request.flush(successBody({ id: 'c1', roomId: '1', name: 'Общий' }, 2));
    render();

    expect(navigate).toHaveBeenCalledWith(['/chat', 'c1']);
    expect(fixture.componentInstance.creating()).toBe(false);
    expect(element.querySelector('.create-form')).toBeNull();
    expect(chatNames()).toEqual([]);

    chatChanged$.next({ kind: 'created', roomId: '1', chatId: 'c1', name: 'Общий' });
    render();

    expect(chatNames()).toEqual(['Общий']);
  });

  it('CreateForm_Submit_WithServerError_ShouldShowProblemAndKeepForm', () => {
    create(true);
    expectList().flush(listBody([]));
    render();

    typeNewChatName('Общий');
    element.querySelector('form')!.dispatchEvent(new Event('submit'));
    http
      .expectOne({ method: 'POST', url: '/api/chat' })
      .flush({ detail: 'Комната не найдена по указанному идентификатору.' }, { status: 404, statusText: 'Not Found' });
    render();

    expect(element.querySelector('.create-form')).not.toBeNull();
    expect(element.querySelector('[role=alert]')!.textContent).toContain('Комната не найдена по указанному идентификатору.');
  });

  it('CreateForm_Input_WithBlankName_ShouldDisableCreate', () => {
    create(true);
    expectList().flush(listBody([]));
    render();

    typeNewChatName('   ');

    expect(button('Создать').disabled).toBe(true);
  });

  it('CreateForm_Escape_WithDraftName_ShouldCloseForm', () => {
    create(true);
    expectList().flush(listBody([]));
    render();
    typeNewChatName('Общий');

    element.querySelector('.create-form input')!.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    render();

    expect(fixture.componentInstance.creating()).toBe(false);
    expect(element.querySelector('.create-form')).toBeNull();
  });

  it('ChatRename_Save_WithNewName_ShouldPatchViaChatChanged', () => {
    create(false);
    expectList().flush(listBody([row({ id: 'c1', name: 'Общий' })]));
    render();

    element.querySelector<HTMLButtonElement>('[aria-label="Переименовать чат Общий"]')!.click();
    render();
    const field = element.querySelector<HTMLInputElement>('.rename-form input')!;
    field.value = 'Объявления';
    field.dispatchEvent(new Event('input'));
    element.querySelector('.rename-form')!.dispatchEvent(new Event('submit'));
    http.expectOne({ method: 'PUT', url: '/api/chat' }).flush(successBody(null, 2));
    render();

    expect(chatNames()).toEqual(['Общий']);

    chatChanged$.next({ kind: 'updated', roomId: '1', chatId: 'c1', name: 'Объявления' });
    render();

    expect(TestBed.inject(ChatChanges).version()).toBe(0);
    expect(chatNames()).toEqual(['Объявления']);
  });

  it('ChatDelete_Confirm_WithExistingChat_ShouldDeleteAndPatchViaChatChanged', () => {
    create(false);
    expectList().flush(listBody([row({ id: 'c1', name: 'Общий' })]));
    render();

    element.querySelector<HTMLButtonElement>('[aria-label="Удалить чат Общий"]')!.click();
    render();
    http
      .expectOne((request) => request.method === 'GET' && request.url === '/api/chat/delete')
      .flush(successBody({ id: 'c1', roomId: '1', name: 'Общий' }));
    render();
    element.querySelector<HTMLButtonElement>('[role=group] .danger')!.click();
    http.expectOne({ method: 'DELETE', url: '/api/chat?id=c1' }).flush(successBody({ isDeleted: true }, 2));
    render();

    expect(element.querySelector('[role=group]')).not.toBeNull();

    chatChanged$.next({ kind: 'deleted', roomId: '1', chatId: 'c1' });
    render();

    expect(element.querySelector('[role=group]')).toBeNull();
    expect(TestBed.inject(ChatChanges).version()).toBe(0);
    expect(element.textContent).toContain('Чатов пока нет');
  });

  it('ChatChanged_Event_WithUpdatedAndDeleted_ShouldPatchList', () => {
    create(false);
    expectList().flush(listBody([row({ id: 'c1', name: 'Общий' }), row({ id: 'c2', name: 'Арт' })]));
    render();

    chatChanged$.next({ kind: 'updated', roomId: '1', chatId: 'c1', name: 'Объявления' });
    render();
    expect(chatNames()).toContain('Объявления');

    chatChanged$.next({ kind: 'deleted', roomId: '1', chatId: 'c2' });
    render();
    expect(chatNames()).toEqual(['Объявления']);
  });

  it('ChatChanged_Event_WithOtherRoom_ShouldIgnore', () => {
    create(false);
    expectList().flush(listBody([row({ id: 'c1', name: 'Общий' })]));
    render();

    chatChanged$.next({ kind: 'created', roomId: '2', chatId: 'c9', name: 'Чужой' });
    chatChanged$.next({ kind: 'deleted', roomId: '2', chatId: 'c1' });
    render();

    expect(chatNames()).toEqual(['Общий']);
  });

  it('List_Reload_WithCurrentUserChanged_ShouldLoadAdminFlagsOfNewUser', () => {
    create(false);
    expectList().flush(listBody([row({ id: 'c1', name: 'Общий' })]));
    render();

    signInAs('u2');
    render();
    expectList().flush(listBody([row({ id: 'c1', name: 'Общий', canUpdate: false, canDelete: false })]));
    render();

    expect(element.querySelector('.chat-name')!.textContent).toContain('Общий');
    expect(element.querySelector('.actions')).toBeNull();
  });

  it('List_Reload_WithChatChangesNotified_ShouldReloadList', () => {
    create(false);
    expectList().flush(listBody([]));
    render();

    TestBed.inject(ChatChanges).notify();
    render();
    expectList().flush(listBody([row({ id: 'c1', name: 'Общий', canUpdate: false, canDelete: false })]));
    render();

    expect(element.querySelector('.chat-name')!.textContent).toContain('Общий');
  });

  it('List_Realtime_WithChatListUpdated_ShouldPatchPreviewUnreadAndReorder', () => {
    create(false);
    expectList().flush(
      listBody([
        row({
          id: 'c2',
          name: 'Свежий',
          lastMessageAt: '2020-01-01T12:00:00Z',
          lastMessageText: 'середина',
        }),
        row({
          id: 'c1',
          name: 'Старый',
          lastMessageAt: '2020-01-01T00:00:00Z',
          lastMessageText: 'было',
        }),
      ]),
    );
    render();

    chatListUpdated$.next({
      chatId: 'c1',
      roomId: '1',
      targetUserId: 'u1',
      unreadCount: 2,
      lastMessageText: 'привет',
      lastMessageAt: '2020-01-02T00:00:00Z',
      lastMessageSenderName: 'Анна',
      lastMessageSenderUserId: 'u-anna',
      lastMessageHasAttachments: false,
    });
    render();

    expect(chatNames()).toEqual(['Старый', 'Свежий']);
    expect(element.querySelector('.chat-preview')!.textContent).toContain('Анна: привет');
    expect(element.querySelector('.unread')!.textContent).toBe('2');
  });

  it('List_Realtime_WithUserChanged_ShouldPatchLastMessageSenderName', () => {
    create(false);
    expectList().flush(
      listBody([
        row({
          id: 'c1',
          name: 'Общий',
          lastMessageAt: '2020-01-01T00:00:00Z',
          lastMessageText: 'привет',
          lastMessageSenderName: 'Анна',
          lastMessageSenderUserId: 'u-anna',
        }),
        row({
          id: 'c2',
          name: 'Другой',
          lastMessageAt: '2020-01-01T12:00:00Z',
          lastMessageText: 'ок',
          lastMessageSenderName: 'Борис',
          lastMessageSenderUserId: 'u-boris',
        }),
      ]),
    );
    render();

    userChanged$.next({ userId: 'u-anna', name: 'Анна Петрова' });
    render();

    expect(element.querySelector('.chat-preview')!.textContent).toContain('Борис: ок');
    const previews = Array.from(element.querySelectorAll('.chat-preview')).map((item) => item.textContent!.trim());
    expect(previews).toEqual(['Борис: ок', 'Анна Петрова: привет']);
  });
});
