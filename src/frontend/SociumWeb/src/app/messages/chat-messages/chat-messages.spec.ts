import { HttpRequest, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { successBody } from '../../shared/api/api-response.testing';
import { CurrentUser, currentUserStorageKey } from '../../users/current-user';
import { MessageListModel } from '../message.models';
import { ChatMessages } from './chat-messages';

describe('ChatMessages', () => {
  const first: MessageListModel = { id: 'm1', text: 'Первое', createdAt: '2020-01-15T12:00:00Z', senderUserId: 'u1', senderName: 'Анна' };
  const second: MessageListModel = { id: 'm2', text: 'Второе', createdAt: '2020-01-15T12:01:00Z', senderUserId: 'u2', senderName: 'Борис' };

  let fixture: ComponentFixture<ChatMessages>;
  let http: HttpTestingController;
  let element: HTMLElement;

  function render(): void {
    fixture.detectChanges();
  }

  function isList(chatId: string) {
    return (request: HttpRequest<unknown>) =>
      request.method === 'GET' && request.url === '/api/message' && request.params.get('chatId') === chatId;
  }

  function flushList(chatId: string, rows: MessageListModel[]): void {
    http.expectOne(isList(chatId)).flush(successBody({ rowExists: rows.length > 0, rowCount: rows.length, rows }));
    render();
  }

  function textarea(): HTMLTextAreaElement {
    return element.querySelector<HTMLTextAreaElement>('.composer textarea')!;
  }

  function sendButton(): HTMLButtonElement {
    return element.querySelector<HTMLButtonElement>('button[aria-label="Отправить"]')!;
  }

  function type(value: string): void {
    textarea().value = value;
    textarea().dispatchEvent(new Event('input'));
    render();
  }

  function press(init: KeyboardEventInit): KeyboardEvent {
    const event = new KeyboardEvent('keydown', { ...init, cancelable: true });
    textarea().dispatchEvent(event);
    render();
    return event;
  }

  function texts(): string[] {
    return Array.from(element.querySelectorAll('.text')).map((item) => item.textContent!);
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ChatMessages],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    TestBed.inject(CurrentUser).select('u1');
    fixture = TestBed.createComponent(ChatMessages);
    fixture.componentRef.setInput('chatId', 'c1');
    fixture.componentRef.setInput('chatName', 'Общий');
    element = fixture.nativeElement;
    render();
  });

  afterEach(() => {
    http.verify();
    localStorage.removeItem(currentUserStorageKey);
  });

  it('Feed_Load_WithMessages_ShouldShowTextsInSendOrder', () => {
    expect(element.textContent).toContain('Загрузка…');

    flushList('c1', [first, second]);

    expect(texts()).toEqual(['Первое', 'Второе']);
    expect(element.querySelector('ol')!.getAttribute('aria-label')).toBe('Сообщения чата Общий');
  });

  it('Feed_Load_WithOwnAndOtherMessages_ShouldMarkOnlyCurrentUserMessagesAsOwn', () => {
    flushList('c1', [first, second]);

    const rows = Array.from(element.querySelectorAll('.row'));
    expect(rows.map((row) => row.classList.contains('own'))).toEqual([true, false]);
    expect(rows[1].querySelector('.sender')!.textContent).toContain('Борис');
    expect(element.querySelectorAll('.actions').length).toBe(1);
  });

  it('Feed_Load_WithEmptyChat_ShouldShowEmptyState', () => {
    flushList('c1', []);

    expect(element.textContent).toContain('Сообщений пока нет');
  });

  it('Feed_Load_WithServerError_ShouldShowProblem', () => {
    http.expectOne(isList('c1')).flush({ detail: 'База недоступна.' }, { status: 500, statusText: 'Server Error' });
    render();

    expect(element.querySelector('[role=alert]')!.textContent).toContain('База недоступна.');
  });

  it('Composer_Input_WithBlankOrFilledText_ShouldToggleSendButton', () => {
    flushList('c1', []);
    expect(sendButton().disabled).toBe(true);

    type('  \n ');
    expect(sendButton().disabled).toBe(true);

    type('Привет');
    expect(sendButton().disabled).toBe(false);
  });

  it('Composer_Enter_WithPaddedMultilineText_ShouldSendTrimmedTextClearAndReload', () => {
    flushList('c1', [first]);
    type('  Строка 1\nСтрока 2  ');

    const event = press({ key: 'Enter' });

    expect(event.defaultPrevented).toBe(true);
    const request = http.expectOne({ method: 'POST', url: '/api/message' });
    expect(request.request.body).toEqual({ chatId: 'c1', text: 'Строка 1\nСтрока 2' });
    request.flush(successBody(null, 2));
    render();
    flushList('c1', [first, { ...first, id: 'm3', text: 'Строка 1\nСтрока 2', createdAt: '2020-01-15T12:05:00Z' }]);

    expect(textarea().value).toBe('');
    expect(texts()).toEqual(['Первое', 'Строка 1\nСтрока 2']);
  });

  it('Composer_ShiftEnter_WithText_ShouldNotSend', () => {
    flushList('c1', []);
    type('Привет');

    const event = press({ key: 'Enter', shiftKey: true });

    expect(event.defaultPrevented).toBe(false);
    http.expectNone({ method: 'POST', url: '/api/message' });
  });

  it('Composer_Enter_WithBlankText_ShouldNotSend', () => {
    flushList('c1', []);
    type('   ');

    press({ key: 'Enter' });

    http.expectNone({ method: 'POST', url: '/api/message' });
  });

  it('Composer_Submit_WithServerError_ShouldShowProblemAndKeepText', () => {
    flushList('c1', []);
    type('Привет');

    sendButton().click();
    http
      .expectOne({ method: 'POST', url: '/api/message' })
      .flush({ detail: 'Чат не найден по указанному идентификатору.' }, { status: 404, statusText: 'Not Found' });
    render();

    expect(element.querySelector('.send-error')!.textContent).toContain('Чат не найден по указанному идентификатору.');
    expect(textarea().value).toBe('Привет');
  });

  it('Message_Delete_WithConfirm_ShouldReloadFeed', () => {
    flushList('c1', [first]);
    element.querySelector<HTMLButtonElement>('[aria-label="Удалить сообщение"]')!.click();
    render();

    http.expectOne({ method: 'GET', url: '/api/message/delete?id=m1' }).flush(successBody({ ...first, chatId: 'c1' }));
    render();
    element.querySelector<HTMLButtonElement>('[role=group] .danger')!.click();
    http.expectOne({ method: 'DELETE', url: '/api/message?id=m1' }).flush(successBody({ isDeleted: true }, 2));
    render();
    flushList('c1', []);

    expect(element.querySelector('[role=group]')).toBeNull();
    expect(element.textContent).toContain('Сообщений пока нет');
  });

  it('Chat_Switch_WithTypedText_ShouldClearTextAndLoadOtherChat', () => {
    flushList('c1', [first]);
    type('Черновик');

    fixture.componentRef.setInput('chatId', 'c2');
    render();
    flushList('c2', [second]);

    expect(textarea().value).toBe('');
    expect(texts()).toEqual(['Второе']);
  });
});
