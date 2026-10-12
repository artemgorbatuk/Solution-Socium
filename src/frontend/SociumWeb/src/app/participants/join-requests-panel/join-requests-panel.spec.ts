import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { successBody } from '../../shared/api/api-response.testing';
import { JoinRequestPendingListModel } from '../join-request.models';
import { ParticipantsPanelState, joinRequestsPanelOpenStorageKey } from '../participants-panel-state';
import { JoinRequestsPanel } from './join-requests-panel';

describe('JoinRequestsPanel', () => {
  const agent: JoinRequestPendingListModel = {
    id: 'jr1',
    userId: 'u2',
    login: 'agent_demo',
    name: 'Agent',
  };

  let fixture: ComponentFixture<JoinRequestsPanel>;
  let http: HttpTestingController;
  let element: HTMLElement;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [JoinRequestsPanel],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(JoinRequestsPanel);
    fixture.componentRef.setInput('chatId', 'c1');
    element = fixture.nativeElement;
    fixture.detectChanges();
  });

  afterEach(() => {
    http.verify();
    localStorage.removeItem(joinRequestsPanelOpenStorageKey);
  });

  it('List_Load_WithPending_ShouldShowNameLoginAndActions', () => {
    expect(element.textContent).toContain('Загрузка…');

    http
      .expectOne(
        (request) =>
          request.method === 'GET' && request.url === '/api/joinRequest/pending' && request.params.get('chatId') === 'c1',
      )
      .flush(successBody({ rowCount: 1, rows: [agent] }));
    fixture.detectChanges();

    expect(element.querySelector('.count')!.textContent).toBe('1');
    expect(element.textContent).toContain('Agent');
    expect(element.textContent).toContain('@agent_demo');
    expect(element.textContent).toContain('Принять');
    expect(element.textContent).toContain('Отклонить');
  });

  it('Accept_Click_WithPending_ShouldRemoveRowAndNotifyCount', () => {
    http
      .expectOne((request) => request.method === 'GET' && request.url === '/api/joinRequest/pending')
      .flush(successBody({ rowCount: 1, rows: [agent] }));
    fixture.detectChanges();
    const panel = TestBed.inject(ParticipantsPanelState);
    panel.pendingJoinRequestCount.set(1);

    const accept = Array.from(element.querySelectorAll('button')).find((item) => item.textContent!.trim() === 'Принять')!;
    accept.click();

    http.expectOne({ method: 'POST', url: '/api/joinRequest/accept' }).flush(successBody({ id: 'jr1', participantId: 'p1' }));
    fixture.detectChanges();

    const reload = http.expectOne(
      (request) => request.method === 'GET' && request.url === '/api/joinRequest/pending',
    );
    reload.flush(successBody({ rowCount: 0, rows: [] }));
    fixture.detectChanges();

    expect(element.textContent).toContain('Нет заявок');
    expect(panel.pendingJoinRequestCount()).toBe(0);
  });
});
