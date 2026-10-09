import { Component, ElementRef, Injector, afterNextRender, inject, input, output, signal, viewChild } from '@angular/core';
import { problemDetail } from '../../shared/api/api-response';
import { RoomApi } from '../room-api';
import { RoomListModel, roomNameMaxLength } from '../room.models';

@Component({
  selector: 'app-room-list-item',
  templateUrl: './room-list-item.html',
  styleUrl: './room-list-item.css',
  host: {
    '(document:click)': 'onDocumentClick($event)',
    '(keydown.escape)': 'closeMenu()',
  },
})
export class RoomListItem {
  private readonly roomApi = inject(RoomApi);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly injector = inject(Injector);

  readonly room = input.required<RoomListModel>();
  /** Комната переименована — владелец перезагружает список. */
  readonly changed = output<void>();
  readonly deleteRequested = output<RoomListModel>();

  protected readonly nameMaxLength = roomNameMaxLength;
  protected readonly menuOpen = signal(false);
  protected readonly editing = signal(false);
  protected readonly draftName = signal('');
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  private readonly nameInput = viewChild<ElementRef<HTMLInputElement>>('nameInput');

  protected toggleMenu(): void {
    this.menuOpen.update((value) => !value);
  }

  protected closeMenu(): void {
    this.menuOpen.set(false);
  }

  protected onDocumentClick(event: MouseEvent): void {
    if (this.menuOpen() && !this.host.nativeElement.contains(event.target as Node)) {
      this.menuOpen.set(false);
    }
  }

  protected startRename(): void {
    this.menuOpen.set(false);
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
    this.menuOpen.set(false);
    this.deleteRequested.emit(this.room());
  }
}
