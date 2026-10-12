import { Component, DestroyRef, effect, inject, input, signal, untracked } from '@angular/core';
import { Subscription } from 'rxjs';
import { ChatChanges } from '../../chats/chat-changes';
import { ChatHub } from '../../realtime/chat-hub';
import { problemDetail } from '../../shared/api/api-response';
import { JoinRequestApi } from '../join-request-api';
import { JoinRequestPendingListModel } from '../join-request.models';
import { ParticipantsPanelState } from '../participants-panel-state';

@Component({
  selector: 'app-join-requests-panel',
  templateUrl: './join-requests-panel.html',
  styleUrl: './join-requests-panel.css',
})
export class JoinRequestsPanel {
  private readonly joinRequestApi = inject(JoinRequestApi);
  private readonly chatChanges = inject(ChatChanges);
  private readonly chatHub = inject(ChatHub);
  private readonly panelState = inject(ParticipantsPanelState);

  readonly chatId = input.required<string>();

  protected readonly rows = signal<JoinRequestPendingListModel[]>([]);
  protected readonly loading = signal(true);
  protected readonly loadError = signal<string | null>(null);
  protected readonly respondingId = signal<string | null>(null);
  protected readonly actionError = signal<string | null>(null);

  private hubSubscriptions: Subscription[] = [];
  private boundChatId?: string;

  constructor() {
    effect((onCleanup) => {
      const chatId = this.chatId();
      this.chatChanges.version();
      untracked(() => {
        this.bindHub(chatId);
        onCleanup(this.load(chatId));
      });
    });
    inject(DestroyRef).onDestroy(() => this.unbindHub());
  }

  protected close(): void {
    this.panelState.closeRequests();
  }

  protected accept(request: JoinRequestPendingListModel): void {
    if (this.respondingId()) {
      return;
    }
    this.respondingId.set(request.id);
    this.actionError.set(null);
    this.joinRequestApi.accept(request.id).subscribe({
      next: () => {
        this.respondingId.set(null);
        this.removeRow(request.id);
        this.chatChanges.notify();
      },
      error: (error: unknown) => {
        this.respondingId.set(null);
        this.actionError.set(problemDetail(error, 'Не удалось принять заявку'));
      },
    });
  }

  protected reject(request: JoinRequestPendingListModel): void {
    if (this.respondingId()) {
      return;
    }
    this.respondingId.set(request.id);
    this.actionError.set(null);
    this.joinRequestApi.reject(request.id).subscribe({
      next: () => {
        this.respondingId.set(null);
        this.removeRow(request.id);
      },
      error: (error: unknown) => {
        this.respondingId.set(null);
        this.actionError.set(problemDetail(error, 'Не удалось отклонить заявку'));
      },
    });
  }

  private removeRow(id: string): void {
    this.rows.update((list) => list.filter((row) => row.id !== id));
    this.panelState.pendingJoinRequestCount.update((count) => Math.max(0, count - 1));
  }

  private load(chatId: string): () => void {
    this.loading.set(true);
    this.loadError.set(null);
    const subscription = this.joinRequestApi.getPendingList(chatId).subscribe({
      next: (body) => {
        const rows = body.response?.rows ?? [];
        this.rows.set(rows);
        this.panelState.pendingJoinRequestCount.set(body.response?.rowCount ?? rows.length);
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.loadError.set(problemDetail(error, 'Не удалось загрузить заявки'));
        this.loading.set(false);
      },
    });
    return () => subscription.unsubscribe();
  }

  private bindHub(chatId: string): void {
    if (this.boundChatId === chatId) {
      return;
    }

    this.unbindHub();
    this.boundChatId = chatId;
    this.hubSubscriptions = [
      this.chatHub.joinRequestChanged$.subscribe((event) => {
        if (event.chatId !== chatId) {
          return;
        }
        this.panelState.pendingJoinRequestCount.set(event.pendingCount);
        this.load(chatId);
      }),
      this.chatHub.userChanged$.subscribe((change) => {
        const name = change.name?.trim();
        if (!name) {
          return;
        }
        this.rows.update((list) =>
          list.map((row) => (row.userId === change.userId ? { ...row, name } : row)),
        );
      }),
      this.chatHub.reconnected$.subscribe(() => {
        if (this.chatId() === chatId) {
          this.load(chatId);
        }
      }),
    ];
  }

  private unbindHub(): void {
    for (const subscription of this.hubSubscriptions) {
      subscription.unsubscribe();
    }
    this.hubSubscriptions = [];
    this.boundChatId = undefined;
  }
}
