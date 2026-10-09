import { Component, ElementRef, Injector, afterNextRender, inject, input, output, signal, viewChild } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { problemDetail } from '../../shared/api/api-response';
import { Icon } from '../../shared/icon/icon';
import { ChatApi } from '../chat-api';
import { ChatDeleteConfirm } from '../chat-delete-confirm/chat-delete-confirm';
import { ChatListModel, chatNameMaxLength } from '../chat.models';

@Component({
  selector: 'app-chat-list-item',
  imports: [RouterLink, RouterLinkActive, ChatDeleteConfirm, Icon],
  templateUrl: './chat-list-item.html',
  styleUrl: './chat-list-item.css',
})
export class ChatListItem {
  private readonly chatApi = inject(ChatApi);
  private readonly injector = inject(Injector);

  readonly chat = input.required<ChatListModel>();
  /** Чат переименован — владелец перезагружает список. */
  readonly changed = output<void>();
  /** Чат удалён — владелец перезагружает список. */
  readonly deleted = output<void>();

  protected readonly nameMaxLength = chatNameMaxLength;
  protected readonly editing = signal(false);
  protected readonly confirmingDelete = signal(false);
  protected readonly draftName = signal('');
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  private readonly nameInput = viewChild<ElementRef<HTMLInputElement>>('nameInput');

  protected startRename(): void {
    this.draftName.set(this.chat().name);
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
    if (name === this.chat().name) {
      this.editing.set(false);
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    this.chatApi.update({ id: this.chat().id, name }).subscribe({
      next: () => {
        this.saving.set(false);
        this.editing.set(false);
        this.changed.emit();
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.error.set(problemDetail(error, 'Не удалось переименовать чат'));
      },
    });
  }

  protected requestDelete(): void {
    this.confirmingDelete.set(true);
  }
}
