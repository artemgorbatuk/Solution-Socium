import { Component, ElementRef, Injector, afterNextRender, computed, inject, input, output, signal, viewChild } from '@angular/core';
import { problemDetail } from '../../shared/api/api-response';
import { Icon } from '../../shared/icon/icon';
import { MessageApi } from '../message-api';
import { MessageDeleteConfirm } from '../message-delete-confirm/message-delete-confirm';
import { messageFullTime, messageTime } from '../message-time';
import { MessageListModel } from '../message.models';
import { isSubmitKey } from '../submit-key';

@Component({
  selector: 'app-message-item',
  imports: [MessageDeleteConfirm, Icon],
  templateUrl: './message-item.html',
  styleUrl: './message-item.css',
})
export class MessageItem {
  private readonly messageApi = inject(MessageApi);
  private readonly injector = inject(Injector);

  readonly message = input.required<MessageListModel>();
  /** Текст изменён — владелец перезагружает ленту. */
  readonly changed = output<void>();
  /** Сообщение удалено — владелец перезагружает ленту. */
  readonly deleted = output<void>();

  protected readonly time = computed(() => messageTime(this.message().createdAt));
  protected readonly fullTime = computed(() => messageFullTime(this.message().createdAt));

  protected readonly editing = signal(false);
  protected readonly draftText = signal('');
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly confirmingDelete = signal(false);

  private readonly editInput = viewChild<ElementRef<HTMLTextAreaElement>>('editInput');

  protected startEdit(): void {
    this.draftText.set(this.message().text);
    this.error.set(null);
    this.editing.set(true);
    afterNextRender(
      () => {
        const field = this.editInput()?.nativeElement;
        field?.focus();
        field?.setSelectionRange(field.value.length, field.value.length);
      },
      { injector: this.injector },
    );
  }

  protected cancelEdit(): void {
    if (!this.saving()) {
      this.editing.set(false);
      this.error.set(null);
    }
  }

  protected onDraftInput(event: Event): void {
    this.draftText.set((event.target as HTMLTextAreaElement).value);
  }

  protected onEditKeydown(event: KeyboardEvent): void {
    if (isSubmitKey(event)) {
      this.save(event);
    }
  }

  protected save(event: Event): void {
    event.preventDefault();
    const text = this.draftText().trim();
    if (!text || this.saving()) {
      return;
    }
    if (text === this.message().text) {
      this.editing.set(false);
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    this.messageApi.update({ id: this.message().id, text }).subscribe({
      next: () => {
        this.saving.set(false);
        this.editing.set(false);
        this.changed.emit();
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.error.set(problemDetail(error, 'Не удалось изменить сообщение'));
      },
    });
  }

  protected requestDelete(): void {
    this.confirmingDelete.set(true);
  }
}
