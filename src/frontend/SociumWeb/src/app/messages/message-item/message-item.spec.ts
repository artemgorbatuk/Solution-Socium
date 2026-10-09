import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { successBody } from '../../shared/api/api-response.testing';
import { MessageListModel } from '../message.models';
import { MessageItem } from './message-item';

describe('MessageItem', () => {
  const message: MessageListModel = { id: 'm1', text: 'Привет\nмир', createdAt: '2020-01-15T12:02:00Z' };

  let fixture: ComponentFixture<MessageItem>;
  let http: HttpTestingController;
  let element: HTMLElement;
  let changed: number;
  let deleteRequested: MessageListModel[];

  function render(): void {
    fixture.detectChanges();
  }

  function button(text: string): HTMLButtonElement {
    return Array.from(element.querySelectorAll('button')).find((item) => item.textContent!.includes(text))!;
  }

  function field(): HTMLTextAreaElement {
    return element.querySelector<HTMLTextAreaElement>('.edit-form textarea')!;
  }

  function openMenu(): void {
    element.querySelector<HTMLButtonElement>('.menu-button')!.click();
    render();
  }

  function startEdit(value: string): void {
    openMenu();
    button('Изменить').click();
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
    changed = 0;
    deleteRequested = [];
    fixture.componentInstance.changed.subscribe(() => changed++);
    fixture.componentInstance.deleteRequested.subscribe((value) => deleteRequested.push(value));
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
    expect(element.querySelector('.menu-button')!.getAttribute('aria-label')).toBe('Действия с сообщением');
  });

  it('Menu_Click_WithOutsideClickAfterOpen_ShouldOpenThenClose', () => {
    openMenu();
    expect(element.querySelector('[role=menu]')).not.toBeNull();

    document.body.click();
    render();

    expect(element.querySelector('[role=menu]')).toBeNull();
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

  it('Menu_Delete_WithExistingMessage_ShouldEmitDeleteRequested', () => {
    openMenu();
    button('Удалить').click();
    render();

    expect(deleteRequested).toEqual([message]);
    expect(element.querySelector('[role=menu]')).toBeNull();
  });
});
