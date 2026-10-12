import { Component, DestroyRef, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { Subscription } from 'rxjs';
import { ChatChanges } from '../../chats/chat-changes';
import { ChatHub } from '../../realtime/chat-hub';
import { problemDetail } from '../../shared/api/api-response';
import { CurrentUser } from '../../users/current-user';
import { ParticipantApi } from '../participant-api';
import { ParticipantListModel } from '../participant.models';
import { ParticipantsPanelState } from '../participants-panel-state';

/** Шаг выхода из чата: проверка на сервере, подтверждение на месте или причина, почему уйти нельзя. */
type LeaveStep = 'idle' | 'checking' | 'confirm' | 'blocked' | 'leaving';

@Component({
  selector: 'app-participants-panel',
  templateUrl: './participants-panel.html',
  styleUrl: './participants-panel.css',
})
export class ParticipantsPanel {
  private readonly participantApi = inject(ParticipantApi);
  private readonly chatChanges = inject(ChatChanges);
  private readonly chatHub = inject(ChatHub);
  private readonly panelState = inject(ParticipantsPanelState);

  readonly chatId = input.required<string>();
  readonly chatName = input.required<string>();

  private readonly currentUser = inject(CurrentUser);
  protected readonly currentUserId = this.currentUser.id;
  protected readonly rows = signal<ParticipantListModel[]>([]);
  /** С сервера: можно менять админ-роль (ChatAdminGrant/Revoke). */
  protected readonly canManageRoles = signal(false);
  protected readonly loading = signal(true);
  protected readonly loadError = signal<string | null>(null);

  protected readonly updatingId = signal<string | null>(null);
  protected readonly roleError = signal<string | null>(null);
  /** API разрешил + в чате больше одного участника (нельзя снять последнего админа). */
  protected readonly canChangeRole = computed(() => this.canManageRoles() && this.rows().length > 1);

  protected readonly leaveStep = signal<LeaveStep>('idle');
  protected readonly leaveReason = signal<string | null>(null);
  protected readonly leaveError = signal<string | null>(null);

  private hubSubscriptions: Subscription[] = [];
  private boundChatId?: string;

  constructor() {
    effect(() => {
      this.chatId();
      untracked(() => this.resetLeave());
    });
    effect((onCleanup) => {
      const chatId = this.chatId();
      this.chatChanges.version();
      this.currentUserId();
      this.currentUser.name();
      untracked(() => {
        this.bindHub(chatId);
        onCleanup(this.load(chatId));
      });
    });
    inject(DestroyRef).onDestroy(() => this.unbindHub());
  }

  protected close(): void {
    this.panelState.close();
  }

  protected setAdmin(participant: ParticipantListModel, isAdmin: boolean): void {
    if (this.updatingId() || !this.canChangeRole()) {
      return;
    }

    this.updatingId.set(participant.id);
    this.roleError.set(null);
    this.participantApi.update({ id: participant.id, isAdmin }).subscribe({
      next: () => {
        this.updatingId.set(null);
        this.resetLeave();
        this.chatChanges.notify();
      },
      error: (error: unknown) => {
        this.updatingId.set(null);
        this.roleError.set(problemDetail(error, 'Не удалось изменить роль'));
      },
    });
  }

  protected requestLeave(): void {
    this.leaveStep.set('checking');
    this.leaveError.set(null);
    this.participantApi.getLeavePage(this.chatId()).subscribe({
      next: (body) => {
        const page = body.response;
        this.leaveReason.set(page?.reason ?? null);
        this.leaveStep.set(page?.canLeave ? 'confirm' : 'blocked');
      },
      error: (error: unknown) => {
        this.leaveStep.set('idle');
        this.leaveError.set(problemDetail(error, 'Не удалось проверить, можно ли покинуть чат'));
      },
    });
  }

  protected confirmLeave(): void {
    this.leaveStep.set('leaving');
    this.leaveError.set(null);
    this.participantApi.leave(this.chatId()).subscribe({
      next: () => {
        this.resetLeave();
        this.chatChanges.notify();
      },
      error: (error: unknown) => {
        this.leaveStep.set('confirm');
        this.leaveError.set(problemDetail(error, 'Не удалось покинуть чат'));
      },
    });
  }

  protected resetLeave(): void {
    this.leaveStep.set('idle');
    this.leaveReason.set(null);
    this.leaveError.set(null);
  }

  /** Загружает участников; возвращает отмену запроса. */
  private load(chatId: string): () => void {
    this.loading.set(true);
    this.loadError.set(null);
    const subscription = this.participantApi.getList(chatId).subscribe({
      next: (body) => {
        this.rows.set(body.response?.rows ?? []);
        this.canManageRoles.set(body.response?.canChangeRole ?? false);
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.loadError.set(problemDetail(error, 'Не удалось загрузить участников'));
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
      this.chatHub.participantJoined$.subscribe((payload) => {
        if (payload.chatId !== chatId) {
          return;
        }
        this.rows.update((list) => {
          if (list.some((row) => row.id === payload.id)) {
            return list;
          }
          return [
            ...list,
            {
              id: payload.id,
              userId: payload.userId,
              login: payload.login,
              name: payload.name,
              isAdmin: payload.isAdmin,
            },
          ];
        });
      }),
      this.chatHub.participantLeft$.subscribe((payload) => {
        if (payload.chatId !== chatId) {
          return;
        }
        this.rows.update((list) => list.filter((row) => row.id !== payload.id));
        if (payload.userId === this.currentUserId()) {
          this.canManageRoles.set(false);
        }
      }),
      this.chatHub.participantRoleChanged$.subscribe((payload) => {
        if (payload.chatId !== chatId) {
          return;
        }
        this.rows.update((list) =>
          list.map((row) => (row.id === payload.id ? { ...row, isAdmin: payload.isAdmin } : row)),
        );
        if (payload.userId === this.currentUserId()) {
          this.canManageRoles.set(payload.isAdmin);
        }
      }),
      this.chatHub.reconnected$.subscribe(() => {
        if (this.chatId() === chatId) {
          this.chatChanges.notify();
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
