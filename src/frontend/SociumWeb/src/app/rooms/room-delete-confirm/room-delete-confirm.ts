import { Component, ElementRef, OnInit, afterNextRender, inject, input, output, signal, viewChild } from '@angular/core';
import { problemDetail } from '../../shared/api/api-response';
import { RoomApi } from '../room-api';
import { RoomDeletePageResponse } from '../room.models';

@Component({
  selector: 'app-room-delete-confirm',
  templateUrl: './room-delete-confirm.html',
  styleUrl: './room-delete-confirm.css',
  host: {
    role: 'group',
    'aria-label': 'Удалить комнату?',
    '(keydown.escape)': 'cancel()',
  },
})
export class RoomDeleteConfirm implements OnInit {
  private readonly roomApi = inject(RoomApi);

  readonly roomId = input.required<string>();
  /** Название из списка — вопрос виден сразу, до ответа сервера. */
  readonly roomName = input.required<string>();
  readonly deleted = output<void>();
  readonly cancelled = output<void>();

  /** Данные с сервера; пока их нет, удалить нельзя. */
  protected readonly room = signal<RoomDeletePageResponse | null>(null);
  protected readonly deleting = signal(false);
  protected readonly error = signal<string | null>(null);

  private readonly cancelButton = viewChild<ElementRef<HTMLButtonElement>>('cancelButton');

  constructor() {
    afterNextRender(() => this.cancelButton()?.nativeElement.focus());
  }

  ngOnInit(): void {
    this.roomApi.getDeletePage(this.roomId()).subscribe({
      next: (body) => this.room.set(body.response),
      error: (error: unknown) => this.error.set(problemDetail(error, 'Не удалось загрузить комнату')),
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

  protected cancel(): void {
    if (!this.deleting()) {
      this.cancelled.emit();
    }
  }
}
