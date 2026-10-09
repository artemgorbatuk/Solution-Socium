import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { successBody } from '../../shared/api/api-response.testing';
import { MessageListModel } from '../message.models';
import { MessageItem } from './message-item';

describe('MessageItem', () => {
  const message: MessageListModel = {
    id: 'm1',
    text: 'Привет\nмир',
    createdAt: '2020-01-15T12:02:00Z',
    senderUserId: 'u1',
    senderName: 'Анна',
  };

  let fixture: ComponentFixture<MessageItem>;
  let http: HttpTestingController;
  let element: HTMLElement;
  let changed: number;
  let deleted: number;

  function render(): void {
    fixture.detectChanges();
  }

  function button(text: string): HTMLButtonElement {
    return Array.from(element.querySelectorAll('button')).find((item) => item.textContent!.includes(text))!;
  }

  function field(): HTMLTextAreaElement {
    return element.querySelector<HTMLTextAreaElement>('.edit-form textarea')!;
  }

  function action(label: string): HTMLButtonElement {
    return element.querySelector<HTMLButtonElement>(`.actions button[aria-label="${label}"]`)!;
  }

  function startEdit(value: string): void {
    action('Изменить сообщение').click();
    render();
    field().value = value;
    field().dispatchEvent(new Event('input'));
    render();
  }

  function press(init: KeyboardEventInit): KeyboardEvent {
    const event = new KeyboardEvent('keydown', { ...init, cancelable: true });
    field().dispatchEvent(event);
    render();
    return event;
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [MessageItem],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(MessageItem);
    fixture.componentRef.setInput('message', message);
    fixture.componentRef.setInput('own', true);
    changed = 0;
    deleted = 0;
    fixture.componentInstance.changed.subscribe(() => changed++);
    fixture.componentInstance.deleted.subscribe(() => deleted++);
    element = fixture.nativeElement;
    render();
  });

  afterEach(() => http.verify());

  it('Message_Render_WithMultilineText_ShouldKeepLineBreaksAndShowTime', () => {
    const time = element.querySelector<HTMLTimeElement>('time')!;

    expect(element.querySelector('.text')!.textContent).toBe('Привет\nмир');
    expect(time.getAttribute('datetime')).toBe(message.createdAt);
    expect(time.textContent).toContain('15.01.2020');
    expect(time.title).toContain('января 2020');
  });

  it('Message_Render_WithExistingMessage_ShouldShowActionButtonsAfterBubble', () => {
    const buttons = Array.from(element.querySelectorAll<HTMLButtonElement>('.actions button'));

    expect(buttons.map((item) => item.getAttribute('aria-label'))).toEqual(['Изменить сообщение', 'Удалить сообщение']);
    expect(buttons.map((item) => item.title)).toEqual(['Изменить', 'Удалить']);
    expect(buttons.map((item) => item.querySelector('app-icon svg')!.getAttribute('data-icon'))).toEqual(['pencil', 'trash']);
    expect(buttons[1].classList).toContain('danger');
    expect(element.querySelector('.row')!.lastElementChild!.classList).toContain('actions');
    expect(element.querySelector('[role=menu]')).toBeNull();
  });

  it('Message_Render_WithOwnMessage_ShouldAlignRightWithSenderName', () => {
    expect(element.querySelector('.row')!.classList).toContain('own');
    expect(element.querySelector('.sender')!.textContent).toBe('Анна');
  });

  it('Message_Render_WithOtherSender_ShouldAlignLeftWithSenderNameAndNoActions', () => {
    fixture.componentRef.setInput('own', false);
    render();

    expect(element.querySelector('.row')!.classList).not.toContain('own');
    expect(element.querySelector('.sender')!.textContent).toBe('Анна');
    expect(element.querySelector('.text')!.textContent).toBe('Привет\nмир');
    expect(element.querySelector('.actions')).toBeNull();
  });

  it('Message_Render_WithEditForm_ShouldHideActionButtons', () => {
    startEdit('Новый текст');

    expect(element.querySelector('.actions')).toBeNull();
  });

  it('EditForm_Enter_WithPaddedText_ShouldSendTrimmedTextAndEmitChanged', () => {
    startEdit('  Строка 1\nСтрока 2  ');

    const event = press({ key: 'Enter' });

    expect(event.defaultPrevented).toBe(true);
    const request = http.expectOne({ method: 'PUT', url: '/api/message' });
    expect(request.request.body).toEqual({ id: 'm1', text: 'Строка 1\nСтрока 2' });
    request.flush(successBody(null, 2));
    render();

    expect(changed).toBe(1);
    expect(element.querySelector('.edit-form')).toBeNull();
  });

  it('EditForm_ShiftEnter_WithText_ShouldKeepEditingWithoutRequest', () => {
    startEdit('Новый текст');

    const event = press({ key: 'Enter', shiftKey: true });

    expect(event.defaultPrevented).toBe(false);
    http.expectNone({ method: 'PUT', url: '/api/message' });
    expect(element.querySelector('.edit-form')).not.toBeNull();
  });

  it('EditForm_Submit_WithUnchangedText_ShouldCloseWithoutRequest', () => {
    startEdit(' Привет\nмир ');

    element.querySelector('form')!.dispatchEvent(new Event('submit'));
    render();

    http.expectNone({ method: 'PUT', url: '/api/message' });
    expect(element.querySelector('.edit-form')).toBeNull();
    expect(changed).toBe(0);
  });

  it('EditForm_Submit_WithServerError_ShouldShowProblemAndKeepForm', () => {
    startEdit('Новый текст');
    button('Сохранить').click();
    render();

    http
      .expectOne({ method: 'PUT', url: '/api/message' })
      .flush({ detail: 'Сообщение не найдено по указанному идентификатору.' }, { status: 404, statusText: 'Not Found' });
    render();

    expect(element.querySelector('.edit-form')).not.toBeNull();
    expect(element.querySelector('[role=alert]')!.textContent).toContain('Сообщение не найдено по указанному идентификатору.');
    expect(changed).toBe(0);
  });

  it('EditForm_Escape_WithDraftText_ShouldCancelEdit', () => {
    startEdit('Новый текст');

    press({ key: 'Escape' });

    expect(element.querySelector('.edit-form')).toBeNull();
    expect(element.querySelector('.text')!.textContent).toBe('Привет\nмир');
  });

  function startDelete(): void {
    action('Удалить сообщение').click();
    render();
    http
      .expectOne((request) => request.method === 'GET' && request.url === '/api/message/delete' && request.params.get('id') === 'm1')
      .flush(successBody({ ...message, chatId: 'c1' }));
    render();
  }

  it('Actions_Delete_WithExistingMessage_ShouldKeepBubbleAndShowConfirmBelow', () => {
    startDelete();

    expect(element.querySelector('.text')!.textContent).toBe('Привет\nмир');
    expect(element.querySelector('.actions')).toBeNull();
    expect(element.querySelector('.row')!.nextElementSibling!.tagName).toBe('APP-MESSAGE-DELETE-CONFIRM');
    expect(element.querySelector('app-message-delete-confirm')!.textContent).toContain('Удалить сообщение?');
  });

  it('DeleteConfirm_Delete_WithExistingMessage_ShouldEmitDeleted', () => {
    startDelete();

    button('Удалить').click();
    http
      .expectOne((request) => request.method === 'DELETE' && request.url === '/api/message' && request.params.get('id') === 'm1')
      .flush(successBody({ isDeleted: true }, 2));

    expect(deleted).toBe(1);
  });

  it('DeleteConfirm_Cancel_WithOpenConfirm_ShouldRestoreActionButtonsWithoutDelete', () => {
    startDelete();

    button('Отмена').click();
    render();

    expect(element.querySelector('app-message-delete-confirm')).toBeNull();
    expect(action('Удалить сообщение')).not.toBeNull();
    expect(deleted).toBe(0);
  });
});
