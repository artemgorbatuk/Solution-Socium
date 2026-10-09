import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { successBody } from '../../shared/api/api-response.testing';
import { RoomListModel } from '../room.models';
import { RoomListItem } from './room-list-item';

describe('RoomListItem', () => {
  const room: RoomListModel = { id: '1', name: 'Кухня' };

  let fixture: ComponentFixture<RoomListItem>;
  let http: HttpTestingController;
  let element: HTMLElement;
  let changed: number;
  let deleteRequested: RoomListModel[];

  function render(): void {
    fixture.detectChanges();
  }

  function button(text: string): HTMLButtonElement {
    return Array.from(element.querySelectorAll('button')).find((item) => item.textContent!.includes(text))!;
  }

  function openMenu(): void {
    element.querySelector<HTMLButtonElement>('.menu-button')!.click();
    render();
  }

  function startRename(value: string): void {
    openMenu();
    button('Переименовать').click();
    render();
    const field = element.querySelector<HTMLInputElement>('.rename-form input')!;
    field.value = value;
    field.dispatchEvent(new Event('input'));
    render();
  }

  function submitRename(): void {
    element.querySelector('form')!.dispatchEvent(new Event('submit'));
    render();
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [RoomListItem],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(RoomListItem);
    fixture.componentRef.setInput('room', room);
    changed = 0;
    deleteRequested = [];
    fixture.componentInstance.changed.subscribe(() => changed++);
    fixture.componentInstance.deleteRequested.subscribe((value) => deleteRequested.push(value));
    element = fixture.nativeElement;
    render();
  });

  afterEach(() => http.verify());

  it('Row_Render_WithExistingRoom_ShouldShowNameAndMenuButton', () => {
    expect(element.querySelector('.room-name')!.textContent).toContain('Кухня');
    expect(element.querySelector('.menu-button')!.getAttribute('aria-label')).toBe('Действия с комнатой Кухня');
  });

  it('Menu_Click_WithOutsideClickAfterOpen_ShouldOpenThenClose', () => {
    openMenu();
    expect(element.querySelector('[role=menu]')).not.toBeNull();

    document.body.click();
    render();

    expect(element.querySelector('[role=menu]')).toBeNull();
  });

  it('Menu_Escape_WithOpenMenu_ShouldClose', () => {
    openMenu();

    element.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    render();

    expect(element.querySelector('[role=menu]')).toBeNull();
  });

  it('RenameForm_Submit_WithPaddedName_ShouldSendTrimmedNameAndEmitChanged', () => {
    startRename('  Столовая  ');
    submitRename();

    const request = http.expectOne({ method: 'PUT', url: '/api/room' });
    expect(request.request.body).toEqual({ id: '1', name: 'Столовая' });
    request.flush(successBody(null, 2));
    render();

    expect(changed).toBe(1);
    expect(element.querySelector('.rename-form')).toBeNull();
  });

  it('RenameForm_Submit_WithUnchangedName_ShouldCloseWithoutRequest', () => {
    startRename(' Кухня ');
    submitRename();

    http.expectNone({ method: 'PUT', url: '/api/room' });
    expect(element.querySelector('.rename-form')).toBeNull();
    expect(changed).toBe(0);
  });

  it('RenameForm_Submit_WithDuplicateName_ShouldShowProblemAndKeepForm', () => {
    startRename('Гостиная');
    submitRename();

    http
      .expectOne({ method: 'PUT', url: '/api/room' })
      .flush({ detail: 'Комната с таким названием уже существует.' }, { status: 422, statusText: 'Unprocessable Entity' });
    render();

    expect(element.querySelector('.rename-form')).not.toBeNull();
    expect(element.querySelector('[role=alert]')!.textContent).toContain('Комната с таким названием уже существует.');
    expect(changed).toBe(0);
  });

  it('RenameForm_Escape_WithDraftName_ShouldCancelRename', () => {
    startRename('Столовая');

    element.querySelector('.rename-form input')!.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    render();

    expect(element.querySelector('.rename-form')).toBeNull();
    expect(element.querySelector('.room-name')!.textContent).toContain('Кухня');
  });

  it('RenameForm_Input_WithBlankName_ShouldDisableSave', () => {
    startRename('   ');

    expect(button('Сохранить').disabled).toBe(true);
  });

  it('Menu_Delete_WithExistingRoom_ShouldEmitDeleteRequested', () => {
    openMenu();
    button('Удалить').click();
    render();

    expect(deleteRequested).toEqual([room]);
    expect(element.querySelector('[role=menu]')).toBeNull();
  });
});
