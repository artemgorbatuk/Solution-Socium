import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { successBody } from '../../shared/api/api-response.testing';
import { MessageDeleteDialog } from './message-delete-dialog';

describe('MessageDeleteDialog', () => {
  const page = { id: 'm1', chatId: 'c1', text: 'Привет\nмир', createdAt: '2026-10-09T12:02:00Z' };

  let fixture: ComponentFixture<MessageDeleteDialog>;
  let http: HttpTestingController;
  let element: HTMLElement;
  let deleted: number;
  let closed: number;

  function render(): void {
    fixture.detectChanges();
  }

  function deleteButton(): HTMLButtonElement {
    return element.querySelector<HTMLButtonElement>('.danger')!;
  }

  function expectDeletePage() {
    return http.expectOne((request) => request.method === 'GET' && request.url === '/api/message/delete' && request.params.get('id') === 'm1');
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [MessageDeleteDialog],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(MessageDeleteDialog);
    fixture.componentRef.setInput('messageId', 'm1');
    deleted = 0;
    closed = 0;
    fixture.componentInstance.deleted.subscribe(() => deleted++);
    fixture.componentInstance.closed.subscribe(() => closed++);
    element = fixture.nativeElement;
    render();
  });

  afterEach(() => http.verify());

  it('Dialog_Load_WithExistingMessage_ShouldShowTextAndEnableDelete', () => {
    expect(deleteButton().disabled).toBe(true);

    expectDeletePage().flush(successBody(page));
    render();

    expect(element.querySelector('h2')!.textContent).toBe('Удалить сообщение?');
    expect(element.querySelector('.preview')!.textContent).toBe('Привет\nмир');
    expect(deleteButton().disabled).toBe(false);
  });

  it('Dialog_Load_WithLongText_ShouldShowBeginningWithEllipsis', () => {
    expectDeletePage().flush(successBody({ ...page, text: 'я'.repeat(1000) }));
    render();

    expect(element.querySelector('.preview')!.textContent).toBe(`${'я'.repeat(300)}…`);
  });

  it('Dialog_Confirm_WithExistingMessage_ShouldDeleteAndEmitDeleted', () => {
    expectDeletePage().flush(successBody(page));
    render();

    deleteButton().click();
    http
      .expectOne((request) => request.method === 'DELETE' && request.url === '/api/message' && request.params.get('id') === 'm1')
      .flush(successBody({ isDeleted: true }, 2));

    expect(deleted).toBe(1);
  });

  it('Dialog_Load_WithUnknownId_ShouldShowProblemAndKeepDeleteDisabled', () => {
    expectDeletePage().flush({ detail: 'Сообщение не найдено по указанному идентификатору.' }, { status: 404, statusText: 'Not Found' });
    render();

    expect(element.querySelector('[role=alert]')!.textContent).toContain('Сообщение не найдено по указанному идентификатору.');
    expect(deleteButton().disabled).toBe(true);
  });

  it('Dialog_Close_WithCancelEscapeOrBackdrop_ShouldEmitClosedWithoutDelete', () => {
    expectDeletePage().flush(successBody(page));
    render();

    Array.from(element.querySelectorAll('button')).find((item) => item.textContent!.includes('Отмена'))!.click();
    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    element.querySelector<HTMLElement>('.backdrop')!.click();

    expect(closed).toBe(3);
    expect(deleted).toBe(0);
  });
});
