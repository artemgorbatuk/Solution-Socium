import { Component, ElementRef, OnInit, afterNextRender, inject, input, output, signal, viewChild } from '@angular/core';
import { problemDetail } from '../../shared/api/api-response';
import { MessageApi } from '../message-api';
import { MessageDeletePageResponse } from '../message.models';

@Component({
  selector: 'app-message-delete-confirm',
  templateUrl: './message-delete-confirm.html',
  styleUrl: './message-delete-confirm.css',
  host: {
    role: 'group',
    'aria-label': 'Удалить сообщение?',
    '(keydown.escape)': 'cancel()',
  },
})
export class MessageDeleteConfirm implements OnInit {
  private readonly messageApi = inject(MessageApi);

  readonly messageId = input.required<string>();
  readonly deleted = output<void>();
  readonly cancelled = output<void>();

  /** Данные с сервера; пока их нет, удалить нельзя. */
  protected readonly message = signal<MessageDeletePageResponse | null>(null);
  protected readonly deleting = signal(false);
  protected readonly error = signal<string | null>(null);

  private readonly cancelButton = viewChild<ElementRef<HTMLButtonElement>>('cancelButton');

  constructor() {
    afterNextRender(() => this.cancelButton()?.nativeElement.focus());
  }

  ngOnInit(): void {
    this.messageApi.getDeletePage(this.messageId()).subscribe({
      next: (body) => this.message.set(body.response),
      error: (error: unknown) => this.error.set(problemDetail(error, 'Не удалось загрузить сообщение')),
    });
  }

  protected confirm(): void {
    const message = this.message();
    if (!message || this.deleting()) {
      return;
    }

    this.deleting.set(true);
    this.error.set(null);
    this.messageApi.delete(message.id).subscribe({
      next: () => this.deleted.emit(),
      error: (error: unknown) => {
        this.deleting.set(false);
        this.error.set(problemDetail(error, 'Не удалось удалить сообщение'));
      },
    });
  }

  protected cancel(): void {
    if (!this.deleting()) {
      this.cancelled.emit();
    }
  }
}
