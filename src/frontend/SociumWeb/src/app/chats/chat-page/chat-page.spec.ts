import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { TopbarTitle } from '../../layout/topbar/topbar-title';
import { successBody } from '../../shared/api/api-response.testing';
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

  function flushChat(id: string, name: string): void {
    expectInfo(id).flush(successBody({ id, roomId: 'r1', name }));
    render();
  }

  function textarea(): HTMLTextAreaElement {
    return element.querySelector<HTMLTextAreaElement>('textarea')!;
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
    fixture = TestBed.createComponent(ChatPage);
    element = fixture.nativeElement;
  });

  afterEach(() => http.verify());

  it('Chat_Load_WithExistingChat_ShouldSetTopbarTitleAndShowComposer', () => {
    open('c1');
    expect(element.textContent).toContain('Загрузка…');

    flushChat('c1', 'Общий');

    expect(TestBed.inject(TopbarTitle).text()).toBe('Общий');
    expect(textarea().getAttribute('aria-label')).toBe('Сообщение в чат Общий');
    expect(textarea().placeholder).toBe('Напишите сообщение…');
    expect(element.querySelector<HTMLButtonElement>('button[aria-label="Отправить"]')!.disabled).toBe(true);
  });

  it('Composer_Input_WithText_ShouldKeepTextAndKeepSendDisabled', () => {
    open('c1');
    flushChat('c1', 'Общий');

    type('Привет');

    expect(textarea().value).toBe('Привет');
    expect(element.querySelector<HTMLButtonElement>('button[aria-label="Отправить"]')!.disabled).toBe(true);
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

  it('Chat_Switch_WithTypedText_ShouldClearTextAndLoadOtherChat', () => {
    open('c1');
    flushChat('c1', 'Общий');
    type('Черновик');

    open('c2');
    expect(element.textContent).toContain('Загрузка…');
    flushChat('c2', 'Арт');

    expect(textarea().value).toBe('');
    expect(TestBed.inject(TopbarTitle).text()).toBe('Арт');
  });

  it('Changes_Notify_WithRenamedChat_ShouldUpdateTitleAndKeepText', () => {
    open('c1');
    flushChat('c1', 'Общий');
    type('Черновик');

    TestBed.inject(ChatChanges).notify();
    render();
    flushChat('c1', 'Объявления');

    expect(TestBed.inject(TopbarTitle).text()).toBe('Объявления');
    expect(textarea().value).toBe('Черновик');
  });

  it('Changes_Notify_WithDeletedChat_ShouldNavigateHome', () => {
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    open('c1');
    flushChat('c1', 'Общий');

    TestBed.inject(ChatChanges).notify();
    render();
    expectInfo('c1').flush({ detail: 'Чат не найден по указанному идентификатору.' }, { status: 404, statusText: 'Not Found' });
    render();

    expect(navigate).toHaveBeenCalledWith('/');
  });

  it('Page_Destroy_WithLoadedChat_ShouldClearTopbarTitle', () => {
    open('c1');
    flushChat('c1', 'Общий');

    fixture.destroy();

    expect(TestBed.inject(TopbarTitle).text()).toBeNull();
  });
});
