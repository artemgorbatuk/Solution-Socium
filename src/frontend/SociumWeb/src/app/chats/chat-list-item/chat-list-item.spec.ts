import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { successBody } from '../../shared/api/api-response.testing';
import { ChatListModel } from '../chat.models';
import { ChatListItem } from './chat-list-item';

describe('ChatListItem', () => {
  const chat: ChatListModel = { id: 'c1', name: 'Общий', isAdmin: true };

  let fixture: ComponentFixture<ChatListItem>;
  let http: HttpTestingController;
  let element: HTMLElement;
  let changed: number;
  let deleted: number;

  function render(): void {
    fixture.detectChanges();
  }

  function action(label: string): HTMLButtonElement {
    return element.querySelector<HTMLButtonElement>(`.actions button[aria-label="${label}"]`)!;
  }

  function startRename(value: string): void {
    action('Переименовать чат Общий').click();
    render();
    const field = element.querySelector<HTMLInputElement>('.rename-form input')!;
    field.value = value;
    field.dispatchEvent(new Event('input'));
    render();
  }

  function submitRename(): void {
    element.querySelector('form')!.dispatchEvent(new Event('submit'));
    render();
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ChatListItem],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([{ path: 'chat/:id', children: [] }])],
    });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(ChatListItem);
    fixture.componentRef.setInput('chat', chat);
    changed = 0;
    deleted = 0;
    fixture.componentInstance.changed.subscribe(() => changed++);
    fixture.componentInstance.deleted.subscribe(() => deleted++);
    element = fixture.nativeElement;
    render();
  });

  afterEach(() => http.verify());

  it('Row_Render_WithExistingChat_ShouldShowNameAndActionButtons', () => {
    const buttons = Array.from(element.querySelectorAll<HTMLButtonElement>('.actions button'));

    expect(element.querySelector('.chat-name')!.textContent).toContain('Общий');
    expect(buttons.map((item) => item.getAttribute('aria-label'))).toEqual(['Переименовать чат Общий', 'Удалить чат Общий']);
    expect(buttons.map((item) => item.title)).toEqual(['Переименовать', 'Удалить']);
    expect(buttons.map((item) => item.querySelector('app-icon svg')!.getAttribute('data-icon'))).toEqual(['pencil', 'trash']);
    expect(buttons[1].classList).toContain('danger');
    expect(element.querySelector('[role=menu]')).toBeNull();
  });

  it('Row_Render_WithNotAdmin_ShouldShowNameWithoutActionButtons', () => {
    fixture.componentRef.setInput('chat', { ...chat, isAdmin: false });
    render();

    expect(element.querySelector('.chat-name')!.textContent).toContain('Общий');
    expect(element.querySelector('.actions')).toBeNull();
  });

  it('Link_Render_WithOtherChatOpen_ShouldPointToChatWithoutHighlight', () => {
    const link = element.querySelector<HTMLAnchorElement>('a.chat-link')!;

    expect(link.getAttribute('href')).toBe('/chat/c1');
    expect(link.classList).not.toContain('active');
    expect(link.hasAttribute('aria-current')).toBe(false);
  });

  it('Link_Navigate_WithThisChatOpen_ShouldHighlightAsCurrentPage', async () => {
    await TestBed.inject(Router).navigateByUrl('/chat/c1');
    render();

    const link = element.querySelector<HTMLAnchorElement>('a.chat-link')!;
    expect(link.classList).toContain('active');
    expect(link.getAttribute('aria-current')).toBe('page');
  });

  it('Row_Render_WithRenameForm_ShouldHideActionButtons', () => {
    startRename('Объявления');

    expect(element.querySelector('.actions')).toBeNull();
  });

  it('RenameForm_Submit_WithPaddedName_ShouldSendTrimmedNameAndEmitChanged', () => {
    startRename('  Объявления  ');
    submitRename();

    const request = http.expectOne({ method: 'PUT', url: '/api/chat' });
    expect(request.request.body).toEqual({ id: 'c1', name: 'Объявления' });
    request.flush(successBody(null, 2));
    render();

    expect(changed).toBe(1);
    expect(element.querySelector('.rename-form')).toBeNull();
  });

  it('RenameForm_Submit_WithUnchangedName_ShouldCloseWithoutRequest', () => {
    startRename(' Общий ');
    submitRename();

    http.expectNone({ method: 'PUT', url: '/api/chat' });
    expect(element.querySelector('.rename-form')).toBeNull();
    expect(changed).toBe(0);
  });

  it('RenameForm_Submit_WithServerError_ShouldShowProblemAndKeepForm', () => {
    startRename('Объявления');
    submitRename();

    http
      .expectOne({ method: 'PUT', url: '/api/chat' })
      .flush({ detail: 'Чат не найден по указанному идентификатору.' }, { status: 404, statusText: 'Not Found' });
    render();

    expect(element.querySelector('.rename-form')).not.toBeNull();
    expect(element.querySelector('[role=alert]')!.textContent).toContain('Чат не найден по указанному идентификатору.');
    expect(changed).toBe(0);
  });

  it('RenameForm_Escape_WithDraftName_ShouldCancelRename', () => {
    startRename('Объявления');

    element.querySelector('.rename-form input')!.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    render();

    expect(element.querySelector('.rename-form')).toBeNull();
    expect(element.querySelector('.chat-name')!.textContent).toContain('Общий');
  });

  function button(text: string): HTMLButtonElement {
    return Array.from(element.querySelectorAll('button')).find((item) => item.textContent!.includes(text))!;
  }

  function startDelete(): void {
    action('Удалить чат Общий').click();
    render();
    http
      .expectOne((request) => request.method === 'GET' && request.url === '/api/chat/delete' && request.params.get('id') === 'c1')
      .flush(successBody({ id: 'c1', roomId: '1', name: 'Общий' }));
    render();
  }

  it('Actions_Delete_WithExistingChat_ShouldReplaceRowWithConfirm', () => {
    startDelete();

    expect(element.querySelector('.row')).toBeNull();
    expect(element.querySelector('app-chat-delete-confirm')!.textContent).toContain('Удалить чат «Общий»?');
  });

  it('DeleteConfirm_Delete_WithExistingChat_ShouldEmitDeleted', () => {
    startDelete();

    button('Удалить').click();
    http
      .expectOne((request) => request.method === 'DELETE' && request.url === '/api/chat' && request.params.get('id') === 'c1')
      .flush(successBody({ isDeleted: true }, 2));

    expect(deleted).toBe(1);
  });

  it('DeleteConfirm_Cancel_WithOpenConfirm_ShouldRestoreRowWithoutDelete', () => {
    startDelete();

    button('Отмена').click();
    render();

    expect(element.querySelector('app-chat-delete-confirm')).toBeNull();
    expect(element.querySelector('.row .chat-name')!.textContent).toContain('Общий');
    expect(deleted).toBe(0);
  });
});
