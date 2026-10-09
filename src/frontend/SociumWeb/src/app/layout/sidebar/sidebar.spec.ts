import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ApiSuccessResponse } from '../../shared/api/api-response';
import { successBody } from '../../shared/api/api-response.testing';
import { RoomListModel, RoomListPageResponse } from '../../rooms/room.models';
import { Sidebar } from './sidebar';

function listBody(rows: RoomListModel[]): ApiSuccessResponse<RoomListPageResponse> {
  return successBody({ rowExists: rows.length > 0, rowCount: rows.length, rows });
}

describe('Sidebar', () => {
  let fixture: ComponentFixture<Sidebar>;
  let http: HttpTestingController;
  let element: HTMLElement;

  const rooms: RoomListModel[] = [
    { id: '1', name: 'Кухня' },
    { id: '2', name: 'Гостиная' },
  ];

  function render(): void {
    fixture.detectChanges();
  }

  function roomNames(): string[] {
    return Array.from(element.querySelectorAll('.room-name')).map((item) => item.textContent!.trim());
  }

  function input(selector: string, value: string): void {
    const field = element.querySelector<HTMLInputElement>(selector)!;
    field.value = value;
    field.dispatchEvent(new Event('input'));
    render();
  }

  function button(text: string): HTMLButtonElement {
    return Array.from(element.querySelectorAll('button')).find((item) => item.textContent!.includes(text))!;
  }

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      imports: [Sidebar],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(Sidebar);
    element = fixture.nativeElement;
    render();
  });

  afterEach(() => http.verify());

  it('List_Load_WithRooms_ShouldShowRooms', () => {
    http.expectOne('/api/room').flush(listBody(rooms));
    render();

    expect(roomNames()).toEqual(['Кухня', 'Гостиная']);
  });

  it('List_Load_WithNoRooms_ShouldShowPlaceholder', () => {
    http.expectOne('/api/room').flush(listBody([]));
    render();

    expect(element.querySelector('.rooms')!.textContent).toContain('Комнат пока нет');
  });

  it('List_Load_WithServerError_ShouldShowProblem', () => {
    http.expectOne('/api/room').flush({ detail: 'Не удалось отобразить страницу списка комнат' }, { status: 500, statusText: 'Error' });
    render();

    expect(element.querySelector('[role=alert]')!.textContent).toContain('Не удалось отобразить страницу списка комнат');
  });

  it('Search_Input_WithDifferentCase_ShouldFilterRoomsIgnoringCase', () => {
    http.expectOne('/api/room').flush(listBody(rooms));
    render();

    input('.search', 'кух');

    expect(roomNames()).toEqual(['Кухня']);
  });

  it('Panel_Toggle_WithExpandedPanel_ShouldCollapseAndExpand', () => {
    http.expectOne('/api/room').flush(listBody(rooms));
    render();

    button('«').click();
    render();
    expect(element.classList).toContain('collapsed');
    expect(element.querySelector('.rooms')).toBeNull();

    button('»').click();
    render();
    expect(element.classList).not.toContain('collapsed');
    expect(roomNames()).toEqual(['Кухня', 'Гостиная']);
  });

  it('CreateForm_Submit_WithPaddedName_ShouldCreateTrimmedAndReloadList', () => {
    http.expectOne('/api/room').flush(listBody([]));
    render();

    button('Новая комната').click();
    render();
    input('.create-form input', '  Кабинет  ');
    element.querySelector('form')!.dispatchEvent(new Event('submit'));

    const create = http.expectOne({ method: 'POST', url: '/api/room' });
    expect(create.request.body).toEqual({ name: 'Кабинет' });
    create.flush(listBody([]));

    http.expectOne({ method: 'GET', url: '/api/room' }).flush(listBody([{ id: '3', name: 'Кабинет' }]));
    render();

    expect(element.querySelector('.create-form')).toBeNull();
    expect(roomNames()).toEqual(['Кабинет']);
  });

  it('CreateForm_Submit_WithDuplicateName_ShouldShowProblemAndKeepForm', () => {
    http.expectOne('/api/room').flush(listBody(rooms));
    render();

    button('Новая комната').click();
    render();
    input('.create-form input', 'Кухня');
    element.querySelector('form')!.dispatchEvent(new Event('submit'));

    http
      .expectOne({ method: 'POST', url: '/api/room' })
      .flush({ detail: 'Комната с таким названием уже существует.' }, { status: 422, statusText: 'Unprocessable Entity' });
    render();

    expect(element.querySelector('.create-form')).not.toBeNull();
    expect(element.querySelector('[role=alert]')!.textContent).toContain('Комната с таким названием уже существует.');
  });

  it('RenameForm_Submit_WithNewName_ShouldReloadList', () => {
    http.expectOne('/api/room').flush(listBody(rooms));
    render();

    element.querySelector<HTMLButtonElement>('[aria-label="Действия с комнатой Кухня"]')!.click();
    render();
    button('Переименовать').click();
    render();
    input('.rename-form input', 'Столовая');
    element.querySelector<HTMLFormElement>('.rename-form')!.dispatchEvent(new Event('submit'));

    http.expectOne({ method: 'PUT', url: '/api/room' }).flush(successBody(null, 2));
    http.expectOne({ method: 'GET', url: '/api/room' }).flush(listBody([rooms[1], { id: '1', name: 'Столовая' }]));
    render();

    expect(roomNames()).toEqual(['Гостиная', 'Столовая']);
  });

  it('DeleteDialog_Confirm_WithExistingRoom_ShouldDeleteAndReloadList', () => {
    http.expectOne('/api/room').flush(listBody(rooms));
    render();

    element.querySelector<HTMLButtonElement>('[aria-label="Действия с комнатой Кухня"]')!.click();
    render();
    button('Удалить').click();
    render();

    http
      .expectOne((request) => request.url === '/api/room/delete' && request.params.get('id') === '1')
      .flush(successBody({ id: '1', name: 'Кухня' }));
    render();
    expect(element.querySelector('[role=dialog]')!.textContent).toContain('Кухня');

    element.querySelector<HTMLButtonElement>('[role=dialog] .danger')!.click();
    http
      .expectOne((request) => request.method === 'DELETE' && request.params.get('id') === '1')
      .flush(successBody({ isDeleted: true }, 2));
    http.expectOne({ method: 'GET', url: '/api/room' }).flush(listBody([rooms[1]]));
    render();

    expect(element.querySelector('[role=dialog]')).toBeNull();
    expect(roomNames()).toEqual(['Гостиная']);
  });

  describe('Width', () => {
    const initialWindowWidth = window.innerWidth;

    function setWindowWidth(width: number): void {
      Object.defineProperty(window, 'innerWidth', { value: width, configurable: true });
      window.dispatchEvent(new Event('resize'));
    }

    afterEach(() => setWindowWidth(initialWindowWidth));

    it('Width_Resize_WithNoSavedWidth_ShouldStartFromDefaultAndSave', () => {
      http.expectOne('/api/room').flush(listBody([]));
      render();
      expect(element.style.width).toBe('260px');

      element.querySelector('.resize-handle')!.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight' }));
      render();

      expect(element.style.width).toBe('276px');
      expect(localStorage.getItem('socium.sidebar.width')).toBe('276');
    });

    it('Width_DoubleClick_WithSavedWidth_ShouldRestoreThenReset', () => {
      http.expectOne('/api/room').flush(listBody([]));
      localStorage.setItem('socium.sidebar.width', '350');
      const restored = TestBed.createComponent(Sidebar);
      restored.detectChanges();
      http.expectOne('/api/room').flush(listBody([]));
      restored.detectChanges();
      const host: HTMLElement = restored.nativeElement;

      expect(host.style.width).toBe('350px');

      host.querySelector('.resize-handle')!.dispatchEvent(new MouseEvent('dblclick'));
      restored.detectChanges();

      expect(host.style.width).toBe('260px');
      expect(localStorage.getItem('socium.sidebar.width')).toBeNull();
    });

    it('Width_Load_WithSavedWidthBelowMinimum_ShouldUseDefault', () => {
      http.expectOne('/api/room').flush(listBody([]));
      localStorage.setItem('socium.sidebar.width', '50');
      const restored = TestBed.createComponent(Sidebar);
      restored.detectChanges();
      http.expectOne('/api/room').flush(listBody([]));
      restored.detectChanges();

      expect((restored.nativeElement as HTMLElement).style.width).toBe('260px');
    });

    it('Width_WindowResize_WithSavedWidthAboveHalf_ShouldLimitToHalfAndKeepChoice', () => {
      http.expectOne('/api/room').flush(listBody([]));
      setWindowWidth(1000);
      localStorage.setItem('socium.sidebar.width', '900');
      const restored = TestBed.createComponent(Sidebar);
      restored.detectChanges();
      http.expectOne('/api/room').flush(listBody([]));
      restored.detectChanges();
      const host: HTMLElement = restored.nativeElement;
      const handle = host.querySelector('.resize-handle')!;

      expect(host.style.width).toBe('500px');
      expect(handle.getAttribute('aria-valuemax')).toBe('500');

      setWindowWidth(1600);
      restored.detectChanges();
      expect(host.style.width).toBe('800px');

      setWindowWidth(2400);
      restored.detectChanges();
      expect(host.style.width).toBe('900px');
    });

    it('Width_WindowResize_WithNarrowWindow_ShouldKeepMinimum', () => {
      http.expectOne('/api/room').flush(listBody([]));
      render();

      setWindowWidth(300);
      render();

      expect(element.style.width).toBe('200px');
    });

    it('Width_Collapse_WithExpandedPanel_ShouldDropWidthAndHandle', () => {
      http.expectOne('/api/room').flush(listBody([]));
      render();

      button('«').click();
      render();

      expect(element.style.width).toBe('');
      expect(element.querySelector('.resize-handle')).toBeNull();
    });
  });

  it('CreateForm_Input_WithBlankName_ShouldDisableCreate', () => {
    http.expectOne('/api/room').flush(listBody([]));
    render();

    button('Новая комната').click();
    render();
    input('.create-form input', '   ');

    expect(button('Создать').disabled).toBe(true);
  });
});
