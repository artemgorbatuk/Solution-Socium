import { Component, ElementRef, OnInit, afterNextRender, inject, input, output, signal, viewChild } from '@angular/core';
import { problemDetail } from '../../shared/api/api-response';
import { ChatApi } from '../chat-api';
import { ChatDeletePageResponse } from '../chat.models';

@Component({
  selector: 'app-chat-delete-confirm',
  templateUrl: './chat-delete-confirm.html',
  styleUrl: './chat-delete-confirm.css',
  host: {
    role: 'group',
    'aria-label': 'Удалить чат?',
    '(keydown.escape)': 'cancel()',
  },
})
export class ChatDeleteConfirm implements OnInit {
  private readonly chatApi = inject(ChatApi);

  readonly chatId = input.required<string>();
  /** Название из списка — вопрос виден сразу, до ответа сервера. */
  readonly chatName = input.required<string>();
  readonly deleted = output<void>();
  readonly cancelled = output<void>();

  /** Данные с сервера; пока их нет, удалить нельзя. */
  protected readonly chat = signal<ChatDeletePageResponse | null>(null);
  protected readonly deleting = signal(false);
  protected readonly error = signal<string | null>(null);

  private readonly cancelButton = viewChild<ElementRef<HTMLButtonElement>>('cancelButton');

  constructor() {
    afterNextRender(() => this.cancelButton()?.nativeElement.focus());
  }

  ngOnInit(): void {
    this.chatApi.getDeletePage(this.chatId()).subscribe({
      next: (body) => this.chat.set(body.response),
      error: (error: unknown) => this.error.set(problemDetail(error, 'Не удалось загрузить чат')),
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

  protected cancel(): void {
    if (!this.deleting()) {
      this.cancelled.emit();
    }
  }
}
