import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { successBody } from '../../shared/api/api-response.testing';
import { RoomDeleteConfirm } from './room-delete-confirm';

describe('RoomDeleteConfirm', () => {
  let fixture: ComponentFixture<RoomDeleteConfirm>;
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
    return http.expectOne((request) => request.method === 'GET' && request.url === '/api/room/delete' && request.params.get('id') === '1');
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [RoomDeleteConfirm],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(RoomDeleteConfirm);
    fixture.componentRef.setInput('roomId', '1');
    fixture.componentRef.setInput('roomName', 'Кухня');
    deleted = 0;
    cancelled = 0;
    fixture.componentInstance.deleted.subscribe(() => deleted++);
    fixture.componentInstance.cancelled.subscribe(() => cancelled++);
    element = fixture.nativeElement;
    render();
  });

  afterEach(() => http.verify());

  it('Confirm_Render_BeforeServerResponse_ShouldAskWithNameAndDisableDelete', () => {
    expect(element.getAttribute('role')).toBe('group');
    expect(element.textContent).toContain('Удалить комнату «Кухня»?');
    expect(Array.from(element.querySelectorAll('.buttons button')).map((item) => item.textContent!.trim())).toEqual(['Отмена', 'Удалить']);
    expect(deleteButton().disabled).toBe(true);

    expectDeletePage().flush(successBody({ id: '1', name: 'Кухня', chatCount: 0 }));
  });

  it('Confirm_Load_WithExistingRoom_ShouldEnableDeleteWithoutWarning', () => {
    expectDeletePage().flush(successBody({ id: '1', name: 'Кухня', chatCount: 0 }));
    render();

    expect(element.querySelector('.warning')).toBeNull();
    expect(deleteButton().disabled).toBe(false);
  });

  it('Confirm_Load_WithRoomChats_ShouldWarnAboutChatCount', () => {
    expectDeletePage().flush(successBody({ id: '1', name: 'Кухня', chatCount: 3 }));
    render();

    expect(element.querySelector('.warning')!.textContent).toContain('Вместе с комнатой будут удалены чаты: 3');
  });

  it('Confirm_Delete_WithExistingRoom_ShouldDeleteAndEmitDeleted', () => {
    expectDeletePage().flush(successBody({ id: '1', name: 'Кухня', chatCount: 0 }));
    render();

    deleteButton().click();
    http
      .expectOne((request) => request.method === 'DELETE' && request.url === '/api/room' && request.params.get('id') === '1')
      .flush(successBody({ isDeleted: true }, 2));

    expect(deleted).toBe(1);
  });

  it('Confirm_Load_WithUnknownId_ShouldShowProblemAndKeepDeleteDisabled', () => {
    expectDeletePage().flush(
      { detail: 'Комната не найдена по указанному идентификатору.' },
      { status: 404, statusText: 'Not Found' },
    );
    render();

    expect(element.querySelector('[role=alert]')!.textContent).toContain('Комната не найдена по указанному идентификатору.');
    expect(deleteButton().disabled).toBe(true);
  });

  it('Confirm_Delete_WithServerError_ShouldShowProblemAndAllowRetry', () => {
    expectDeletePage().flush(successBody({ id: '1', name: 'Кухня', chatCount: 0 }));
    render();

    deleteButton().click();
    http
      .expectOne((request) => request.method === 'DELETE')
      .flush({ detail: 'Не удалось удалить комнату' }, { status: 500, statusText: 'Error' });
    render();

    expect(deleted).toBe(0);
    expect(element.querySelector('[role=alert]')!.textContent).toContain('Не удалось удалить комнату');
    expect(deleteButton().disabled).toBe(false);
  });

  it('Confirm_Render_WithAnyRoom_ShouldFocusCancel', async () => {
    await fixture.whenStable();

    expect(document.activeElement).toBe(cancelButton());
    expectDeletePage().flush(successBody({ id: '1', name: 'Кухня', chatCount: 0 }));
  });

  it('Confirm_Cancel_WithButtonOrEscape_ShouldEmitCancelledWithoutDelete', () => {
    expectDeletePage().flush(successBody({ id: '1', name: 'Кухня', chatCount: 0 }));
    render();

    cancelButton().click();
    element.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));

    expect(cancelled).toBe(2);
    expect(deleted).toBe(0);
  });
});
