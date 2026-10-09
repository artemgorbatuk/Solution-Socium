import { HttpRequest, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { TopbarTitle } from '../../layout/topbar/topbar-title';
import { ParticipantsPanelState, participantsPanelOpenStorageKey } from '../../participants/participants-panel-state';
import { successBody } from '../../shared/api/api-response.testing';
import { CurrentUser, currentUserStorageKey } from '../../users/current-user';
import { ChatChanges } from '../chat-changes';
import { ChatPage } from './chat-page';

describe('ChatPage', () => {
  let fixture: ComponentFixture<ChatPage>;
  let http: HttpTestingController;
  let element: HTMLElement;

  function render(): void {
    fixture.detectChanges();
  }

  function expectInfo(id: string) {
    return http.expectOne((request) => request.method === 'GET' && request.url === '/api/chat/info' && request.params.get('id') === id);
  }

  function isMessageList(chatId: string) {
    return (request: HttpRequest<unknown>) =>
      request.method === 'GET' && request.url === '/api/message' && request.params.get('chatId') === chatId;
  }

  function flushChat(id: string, name: string, isParticipant = true): void {
    expectInfo(id).flush(successBody({ id, roomId: 'r1', name, isParticipant, isAdmin: false }));
    render();
  }

  function flushMessages(chatId: string): void {
    http.expectOne(isMessageList(chatId)).flush(successBody({ rowExists: false, rowCount: 0, rows: [] }));
    render();
  }

  function textarea(): HTMLTextAreaElement {
    return element.querySelector<HTMLTextAreaElement>('textarea')!;
  }

  function button(text: string): HTMLButtonElement | undefined {
    return Array.from(element.querySelectorAll('button')).find((item) => item.textContent!.includes(text));
  }

  function type(value: string): void {
    textarea().value = value;
    textarea().dispatchEvent(new Event('input'));
    render();
  }

  function open(id: string): void {
    fixture.componentRef.setInput('id', id);
    render();
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ChatPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    http = TestBed.inject(HttpTestingController);
    TestBed.inject(CurrentUser).select('u1');
    fixture = TestBed.createComponent(ChatPage);
    element = fixture.nativeElement;
  });

  afterEach(() => {
    http.verify();
    localStorage.removeItem(currentUserStorageKey);
    localStorage.removeItem(participantsPanelOpenStorageKey);
  });

  it('Chat_Load_WithExistingChat_ShouldSetTopbarTitleAndShowMessagesWithComposer', () => {
    open('c1');
    expect(element.textContent).toContain('Загрузка…');

    flushChat('c1', 'Общий');
    flushMessages('c1');

    expect(TestBed.inject(TopbarTitle).text()).toBe('Общий');
    expect(element.textContent).toContain('Сообщений пока нет');
    expect(textarea().getAttribute('aria-label')).toBe('Сообщение в чат Общий');
    expect(textarea().placeholder).toBe('Напишите сообщение…');
  });

  it('Chat_Load_WithUnknownChat_ShouldShowNotFoundWithHomeLink', () => {
    open('c1');

    expectInfo('c1').flush({ detail: 'Чат не найден по указанному идентификатору.' }, { status: 404, statusText: 'Not Found' });
    render();

    expect(element.textContent).toContain('Чат не найден');
    expect(element.querySelector('a')!.getAttribute('href')).toBe('/');
    expect(element.querySelector('textarea')).toBeNull();
    expect(TestBed.inject(TopbarTitle).text()).toBeNull();
  });

  it('Chat_Load_WithServerError_ShouldShowProblem', () => {
    open('c1');

    expectInfo('c1').flush({ detail: 'База недоступна.' }, { status: 500, statusText: 'Server Error' });
    render();

    expect(element.querySelector('[role=alert]')!.textContent).toContain('База недоступна.');
  });

  it('Chat_Load_WithNotParticipant_ShouldShowJoinStubWithoutMessages', () => {
    open('c1');
    flushChat('c1', 'Общий', false);

    expect(element.textContent).toContain('Вы не участник этого чата');
    expect(button('Вступить в чат')!.classList).toContain('primary');
    expect(element.querySelector('textarea')).toBeNull();
    expect(TestBed.inject(TopbarTitle).text()).toBe('Общий');
    expect(TestBed.inject(ParticipantsPanelState).available()).toBe(false);
  });

  it('Chat_Load_WithoutCurrentUser_ShouldAskToChooseUserWithoutJoinButton', () => {
    TestBed.inject(CurrentUser).clear();
    open('c1');
    flushChat('c1', 'Общий', false);

    expect(element.textContent).toContain('Выберите пользователя внизу боковой панели');
    expect(button('Вступить в чат')).toBeUndefined();
    expect(element.querySelector('textarea')).toBeNull();
  });

  it('Join_Click_WithNotParticipant_ShouldJoinNotifyChangesAndShowMessages', () => {
    open('c1');
    flushChat('c1', 'Общий', false);

    button('Вступить в чат')!.click();
    const request = http.expectOne({ method: 'POST', url: '/api/participant' });
    expect(request.request.body).toEqual({ chatId: 'c1' });
    request.flush(successBody({ id: 'p1' }, 2));
    render();
    flushChat('c1', 'Общий');
    flushMessages('c1');

    expect(TestBed.inject(ChatChanges).version()).toBe(1);
    expect(textarea()).not.toBeNull();
    expect(TestBed.inject(ParticipantsPanelState).available()).toBe(true);
  });

  it('Join_Click_WithServerError_ShouldShowProblemAndKeepStub', () => {
    open('c1');
    flushChat('c1', 'Общий', false);

    button('Вступить в чат')!.click();
    http
      .expectOne({ method: 'POST', url: '/api/participant' })
      .flush({ detail: 'Вы уже участник этого чата.' }, { status: 422, statusText: 'Unprocessable Entity' });
    render();

    expect(element.querySelector('[role=alert]')!.textContent).toContain('Вы уже участник этого чата.');
    expect(button('Вступить в чат')!.disabled).toBe(false);
  });

  it('CurrentUser_Change_WithOpenChat_ShouldReloadChatForNewUser', () => {
    open('c1');
    flushChat('c1', 'Общий');
    flushMessages('c1');

    TestBed.inject(CurrentUser).select('u2');
    render();
    flushChat('c1', 'Общий', false);

    expect(element.textContent).toContain('Вы не участник этого чата');
    expect(element.querySelector('textarea')).toBeNull();
  });

  it('ParticipantsPanel_Open_WithParticipant_ShouldShowPanelBesideMessages', () => {
    open('c1');
    flushChat('c1', 'Общий');
    flushMessages('c1');
    const panel = TestBed.inject(ParticipantsPanelState);
    expect(panel.available()).toBe(true);
    expect(element.querySelector('app-participants-panel')).toBeNull();

    panel.toggle();
    render();

    http
      .expectOne((request) => request.method === 'GET' && request.url === '/api/participant' && request.params.get('chatId') === 'c1')
      .flush(successBody({ rowExists: false, rowCount: 0, isAdmin: false, rows: [] }));
    render();
    expect(element.querySelector('.chat-body app-participants-panel')).not.toBeNull();
  });

  it('ParticipantsPanel_Reload_WithParticipantLeft_ShouldHideButtonAndPanelButKeepChoice', () => {
    open('c1');
    flushChat('c1', 'Общий');
    flushMessages('c1');
    const panel = TestBed.inject(ParticipantsPanelState);
    panel.toggle();

    TestBed.inject(ChatChanges).notify();
    render();
    flushChat('c1', 'Общий', false);
    http.match((request) => request.url === '/api/participant');

    expect(panel.available()).toBe(false);
    expect(panel.open()).toBe(true);
    expect(element.querySelector('app-participants-panel')).toBeNull();
  });

  it('Chat_Switch_WithTypedText_ShouldClearTextAndLoadOtherChatMessages', () => {
    open('c1');
    flushChat('c1', 'Общий');
    flushMessages('c1');
    type('Черновик');

    open('c2');
    expect(element.textContent).toContain('Загрузка…');
    flushChat('c2', 'Арт');
    flushMessages('c2');

    expect(textarea().value).toBe('');
    expect(TestBed.inject(TopbarTitle).text()).toBe('Арт');
  });

  it('Changes_Notify_WithRenamedChat_ShouldUpdateTitleAndKeepTextWithoutReloadingMessages', () => {
    open('c1');
    flushChat('c1', 'Общий');
    flushMessages('c1');
    type('Черновик');

    TestBed.inject(ChatChanges).notify();
    render();
    flushChat('c1', 'Объявления');

    http.expectNone(isMessageList('c1'));
    expect(TestBed.inject(TopbarTitle).text()).toBe('Объявления');
    expect(textarea().value).toBe('Черновик');
    expect(textarea().getAttribute('aria-label')).toBe('Сообщение в чат Объявления');
  });

  it('Changes_Notify_WithDeletedChat_ShouldNavigateHome', () => {
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    open('c1');
    flushChat('c1', 'Общий');
    flushMessages('c1');

    TestBed.inject(ChatChanges).notify();
    render();
    expectInfo('c1').flush({ detail: 'Чат не найден по указанному идентификатору.' }, { status: 404, statusText: 'Not Found' });
    render();

    expect(navigate).toHaveBeenCalledWith('/');
  });

  it('Page_Destroy_WithLoadedChat_ShouldClearTopbarTitleAndParticipantsButton', () => {
    open('c1');
    flushChat('c1', 'Общий');
    flushMessages('c1');

    fixture.destroy();

    expect(TestBed.inject(TopbarTitle).text()).toBeNull();
    expect(TestBed.inject(ParticipantsPanelState).available()).toBe(false);
  });

  it('ParticipantsPanel_Load_WithRememberedOpenPanel_ShouldShowPanelForParticipant', () => {
    TestBed.inject(ParticipantsPanelState).toggle();
    open('c1');
    flushChat('c1', 'Общий');
    flushMessages('c1');

    http
      .expectOne((request) => request.url === '/api/participant')
      .flush(successBody({ rowExists: false, rowCount: 0, isAdmin: false, rows: [] }));
    render();
    expect(element.querySelector('app-participants-panel')).not.toBeNull();
  });
});
