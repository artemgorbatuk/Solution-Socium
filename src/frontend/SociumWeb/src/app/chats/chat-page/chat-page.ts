import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, effect, inject, input, signal, untracked } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TopbarTitle } from '../../layout/topbar/topbar-title';
import { ChatMessages } from '../../messages/chat-messages/chat-messages';
import { ParticipantApi } from '../../participants/participant-api';
import { ParticipantsPanel } from '../../participants/participants-panel/participants-panel';
import { ParticipantsPanelState } from '../../participants/participants-panel-state';
import { problemDetail } from '../../shared/api/api-response';
import { CurrentUser } from '../../users/current-user';
import { ChatApi } from '../chat-api';
import { ChatChanges } from '../chat-changes';
import { ChatInfoPageResponse } from '../chat.models';

@Component({
  selector: 'app-chat-page',
  imports: [RouterLink, ChatMessages, ParticipantsPanel],
  templateUrl: './chat-page.html',
  styleUrl: './chat-page.css',
})
export class ChatPage {
  private readonly chatApi = inject(ChatApi);
  private readonly participantApi = inject(ParticipantApi);
  private readonly chatChanges = inject(ChatChanges);
  private readonly topbarTitle = inject(TopbarTitle);
  private readonly router = inject(Router);

  protected readonly participantsPanel = inject(ParticipantsPanelState);
  protected readonly currentUserId = inject(CurrentUser).id;

  /** Из маршрута `chat/:id`. */
  readonly id = input.required<string>();

  protected readonly chat = signal<ChatInfoPageResponse | null>(null);
  protected readonly loading = signal(true);
  protected readonly notFound = signal(false);
  protected readonly loadError = signal<string | null>(null);

  protected readonly joining = signal(false);
  protected readonly joinError = signal<string | null>(null);

  constructor() {
    effect((onCleanup) => {
      const id = this.id();
      this.chatChanges.version();
      this.currentUserId();
      untracked(() => onCleanup(this.load(id)));
    });
    effect(() => this.topbarTitle.text.set(this.chat()?.name ?? null));
    effect(() => this.participantsPanel.available.set(this.chat()?.isParticipant ?? false));
    inject(DestroyRef).onDestroy(() => {
      this.topbarTitle.text.set(null);
      this.participantsPanel.available.set(false);
    });
  }

  protected join(chatId: string): void {
    if (this.joining()) {
      return;
    }

    this.joining.set(true);
    this.joinError.set(null);
    this.participantApi.join(chatId).subscribe({
      next: () => {
        this.joining.set(false);
        this.chatChanges.notify();
      },
      error: (error: unknown) => {
        this.joining.set(false);
        this.joinError.set(problemDetail(error, 'Не удалось вступить в чат'));
      },
    });
  }

  /** Загружает чат; возвращает отмену запроса. */
  private load(id: string): () => void {
    const isReload = this.chat()?.id === id;
    if (!isReload) {
      this.chat.set(null);
    }
    this.loading.set(true);
    this.notFound.set(false);
    this.loadError.set(null);
    this.joinError.set(null);

    const subscription = this.chatApi.getInfo(id).subscribe({
      next: (body) => {
        this.chat.set(body.response);
        this.notFound.set(!body.response);
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.loading.set(false);
        if (!isNotFound(error)) {
          this.loadError.set(problemDetail(error, 'Не удалось загрузить чат'));
        } else if (isReload) {
          void this.router.navigateByUrl('/');
        } else {
          this.notFound.set(true);
        }
      },
    });
    return () => subscription.unsubscribe();
  }
}

/** `400` — `id` в адресе не является `Guid`, `404` — чат удалён или не существовал. */
function isNotFound(error: unknown): boolean {
  return error instanceof HttpErrorResponse && (error.status === 400 || error.status === 404);
}
