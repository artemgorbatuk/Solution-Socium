import { Component, ElementRef, OnInit, afterNextRender, computed, inject, input, output, signal, viewChild } from '@angular/core';
import { problemDetail } from '../../shared/api/api-response';
import { MessageApi } from '../message-api';
import { MessageDeletePageResponse } from '../message.models';

/** Сколько символов текста показывать в подтверждении; остальное обрезается многоточием. */
const previewLength = 300;

@Component({
  selector: 'app-message-delete-dialog',
  templateUrl: './message-delete-dialog.html',
  styleUrl: './message-delete-dialog.css',
  host: {
    '(document:keydown.escape)': 'close()',
  },
})
export class MessageDeleteDialog implements OnInit {
  private readonly messageApi = inject(MessageApi);

  readonly messageId = input.required<string>();
  readonly deleted = output<void>();
  readonly closed = output<void>();

  protected readonly message = signal<MessageDeletePageResponse | null>(null);
  protected readonly loading = signal(true);
  protected readonly deleting = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly preview = computed(() => {
    const text = this.message()?.text ?? '';
    return text.length > previewLength ? `${text.slice(0, previewLength)}…` : text;
  });

  private readonly cancelButton = viewChild<ElementRef<HTMLButtonElement>>('cancelButton');

  constructor() {
    afterNextRender(() => this.cancelButton()?.nativeElement.focus());
  }

  ngOnInit(): void {
    this.messageApi.getDeletePage(this.messageId()).subscribe({
      next: (body) => {
        this.message.set(body.response);
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.error.set(problemDetail(error, 'Не удалось загрузить сообщение'));
        this.loading.set(false);
      },
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

  protected close(): void {
    if (!this.deleting()) {
      this.closed.emit();
    }
  }
}
