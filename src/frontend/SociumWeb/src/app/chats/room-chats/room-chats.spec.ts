import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { successBody } from '../../shared/api/api-response.testing';
import { CurrentUser, currentUserStorageKey } from '../../users/current-user';
import { ChatChanges } from '../chat-changes';
import { ChatListModel } from '../chat.models';
import { RoomChats } from './room-chats';

describe('RoomChats', () => {
  let fixture: ComponentFixture<RoomChats>;
  let http: HttpTestingController;
  let element: HTMLElement;

  function render(): void {
    fixture.detectChanges();
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
    TestBed.configureTestingModule({
      imports: [RoomChats],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    localStorage.removeItem(currentUserStorageKey);
  });

  it('List_Load_WithRoomChats_ShouldShowChatsInOrder', () => {
    create(false);
    expect(element.textContent).toContain('Загрузка…');

    expectList().flush(listBody([{ id: 'c1', name: 'Арт', isAdmin: true }, { id: 'c2', name: 'Общий', isAdmin: true }]));
    render();

    const names = Array.from(element.querySelectorAll('.chat-name')).map((item) => item.textContent!.trim());
    expect(names).toEqual(['Арт', 'Общий']);
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

  it('CreateForm_Submit_WithPaddedName_ShouldSendTrimmedNameCloseFormAndReloadList', () => {
    create(true);
    expectList().flush(listBody([]));
    render();

    typeNewChatName('  Общий  ');
    element.querySelector('form')!.dispatchEvent(new Event('submit'));
    const request = http.expectOne({ method: 'POST', url: '/api/chat' });
    expect(request.request.body).toEqual({ roomId: '1', name: 'Общий' });
    request.flush(successBody(null, 2));
    expectList().flush(listBody([{ id: 'c1', name: 'Общий', isAdmin: true }]));
    render();

    expect(fixture.componentInstance.creating()).toBe(false);
    expect(element.querySelector('.create-form')).toBeNull();
    expect(element.querySelector('.chat-name')!.textContent).toContain('Общий');
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

  it('ChatRename_Save_WithNewName_ShouldNotifyChangesAndReloadList', () => {
    create(false);
    expectList().flush(listBody([{ id: 'c1', name: 'Общий', isAdmin: true }]));
    render();

    element.querySelector<HTMLButtonElement>('[aria-label="Переименовать чат Общий"]')!.click();
    render();
    const field = element.querySelector<HTMLInputElement>('.rename-form input')!;
    field.value = 'Объявления';
    field.dispatchEvent(new Event('input'));
    element.querySelector('.rename-form')!.dispatchEvent(new Event('submit'));
    http.expectOne({ method: 'PUT', url: '/api/chat' }).flush(successBody(null, 2));
    render();
    expectList().flush(listBody([{ id: 'c1', name: 'Объявления', isAdmin: true }]));
    render();

    expect(TestBed.inject(ChatChanges).version()).toBe(1);
    expect(element.querySelector('.chat-name')!.textContent).toContain('Объявления');
  });

  it('ChatDelete_Confirm_WithExistingChat_ShouldDeleteNotifyChangesAndReloadList', () => {
    create(false);
    expectList().flush(listBody([{ id: 'c1', name: 'Общий', isAdmin: true }]));
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
    expectList().flush(listBody([]));
    render();

    expect(element.querySelector('[role=group]')).toBeNull();
    expect(TestBed.inject(ChatChanges).version()).toBe(1);
    expect(element.textContent).toContain('Чатов пока нет');
  });

  it('List_Reload_WithCurrentUserChanged_ShouldLoadAdminFlagsOfNewUser', () => {
    create(false);
    expectList().flush(listBody([{ id: 'c1', name: 'Общий', isAdmin: true }]));
    render();

    TestBed.inject(CurrentUser).select('u2');
    render();
    expectList().flush(listBody([{ id: 'c1', name: 'Общий', isAdmin: false }]));
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
    expectList().flush(listBody([{ id: 'c1', name: 'Общий', isAdmin: false }]));
    render();

    expect(element.querySelector('.chat-name')!.textContent).toContain('Общий');
  });
});
