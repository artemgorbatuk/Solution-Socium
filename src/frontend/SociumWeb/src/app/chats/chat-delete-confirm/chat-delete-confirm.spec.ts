import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { successBody } from '../../shared/api/api-response.testing';
import { ChatDeleteConfirm } from './chat-delete-confirm';

describe('ChatDeleteConfirm', () => {
  const page = { id: 'c1', roomId: '1', name: 'Общий' };

  let fixture: ComponentFixture<ChatDeleteConfirm>;
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
    return http.expectOne((request) => request.method === 'GET' && request.url === '/api/chat/delete' && request.params.get('id') === 'c1');
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ChatDeleteConfirm],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(ChatDeleteConfirm);
    fixture.componentRef.setInput('chatId', 'c1');
    fixture.componentRef.setInput('chatName', 'Общий');
    deleted = 0;
    cancelled = 0;
    fixture.componentInstance.deleted.subscribe(() => deleted++);
    fixture.componentInstance.cancelled.subscribe(() => cancelled++);
    element = fixture.nativeElement;
    render();
  });

  afterEach(() => http.verify());

  it('Confirm_Load_WithExistingChat_ShouldAskWithNameAndEnableDeleteAfterResponse', () => {
    expect(element.getAttribute('role')).toBe('group');
    expect(element.textContent).toContain('Удалить чат «Общий»?');
    expect(deleteButton().disabled).toBe(true);

    expectDeletePage().flush(successBody(page));
    render();

    expect(deleteButton().disabled).toBe(false);
  });

  it('Confirm_Delete_WithExistingChat_ShouldDeleteAndEmitDeleted', () => {
    expectDeletePage().flush(successBody(page));
    render();

    deleteButton().click();
    http
      .expectOne((request) => request.method === 'DELETE' && request.url === '/api/chat' && request.params.get('id') === 'c1')
      .flush(successBody({ isDeleted: true }, 2));

    expect(deleted).toBe(1);
  });

  it('Confirm_Load_WithUnknownId_ShouldShowProblemAndKeepDeleteDisabled', () => {
    expectDeletePage().flush({ detail: 'Чат не найден по указанному идентификатору.' }, { status: 404, statusText: 'Not Found' });
    render();

    expect(element.querySelector('[role=alert]')!.textContent).toContain('Чат не найден по указанному идентификатору.');
    expect(deleteButton().disabled).toBe(true);
  });

  it('Confirm_Render_WithAnyChat_ShouldFocusCancel', async () => {
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
