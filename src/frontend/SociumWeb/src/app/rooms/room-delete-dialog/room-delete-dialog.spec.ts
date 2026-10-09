import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { successBody } from '../../shared/api/api-response.testing';
import { RoomDeleteDialog } from './room-delete-dialog';

describe('RoomDeleteDialog', () => {
  let fixture: ComponentFixture<RoomDeleteDialog>;
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
    return http.expectOne((request) => request.method === 'GET' && request.url === '/api/room/delete' && request.params.get('id') === '1');
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [RoomDeleteDialog],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(RoomDeleteDialog);
    fixture.componentRef.setInput('roomId', '1');
    deleted = 0;
    closed = 0;
    fixture.componentInstance.deleted.subscribe(() => deleted++);
    fixture.componentInstance.closed.subscribe(() => closed++);
    element = fixture.nativeElement;
    render();
  });

  afterEach(() => http.verify());

  it('Dialog_Load_WithExistingRoom_ShouldShowNameAndEnableDelete', () => {
    expect(deleteButton().disabled).toBe(true);

    expectDeletePage().flush(successBody({ id: '1', name: 'Кухня', chatCount: 0 }));
    render();

    expect(element.querySelector('[role=dialog]')!.textContent).toContain('Кухня');
    expect(element.querySelector('.warning')).toBeNull();
    expect(deleteButton().disabled).toBe(false);
  });

  it('Dialog_Load_WithRoomChats_ShouldWarnAboutChatCount', () => {
    expectDeletePage().flush(successBody({ id: '1', name: 'Кухня', chatCount: 3 }));
    render();

    expect(element.querySelector('.warning')!.textContent).toContain('Вместе с комнатой будут удалены чаты: 3');
  });

  it('Dialog_Confirm_WithExistingRoom_ShouldDeleteAndEmitDeleted', () => {
    expectDeletePage().flush(successBody({ id: '1', name: 'Кухня', chatCount: 0 }));
    render();

    deleteButton().click();
    http
      .expectOne((request) => request.method === 'DELETE' && request.url === '/api/room' && request.params.get('id') === '1')
      .flush(successBody({ isDeleted: true }, 2));

    expect(deleted).toBe(1);
  });

  it('Dialog_Load_WithUnknownId_ShouldShowProblemAndKeepDeleteDisabled', () => {
    expectDeletePage().flush(
      { detail: 'Комната не найдена по указанному идентификатору.' },
      { status: 404, statusText: 'Not Found' },
    );
    render();

    expect(element.querySelector('[role=alert]')!.textContent).toContain('Комната не найдена по указанному идентификатору.');
    expect(deleteButton().disabled).toBe(true);
  });

  it('Dialog_Confirm_WithServerError_ShouldShowProblemAndAllowRetry', () => {
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

  it('Dialog_Close_WithCancelEscapeOrBackdrop_ShouldEmitClosedWithoutDelete', () => {
    expectDeletePage().flush(successBody({ id: '1', name: 'Кухня', chatCount: 0 }));
    render();

    Array.from(element.querySelectorAll('button')).find((item) => item.textContent!.includes('Отмена'))!.click();
    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    element.querySelector<HTMLElement>('.backdrop')!.click();

    expect(closed).toBe(3);
    expect(deleted).toBe(0);
  });
});
