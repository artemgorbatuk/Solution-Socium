import { Component, ElementRef, OnInit, afterNextRender, inject, input, output, signal, viewChild } from '@angular/core';
import { problemDetail } from '../../shared/api/api-response';
import { ChatApi } from '../chat-api';
import { ChatDeletePageResponse } from '../chat.models';

@Component({
  selector: 'app-chat-delete-dialog',
  templateUrl: './chat-delete-dialog.html',
  styleUrl: './chat-delete-dialog.css',
  host: {
    '(document:keydown.escape)': 'close()',
  },
})
export class ChatDeleteDialog implements OnInit {
  private readonly chatApi = inject(ChatApi);

  readonly chatId = input.required<string>();
  readonly deleted = output<void>();
  readonly closed = output<void>();

  protected readonly chat = signal<ChatDeletePageResponse | null>(null);
  protected readonly loading = signal(true);
  protected readonly deleting = signal(false);
  protected readonly error = signal<string | null>(null);

  private readonly cancelButton = viewChild<ElementRef<HTMLButtonElement>>('cancelButton');

  constructor() {
    afterNextRender(() => this.cancelButton()?.nativeElement.focus());
  }

  ngOnInit(): void {
    this.chatApi.getDeletePage(this.chatId()).subscribe({
      next: (body) => {
        this.chat.set(body.response);
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.error.set(problemDetail(error, 'Не удалось загрузить чат'));
        this.loading.set(false);
      },
    });
  }

  protected confirm(): void {
    const chat = this.chat();
    if (!chat || this.deleting()) {
      return;
    }

    this.deleting.set(true);
    this.error.set(null);
    this.chatApi.delete(chat.id).subscribe({
      next: () => this.deleted.emit(),
      error: (error: unknown) => {
        this.deleting.set(false);
        this.error.set(problemDetail(error, 'Не удалось удалить чат'));
      },
    });
  }

  protected close(): void {
    if (!this.deleting()) {
      this.closed.emit();
    }
  }
}
