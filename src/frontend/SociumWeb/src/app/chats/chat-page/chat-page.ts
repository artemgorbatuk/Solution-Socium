import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, effect, inject, input, signal, untracked } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { TopbarTitle } from '../../layout/topbar/topbar-title';
import { ChatMessages } from '../../messages/chat-messages/chat-messages';
import { JoinRequestApi } from '../../participants/join-request-api';
import { JoinRequestsPanel } from '../../participants/join-requests-panel/join-requests-panel';
import { ParticipantsPanel } from '../../participants/participants-panel/participants-panel';
import { ParticipantsPanelState } from '../../participants/participants-panel-state';
import { ChatHub } from '../../realtime/chat-hub';
import { problemDetail } from '../../shared/api/api-response';
import { CurrentUser } from '../../users/current-user';
import { ChatApi } from '../chat-api';
import { ChatChanges } from '../chat-changes';
import { ChatInfoPageResponse } from '../chat.models';

@Component({
  selector: 'app-chat-page',
  imports: [RouterLink, ChatMessages, ParticipantsPanel, JoinRequestsPanel],
  templateUrl: './chat-page.html',
  styleUrl: './chat-page.css',
})
export class ChatPage {
  private readonly chatApi = inject(ChatApi);
  private readonly joinRequestApi = inject(JoinRequestApi);
  private readonly chatChanges = inject(ChatChanges);
  private readonly chatHub = inject(ChatHub);
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

  protected readonly requesting = signal(false);
  protected readonly requestError = signal<string | null>(null);

  private hubSubscription: Subscription | null = null;

  constructor() {
    effect((onCleanup) => {
      const id = this.id();
      this.chatChanges.version();
      this.currentUserId();
      untracked(() => onCleanup(this.load(id)));
    });
    effect(() => this.topbarTitle.text.set(this.chat()?.name ?? null));
    effect(() => {
      const chat = this.chat();
      this.participantsPanel.available.set(chat?.isParticipant ?? false);
      this.participantsPanel.requestsAvailable.set(chat?.canManageParticipants ?? false);
      this.participantsPanel.pendingJoinRequestCount.set(
        chat?.canManageParticipants ? (chat.pendingJoinRequestCount ?? 0) : 0,
      );
    });
    effect((onCleanup) => {
      const chat = this.chat();
      const userId = this.currentUserId();
      untracked(() => {
        this.hubSubscription?.unsubscribe();
        this.hubSubscription = null;
        if (!chat || !userId) {
          return;
        }
        const subscription = new Subscription();
        subscription.add(
          this.chatHub.joinRequestChanged$.subscribe((event) => {
            if (event.chatId !== chat.id) {
              return;
            }
            if (chat.canManageParticipants) {
              this.participantsPanel.pendingJoinRequestCount.set(event.pendingCount);
            }
            if (event.userId === userId && (event.kind === 'accepted' || event.kind === 'rejected')) {
              this.chatChanges.notify();
            }
            if (event.userId === userId && event.kind === 'created') {
              this.chat.update((current) =>
                current ? { ...current, hasPendingJoinRequest: true } : current,
              );
            }
          }),
        );
        subscription.add(
          this.chatHub.participantLeft$.subscribe((event) => {
            if (event.chatId !== chat.id || event.userId !== userId) {
              return;
            }
            // Kick / leave: убрать ленту; reload info — 403 без ChatRead → уход на главную.
            void this.chatHub.leaveChat(chat.id);
            this.chat.update((current) =>
              current
                ? {
                    ...current,
                    isParticipant: false,
                    isAdmin: false,
                    canManageParticipants: false,
                    pendingJoinRequestCount: 0,
                  }
                : current,
            );
            this.chatChanges.notify();
          }),
        );
        subscription.add(
          this.chatHub.chatChanged$.subscribe((event) => {
            if (event.kind !== 'deleted' || event.chatId !== chat.id) {
              return;
            }
            void this.chatHub.leaveChat(chat.id);
            void this.router.navigateByUrl('/');
          }),
        );
        this.hubSubscription = subscription;
      });
      onCleanup(() => {
        this.hubSubscription?.unsubscribe();
        this.hubSubscription = null;
      });
    });
    inject(DestroyRef).onDestroy(() => {
      this.topbarTitle.clear();
      this.participantsPanel.available.set(false);
      this.participantsPanel.requestsAvailable.set(false);
      this.participantsPanel.pendingJoinRequestCount.set(0);
      this.hubSubscription?.unsubscribe();
    });
  }

  protected requestJoin(chatId: string): void {
    if (this.requesting()) {
      return;
    }

    this.requesting.set(true);
    this.requestError.set(null);
    this.joinRequestApi.create(chatId).subscribe({
      next: () => {
        this.requesting.set(false);
        this.chat.update((current) =>
          current ? { ...current, hasPendingJoinRequest: true } : current,
        );
      },
      error: (error: unknown) => {
        this.requesting.set(false);
        this.requestError.set(problemDetail(error, 'Не удалось подать заявку'));
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
    this.requestError.set(null);

    void this.chatHub.ensureConnected();
    const subscription = this.chatApi.getInfo(id).subscribe({
      next: (body) => {
        this.chat.set(body.response);
        this.notFound.set(!body.response);
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.loading.set(false);
        if (isAccessLost(error)) {
          if (isReload) {
            // Kick / снятие ChatRead: info → 403; уход с страницы без stub.
            void this.router.navigateByUrl('/');
          } else {
            this.notFound.set(true);
          }
          return;
        }
        this.loadError.set(problemDetail(error, 'Не удалось загрузить чат'));
      },
    });
    return () => subscription.unsubscribe();
  }
}

/** `400` — не Guid; `403` — нет ChatRead (kick); `404` — чат не существует. */
function isAccessLost(error: unknown): boolean {
  return (
    error instanceof HttpErrorResponse &&
    (error.status === 400 || error.status === 403 || error.status === 404)
  );
}
