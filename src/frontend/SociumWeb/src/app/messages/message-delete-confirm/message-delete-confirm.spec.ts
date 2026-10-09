import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { successBody } from '../../shared/api/api-response.testing';
import { MessageDeleteConfirm } from './message-delete-confirm';

describe('MessageDeleteConfirm', () => {
  const page = { id: 'm1', chatId: 'c1', text: 'Привет\nмир', createdAt: '2026-10-09T12:02:00Z' };

  let fixture: ComponentFixture<MessageDeleteConfirm>;
  let http: HttpTestingController;
  let element: HTMLElement;
  let deleted: number;
  let cancelled: number;

  function render(): void {
    fixture.detectChanges();
  }

  function deleteButton(): HTMLButtonElement {
    return element.querySelector<HTMLButtonElement>('.danger')!;
  }

  function cancelButton(): HTMLButtonElement {
    return Array.from(element.querySelectorAll('button')).find((item) => item.textContent!.includes('Отмена'))!;
  }

  function expectDeletePage() {
    return http.expectOne((request) => request.method === 'GET' && request.url === '/api/message/delete' && request.params.get('id') === 'm1');
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [MessageDeleteConfirm],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(MessageDeleteConfirm);
    fixture.componentRef.setInput('messageId', 'm1');
    deleted = 0;
    cancelled = 0;
    fixture.componentInstance.deleted.subscribe(() => deleted++);
    fixture.componentInstance.cancelled.subscribe(() => cancelled++);
    element = fixture.nativeElement;
    render();
  });

  afterEach(() => http.verify());

  it('Confirm_Load_WithExistingMessage_ShouldAskAndEnableDeleteAfterResponse', () => {
    expect(element.getAttribute('role')).toBe('group');
    expect(element.querySelector('p')!.textContent).toBe('Удалить сообщение?');
    expect(deleteButton().disabled).toBe(true);

    expectDeletePage().flush(successBody(page));
    render();

    expect(deleteButton().disabled).toBe(false);
  });

  it('Confirm_Delete_WithExistingMessage_ShouldDeleteAndEmitDeleted', () => {
    expectDeletePage().flush(successBody(page));
    render();

    deleteButton().click();
    http
      .expectOne((request) => request.method === 'DELETE' && request.url === '/api/message' && request.params.get('id') === 'm1')
      .flush(successBody({ isDeleted: true }, 2));

    expect(deleted).toBe(1);
  });

  it('Confirm_Load_WithUnknownId_ShouldShowProblemAndKeepDeleteDisabled', () => {
    expectDeletePage().flush({ detail: 'Сообщение не найдено по указанному идентификатору.' }, { status: 404, statusText: 'Not Found' });
    render();

    expect(element.querySelector('[role=alert]')!.textContent).toContain('Сообщение не найдено по указанному идентификатору.');
    expect(deleteButton().disabled).toBe(true);
  });

  it('Confirm_Render_WithAnyMessage_ShouldFocusCancel', async () => {
    await fixture.whenStable();

    expect(document.activeElement).toBe(cancelButton());
    expectDeletePage().flush(successBody(page));
  });

  it('Confirm_Cancel_WithButtonOrEscape_ShouldEmitCancelledWithoutDelete', () => {
    expectDeletePage().flush(successBody(page));
    render();

    cancelButton().click();
    element.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));

    expect(cancelled).toBe(2);
    expect(deleted).toBe(0);
  });
});
