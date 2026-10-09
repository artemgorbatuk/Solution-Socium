import { Component, ElementRef, OnInit, afterNextRender, inject, input, output, signal, viewChild } from '@angular/core';
import { problemDetail } from '../../shared/api/api-response';
import { RoomApi } from '../room-api';
import { RoomDeletePageResponse } from '../room.models';

@Component({
  selector: 'app-room-delete-dialog',
  templateUrl: './room-delete-dialog.html',
  styleUrl: './room-delete-dialog.css',
  host: {
    '(document:keydown.escape)': 'close()',
  },
})
export class RoomDeleteDialog implements OnInit {
  private readonly roomApi = inject(RoomApi);

  readonly roomId = input.required<string>();
  readonly deleted = output<void>();
  readonly closed = output<void>();

  protected readonly room = signal<RoomDeletePageResponse | null>(null);
  protected readonly loading = signal(true);
  protected readonly deleting = signal(false);
  protected readonly error = signal<string | null>(null);

  private readonly cancelButton = viewChild<ElementRef<HTMLButtonElement>>('cancelButton');

  constructor() {
    afterNextRender(() => this.cancelButton()?.nativeElement.focus());
  }

  ngOnInit(): void {
    this.roomApi.getDeletePage(this.roomId()).subscribe({
      next: (body) => {
        this.room.set(body.response);
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.error.set(problemDetail(error, 'Не удалось загрузить комнату'));
        this.loading.set(false);
      },
    });
  }

  protected confirm(): void {
    const room = this.room();
    if (!room || this.deleting()) {
      return;
    }

    this.deleting.set(true);
    this.error.set(null);
    this.roomApi.delete(room.id).subscribe({
      next: () => this.deleted.emit(),
      error: (error: unknown) => {
        this.deleting.set(false);
        this.error.set(problemDetail(error, 'Не удалось удалить комнату'));
      },
    });
  }

  protected close(): void {
    if (!this.deleting()) {
      this.closed.emit();
    }
  }
}
