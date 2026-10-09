import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { successBody } from '../../shared/api/api-response.testing';
import { ChatListModel } from '../chat.models';
import { ChatListItem } from './chat-list-item';

describe('ChatListItem', () => {
  const chat: ChatListModel = { id: 'c1', name: 'Общий' };

  let fixture: ComponentFixture<ChatListItem>;
  let http: HttpTestingController;
  let element: HTMLElement;
  let changed: number;
  let deleteRequested: ChatListModel[];

  function render(): void {
    fixture.detectChanges();
  }

  function button(text: string): HTMLButtonElement {
    return Array.from(element.querySelectorAll('button')).find((item) => item.textContent!.includes(text))!;
  }

  function openMenu(): void {
    element.querySelector<HTMLButtonElement>('.menu-button')!.click();
    render();
  }

  function startRename(value: string): void {
    openMenu();
    button('Переименовать').click();
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
    deleteRequested = [];
    fixture.componentInstance.changed.subscribe(() => changed++);
    fixture.componentInstance.deleteRequested.subscribe((value) => deleteRequested.push(value));
    element = fixture.nativeElement;
    render();
  });

  afterEach(() => http.verify());

  it('Row_Render_WithExistingChat_ShouldShowNameAndMenuButton', () => {
    expect(element.querySelector('.chat-name')!.textContent).toContain('Общий');
    expect(element.querySelector('.menu-button')!.getAttribute('aria-label')).toBe('Действия с чатом Общий');
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

  it('Menu_Click_WithOutsideClickAfterOpen_ShouldOpenThenClose', () => {
    openMenu();
    expect(element.querySelector('[role=menu]')).not.toBeNull();

    document.body.click();
    render();

    expect(element.querySelector('[role=menu]')).toBeNull();
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

  it('Menu_Delete_WithExistingChat_ShouldEmitDeleteRequested', () => {
    openMenu();
    button('Удалить').click();
    render();

    expect(deleteRequested).toEqual([chat]);
    expect(element.querySelector('[role=menu]')).toBeNull();
  });
});
