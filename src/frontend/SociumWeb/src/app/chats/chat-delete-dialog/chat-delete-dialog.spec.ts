import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { successBody } from '../../shared/api/api-response.testing';
import { ChatDeleteDialog } from './chat-delete-dialog';

describe('ChatDeleteDialog', () => {
  const page = { id: 'c1', roomId: '1', name: 'Общий' };

  let fixture: ComponentFixture<ChatDeleteDialog>;
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
    return http.expectOne((request) => request.method === 'GET' && request.url === '/api/chat/delete' && request.params.get('id') === 'c1');
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ChatDeleteDialog],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(ChatDeleteDialog);
    fixture.componentRef.setInput('chatId', 'c1');
    deleted = 0;
    closed = 0;
    fixture.componentInstance.deleted.subscribe(() => deleted++);
    fixture.componentInstance.closed.subscribe(() => closed++);
    element = fixture.nativeElement;
    render();
  });

  afterEach(() => http.verify());

  it('Dialog_Load_WithExistingChat_ShouldShowNameAndEnableDelete', () => {
    expect(deleteButton().disabled).toBe(true);

    expectDeletePage().flush(successBody(page));
    render();

    expect(element.querySelector('[role=dialog]')!.textContent).toContain('Общий');
    expect(deleteButton().disabled).toBe(false);
  });

  it('Dialog_Confirm_WithExistingChat_ShouldDeleteAndEmitDeleted', () => {
    expectDeletePage().flush(successBody(page));
    render();

    deleteButton().click();
    http
      .expectOne((request) => request.method === 'DELETE' && request.url === '/api/chat' && request.params.get('id') === 'c1')
      .flush(successBody({ isDeleted: true }, 2));

    expect(deleted).toBe(1);
  });

  it('Dialog_Load_WithUnknownId_ShouldShowProblemAndKeepDeleteDisabled', () => {
    expectDeletePage().flush({ detail: 'Чат не найден по указанному идентификатору.' }, { status: 404, statusText: 'Not Found' });
    render();

    expect(element.querySelector('[role=alert]')!.textContent).toContain('Чат не найден по указанному идентификатору.');
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
