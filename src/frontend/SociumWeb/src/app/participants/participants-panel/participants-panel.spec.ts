import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ChatChanges } from '../../chats/chat-changes';
import { successBody } from '../../shared/api/api-response.testing';
import { CurrentUser, currentUserStorageKey } from '../../users/current-user';
import { ParticipantListModel } from '../participant.models';
import { ParticipantsPanelState, participantsPanelOpenStorageKey } from '../participants-panel-state';
import { ParticipantsPanel } from './participants-panel';

describe('ParticipantsPanel', () => {
  const anna: ParticipantListModel = { id: 'p1', userId: 'u1', login: 'anna', name: 'Анна', isAdmin: true };
  const boris: ParticipantListModel = { id: 'p2', userId: 'u2', login: 'boris', name: 'Борис', isAdmin: false };

  let fixture: ComponentFixture<ParticipantsPanel>;
  let http: HttpTestingController;
  let element: HTMLElement;

  function render(): void {
    fixture.detectChanges();
  }

  function flushList(rows: ParticipantListModel[], isAdmin: boolean): void {
    http
      .expectOne((request) => request.method === 'GET' && request.url === '/api/participant' && request.params.get('chatId') === 'c1')
      .flush(successBody({ rowExists: rows.length > 0, rowCount: rows.length, isAdmin, rows }));
    render();
  }

  function button(text: string): HTMLButtonElement | undefined {
    return Array.from(element.querySelectorAll('button')).find((item) => item.textContent!.trim() === text);
  }

  function rowTexts(): string[] {
    return Array.from(element.querySelectorAll('.participant')).map((item) =>
      Array.from(item.querySelectorAll('.name, .login, .badge'))
        .map((part) => part.textContent!.replace(/\s+/g, ' ').trim())
        .join(' | '),
    );
  }

  function expectLeavePage() {
    return http.expectOne(
      (request) => request.method === 'GET' && request.url === '/api/participant/delete' && request.params.get('chatId') === 'c1',
    );
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ParticipantsPanel],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    TestBed.inject(CurrentUser).select('u1');
    fixture = TestBed.createComponent(ParticipantsPanel);
    fixture.componentRef.setInput('chatId', 'c1');
    fixture.componentRef.setInput('chatName', 'Общий');
    element = fixture.nativeElement;
    render();
  });

  afterEach(() => {
    http.verify();
    localStorage.removeItem(currentUserStorageKey);
    localStorage.removeItem(participantsPanelOpenStorageKey);
  });

  it('List_Load_WithParticipants_ShouldShowNamesLoginsAdminMarkAndSelf', () => {
    expect(element.textContent).toContain('Загрузка…');

    flushList([anna, boris], false);

    expect(element.querySelector('ul')!.getAttribute('aria-label')).toBe('Участники чата Общий');
    expect(element.querySelector('.count')!.textContent).toBe('2');
    expect(rowTexts()).toEqual(['Анна (вы) | anna | админ', 'Борис | boris']);
    expect(element.querySelector('.avatar')!.textContent).toBe('А');
  });

  it('List_Load_WithNotAdmin_ShouldHideRoleButtons', () => {
    flushList([anna, boris], false);

    expect(element.querySelector('.role')).toBeNull();
  });

  it('List_Load_WithAdmin_ShouldShowRevokeForAdminsAndGrantForOthers', () => {
    flushList([anna, boris], true);

    const labels = Array.from(element.querySelectorAll('.role')).map((item) => item.getAttribute('aria-label'));
    expect(labels).toEqual(['Снять роль админа: Анна', 'Сделать админом: Борис']);
    expect(button('Снять роль')).toBeDefined();
    expect(button('Сделать админом')).toBeDefined();
  });

  it('List_Load_WithServerError_ShouldShowProblem', () => {
    http
      .expectOne((request) => request.url === '/api/participant')
      .flush({ detail: 'Вы не участник этого чата.' }, { status: 403, statusText: 'Forbidden' });
    render();

    expect(element.querySelector('[role=alert]')!.textContent).toContain('Вы не участник этого чата.');
  });

  it('Role_Grant_WithAdmin_ShouldSendUpdateAndNotifyChanges', () => {
    flushList([anna, boris], true);

    button('Сделать админом')!.click();
    const request = http.expectOne({ method: 'PUT', url: '/api/participant' });
    expect(request.request.body).toEqual({ id: 'p2', isAdmin: true });
    request.flush(successBody({ id: 'p2', isAdmin: true }, 2));
    render();
    flushList([anna, { ...boris, isAdmin: true }], true);

    expect(TestBed.inject(ChatChanges).version()).toBe(1);
    expect(rowTexts()[1]).toContain('админ');
  });

  it('Role_Grant_WithLeaveReasonShown_ShouldHideOutdatedReason', () => {
    flushList([anna, boris], true);
    button('Покинуть чат')!.click();
    expectLeavePage().flush(successBody({ chatId: 'c1', canLeave: false, reason: 'Вы последний админ чата.' }));
    render();

    button('Сделать админом')!.click();
    http.expectOne({ method: 'PUT', url: '/api/participant' }).flush(successBody({ id: 'p2', isAdmin: true }, 2));
    render();
    flushList([anna, { ...boris, isAdmin: true }], true);

    expect(element.querySelector('.reason')).toBeNull();
    expect(button('Покинуть чат')).toBeDefined();
  });

  it('Role_Revoke_WithLastAdmin_ShouldShowProblem', () => {
    flushList([anna, boris], true);

    button('Снять роль')!.click();
    http
      .expectOne({ method: 'PUT', url: '/api/participant' })
      .flush({ detail: 'Нельзя снять роль с последнего админа чата.' }, { status: 422, statusText: 'Unprocessable Entity' });
    render();

    expect(element.querySelector('[role=alert]')!.textContent).toContain('Нельзя снять роль с последнего админа чата.');
    expect(TestBed.inject(ChatChanges).version()).toBe(0);
  });

  it('Leave_Request_WithAllowedLeave_ShouldShowConfirmInPlace', () => {
    flushList([anna, boris], false);

    button('Покинуть чат')!.click();
    expectLeavePage().flush(successBody({ chatId: 'c1', canLeave: true, reason: null }));
    render();

    expect(element.querySelector('.footer')!.textContent).toContain('Покинуть чат «Общий»? Ваши сообщения останутся в чате.');
    const buttons = Array.from(element.querySelectorAll<HTMLButtonElement>('.footer .buttons button'));
    expect(buttons.map((item) => item.textContent!.trim())).toEqual(['Отмена', 'Покинуть']);
    expect(buttons[1].classList).toContain('danger');
  });

  it('Leave_Confirm_WithAllowedLeave_ShouldLeaveAndNotifyChanges', () => {
    flushList([anna, boris], false);
    button('Покинуть чат')!.click();
    expectLeavePage().flush(successBody({ chatId: 'c1', canLeave: true, reason: null }));
    render();

    button('Покинуть')!.click();
    http
      .expectOne((request) => request.method === 'DELETE' && request.url === '/api/participant' && request.params.get('chatId') === 'c1')
      .flush(successBody({ isDeleted: true }, 2));
    render();
    flushList([boris], false);

    expect(TestBed.inject(ChatChanges).version()).toBe(1);
    expect(button('Покинуть чат')).toBeDefined();
  });

  it('Leave_Cancel_WithOpenConfirm_ShouldRestoreLeaveButtonWithoutRequest', () => {
    flushList([anna, boris], false);
    button('Покинуть чат')!.click();
    expectLeavePage().flush(successBody({ chatId: 'c1', canLeave: true, reason: null }));
    render();

    button('Отмена')!.click();
    render();

    http.expectNone({ method: 'DELETE' });
    expect(button('Покинуть чат')).toBeDefined();
  });

  it('Leave_Request_WithLastAdmin_ShouldShowReasonInsteadOfConfirm', () => {
    flushList([anna, boris], true);

    button('Покинуть чат')!.click();
    expectLeavePage().flush(
      successBody({ chatId: 'c1', canLeave: false, reason: 'Вы последний админ чата — сначала назначьте админом другого участника.' }),
    );
    render();

    expect(element.querySelector('.reason')!.textContent).toContain('Вы последний админ чата');
    expect(button('Покинуть')).toBeUndefined();

    button('Понятно')!.click();
    render();
    expect(button('Покинуть чат')).toBeDefined();
  });

  it('Close_Click_WithOpenPanel_ShouldClosePanel', () => {
    flushList([anna], false);
    const panel = TestBed.inject(ParticipantsPanelState);
    panel.toggle();

    element.querySelector<HTMLButtonElement>('[aria-label="Закрыть панель участников"]')!.click();

    expect(panel.open()).toBe(false);
    expect(localStorage.getItem(participantsPanelOpenStorageKey)).toBe('false');
  });

  it('List_Reload_WithCurrentUserChanged_ShouldReloadList', () => {
    flushList([anna, boris], true);

    TestBed.inject(CurrentUser).select('u2');
    render();
    flushList([anna, boris], false);

    expect(element.querySelector('.role')).toBeNull();
    expect(rowTexts()[1]).toContain('(вы)');
  });
});
