import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AccountMenu } from '../../account/account-menu/account-menu';
import { ChatChanges } from '../../chats/chat-changes';
import { ChatHub } from '../../realtime/chat-hub';
import { RoomChangedNotify } from '../../realtime/chat-hub.models';
import { problemDetail } from '../../shared/api/api-response';
import { ResizeHandle } from '../../shared/resize/resize-handle';
import { RoomApi } from '../../rooms/room-api';
import { RoomExpansion } from '../../rooms/room-expansion';
import { RoomListItem } from '../../rooms/room-list-item/room-list-item';
import { RoomListModel, roomNameMaxLength } from '../../rooms/room.models';

const widthStorageKey = 'socium.sidebar.width';

@Component({
  selector: 'app-sidebar',
  imports: [AccountMenu, ResizeHandle, RoomListItem],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.css',
  host: {
    '[class.collapsed]': 'collapsed()',
    '[style.width.px]': 'collapsed() ? null : displayWidth()',
    '(window:resize)': 'onWindowResize()',
  },
})
export class Sidebar {
  private readonly roomApi = inject(RoomApi);
  private readonly roomExpansion = inject(RoomExpansion);
  private readonly chatChanges = inject(ChatChanges);
  private readonly chatHub = inject(ChatHub);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly nameMaxLength = roomNameMaxLength;

  protected readonly minWidth = 200;
  protected readonly defaultWidth = 260;
  private readonly viewportWidth = signal(window.innerWidth);
  protected readonly maxWidth = computed(() => Math.max(this.minWidth, Math.floor(this.viewportWidth() / 2)));
  /** Ширина, выбранная пользователем; в узком окне панель показывается ужатой до `maxWidth`, выбор не теряется. */
  protected readonly width = signal(this.restoreWidth());
  protected readonly displayWidth = computed(() => Math.min(this.width(), this.maxWidth()));

  protected readonly collapsed = signal(false);

  protected readonly rooms = signal<RoomListModel[]>([]);
  protected readonly loading = signal(false);
  protected readonly loadError = signal<string | null>(null);

  protected readonly search = signal('');
  protected readonly filteredRooms = computed(() => {
    const query = this.search().trim().toLocaleLowerCase();
    const rooms = this.rooms();
    return query ? rooms.filter((room) => room.name.toLocaleLowerCase().includes(query)) : rooms;
  });

  protected readonly creating = signal(false);
  protected readonly newRoomName = signal('');
  protected readonly saving = signal(false);
  protected readonly createError = signal<string | null>(null);

  /** Имя комнаты, которую нужно раскрыть после своего create (по событию roomChanged). */
  private pendingExpandRoomName: string | null = null;

  constructor() {
    this.loadRooms();
    void this.chatHub.ensureConnected();
    this.chatHub.roomChanged$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((change) => this.applyRoomChanged(change));
  }

  protected toggleCollapsed(): void {
    this.collapsed.update((value) => !value);
  }

  protected saveWidth(width: number): void {
    localStorage.setItem(widthStorageKey, String(width));
  }

  protected resetWidth(): void {
    this.width.set(this.defaultWidth);
    localStorage.removeItem(widthStorageKey);
  }

  protected onWindowResize(): void {
    this.viewportWidth.set(window.innerWidth);
  }

  private restoreWidth(): number {
    const saved = Number(localStorage.getItem(widthStorageKey));
    return Number.isFinite(saved) && saved >= this.minWidth ? saved : this.defaultWidth;
  }

  protected onSearchInput(event: Event): void {
    this.search.set((event.target as HTMLInputElement).value);
  }

  protected startCreate(): void {
    this.creating.set(true);
    this.newRoomName.set('');
    this.createError.set(null);
  }

  protected cancelCreate(): void {
    this.creating.set(false);
    this.createError.set(null);
  }

  protected onNewRoomNameInput(event: Event): void {
    this.newRoomName.set((event.target as HTMLInputElement).value);
  }

  protected createRoom(event: Event): void {
    event.preventDefault();
    const name = this.newRoomName().trim();
    if (!name || this.saving()) {
      return;
    }

    this.saving.set(true);
    this.createError.set(null);
    this.roomApi.create({ name }).subscribe({
      next: () => {
        this.saving.set(false);
        this.creating.set(false);
        this.pendingExpandRoomName = name;
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.createError.set(problemDetail(error, 'Не удалось создать комнату'));
      },
    });
  }

  protected onRoomDeleted(): void {
    this.chatChanges.notify();
  }

  protected loadRooms(onLoaded?: () => void): void {
    this.loading.set(true);
    this.loadError.set(null);
    this.roomApi.getList().subscribe({
      next: (body) => {
        const rooms = body.response?.rows ?? [];
        this.rooms.set(rooms);
        this.roomExpansion.retain(rooms.map((room) => room.id));
        this.loading.set(false);
        onLoaded?.();
      },
      error: (error: unknown) => {
        this.loadError.set(problemDetail(error, 'Не удалось загрузить комнаты'));
        this.loading.set(false);
      },
    });
  }

  private applyRoomChanged(change: RoomChangedNotify): void {
    const roomId = change.roomId;
    if (change.kind === 'deleted') {
      this.rooms.update((rooms) => rooms.filter((room) => room.id !== roomId));
      this.roomExpansion.retain(this.rooms().map((room) => room.id));
      this.chatChanges.notify();
      return;
    }

    const name = change.name?.trim();
    if (!name) {
      return;
    }

    if (change.kind === 'updated') {
      this.rooms.update((rooms) =>
        sortRooms(rooms.map((room) => (room.id === roomId ? { ...room, name } : room))),
      );
      return;
    }

    if (change.kind === 'created') {
      this.rooms.update((rooms) => {
        if (rooms.some((room) => room.id === roomId)) {
          return rooms;
        }
        return sortRooms([...rooms, { id: roomId, name }]);
      });
      if (this.pendingExpandRoomName === name) {
        this.roomExpansion.expand(roomId);
        this.pendingExpandRoomName = null;
      }
    }
  }
}

function sortRooms(rooms: RoomListModel[]): RoomListModel[] {
  return [...rooms].sort((left, right) => left.name.localeCompare(right.name, 'ru'));
}
