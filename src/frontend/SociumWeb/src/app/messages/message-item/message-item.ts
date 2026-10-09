import { Component, ElementRef, Injector, afterNextRender, computed, inject, input, output, signal, viewChild } from '@angular/core';
import { problemDetail } from '../../shared/api/api-response';
import { MessageApi } from '../message-api';
import { messageFullTime, messageTime } from '../message-time';
import { MessageListModel } from '../message.models';
import { isSubmitKey } from '../submit-key';

@Component({
  selector: 'app-message-item',
  templateUrl: './message-item.html',
  styleUrl: './message-item.css',
  host: {
    '(document:click)': 'onDocumentClick($event)',
    '(keydown.escape)': 'closeMenu()',
  },
})
export class MessageItem {
  private readonly messageApi = inject(MessageApi);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly injector = inject(Injector);

  readonly message = input.required<MessageListModel>();
  /** Текст изменён — владелец перезагружает ленту. */
  readonly changed = output<void>();
  readonly deleteRequested = output<MessageListModel>();

  protected readonly time = computed(() => messageTime(this.message().createdAt));
  protected readonly fullTime = computed(() => messageFullTime(this.message().createdAt));

  protected readonly menuOpen = signal(false);
  protected readonly editing = signal(false);
  protected readonly draftText = signal('');
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  private readonly editInput = viewChild<ElementRef<HTMLTextAreaElement>>('editInput');

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

  protected startEdit(): void {
    this.menuOpen.set(false);
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
    this.menuOpen.set(false);
    this.deleteRequested.emit(this.message());
  }
}
