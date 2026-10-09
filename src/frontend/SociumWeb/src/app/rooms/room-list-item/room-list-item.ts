import { Component, ElementRef, Injector, afterNextRender, computed, inject, input, output, signal, viewChild } from '@angular/core';
import { RoomChats } from '../../chats/room-chats/room-chats';
import { problemDetail } from '../../shared/api/api-response';
import { Icon } from '../../shared/icon/icon';
import { RoomApi } from '../room-api';
import { RoomDeleteConfirm } from '../room-delete-confirm/room-delete-confirm';
import { RoomExpansion } from '../room-expansion';
import { RoomListModel, roomNameMaxLength } from '../room.models';

@Component({
  selector: 'app-room-list-item',
  imports: [RoomChats, RoomDeleteConfirm, Icon],
  templateUrl: './room-list-item.html',
  styleUrl: './room-list-item.css',
})
export class RoomListItem {
  private readonly roomApi = inject(RoomApi);
  private readonly expansion = inject(RoomExpansion);
  private readonly injector = inject(Injector);

  readonly room = input.required<RoomListModel>();
  /** Комната переименована — владелец перезагружает список. */
  readonly changed = output<void>();
  /** Комната удалена — владелец перезагружает список. */
  readonly deleted = output<void>();

  protected readonly nameMaxLength = roomNameMaxLength;
  protected readonly editing = signal(false);
  protected readonly confirmingDelete = signal(false);
  protected readonly draftName = signal('');
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly expanded = computed(() => this.expansion.isExpanded(this.room().id));
  protected readonly creatingChat = signal(false);

  private readonly nameInput = viewChild<ElementRef<HTMLInputElement>>('nameInput');

  protected toggleExpanded(): void {
    this.expansion.toggle(this.room().id);
  }

  protected startCreateChat(): void {
    this.expansion.expand(this.room().id);
    this.creatingChat.set(true);
  }

  protected startRename(): void {
    this.draftName.set(this.room().name);
    this.error.set(null);
    this.editing.set(true);
    afterNextRender(() => this.nameInput()?.nativeElement.select(), { injector: this.injector });
  }

  protected cancelRename(): void {
    this.editing.set(false);
    this.error.set(null);
  }

  protected onDraftInput(event: Event): void {
    this.draftName.set((event.target as HTMLInputElement).value);
  }

  protected save(event: Event): void {
    event.preventDefault();
    const name = this.draftName().trim();
    if (!name || this.saving()) {
      return;
    }
    if (name === this.room().name) {
      this.editing.set(false);
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    this.roomApi.update({ id: this.room().id, name }).subscribe({
      next: () => {
        this.saving.set(false);
        this.editing.set(false);
        this.changed.emit();
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.error.set(problemDetail(error, 'Не удалось переименовать комнату'));
      },
    });
  }

  protected requestDelete(): void {
    this.confirmingDelete.set(true);
  }
}
