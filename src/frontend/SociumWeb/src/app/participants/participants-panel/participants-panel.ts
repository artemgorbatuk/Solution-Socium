import { Component, DestroyRef, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { Subscription } from 'rxjs';
import { ChatChanges } from '../../chats/chat-changes';
import { ChatHub } from '../../realtime/chat-hub';
import { problemDetail } from '../../shared/api/api-response';
import { CurrentUser } from '../../users/current-user';
import { UserApi } from '../../users/user-api';
import { UserSearchModel } from '../../users/user.models';
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
  private readonly userApi = inject(UserApi);
  private readonly chatChanges = inject(ChatChanges);
  private readonly chatHub = inject(ChatHub);
  private readonly panelState = inject(ParticipantsPanelState);

  readonly chatId = input.required<string>();
  readonly chatName = input.required<string>();

  private readonly currentUser = inject(CurrentUser);
  protected readonly currentUserId = this.currentUser.id;
  protected readonly rows = signal<ParticipantListModel[]>([]);
  /** ChatAdminGrant — кнопки смены админа. */
  protected readonly canChangeAdmin = signal(false);
  /** ChatParticipantGrant — поиск и Добавить. */
  protected readonly canGrantParticipant = signal(false);
  /** ChatParticipantRevoke — Выгнать. */
  protected readonly canKick = signal(false);
  protected readonly loading = signal(true);
  protected readonly loadError = signal<string | null>(null);

  protected readonly updatingId = signal<string | null>(null);
  protected readonly roleError = signal<string | null>(null);
  protected readonly canToggleAdmin = computed(() => this.canChangeAdmin() && this.rows().length > 1);

  protected readonly searchLogin = signal('');
  protected readonly searchResults = signal<UserSearchModel[]>([]);
  protected readonly searching = signal(false);
  protected readonly searchError = signal<string | null>(null);
  protected readonly addingUserId = signal<string | null>(null);

  protected readonly kickConfirmId = signal<string | null>(null);
  protected readonly kickingId = signal<string | null>(null);
  protected readonly kickError = signal<string | null>(null);

  protected readonly leaveStep = signal<LeaveStep>('idle');
  protected readonly leaveReason = signal<string | null>(null);
  protected readonly leaveError = signal<string | null>(null);

  private hubSubscriptions: Subscription[] = [];
  private boundChatId?: string;
  private searchTimer: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    effect(() => {
      this.chatId();
      untracked(() => {
        this.resetLeave();
        this.resetKick();
      });
    });
    effect((onCleanup) => {
      const chatId = this.chatId();
      this.chatChanges.version();
      this.currentUserId();
      untracked(() => {
        this.bindHub(chatId);
        onCleanup(this.load(chatId));
      });
    });
    inject(DestroyRef).onDestroy(() => {
      this.unbindHub();
      if (this.searchTimer) {
        clearTimeout(this.searchTimer);
      }
    });
  }

  protected close(): void {
    this.panelState.close();
  }

  protected onSearchInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchLogin.set(value);
    this.searchError.set(null);
    if (this.searchTimer) {
      clearTimeout(this.searchTimer);
    }
    const trimmed = value.trim();
    if (!trimmed) {
      this.searchResults.set([]);
      return;
    }
    this.searchTimer = setTimeout(() => this.runSearch(trimmed), 300);
  }

  protected addUser(user: UserSearchModel): void {
    if (this.addingUserId() || !this.canGrantParticipant()) {
      return;
    }
    this.addingUserId.set(user.id);
    this.searchError.set(null);
    this.participantApi.create(this.chatId(), user.id).subscribe({
      next: () => {
        this.addingUserId.set(null);
        this.searchLogin.set('');
        this.searchResults.set([]);
        this.chatChanges.notify();
      },
      error: (error: unknown) => {
        this.addingUserId.set(null);
        this.searchError.set(problemDetail(error, 'Не удалось добавить участника'));
      },
    });
  }

  protected setAdmin(participant: ParticipantListModel, isAdmin: boolean): void {
    if (this.updatingId() || !this.canToggleAdmin()) {
      return;
    }

    this.updatingId.set(participant.id);
    this.roleError.set(null);
    this.kickConfirmId.set(null);
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

  protected requestKick(participant: ParticipantListModel): void {
    this.kickError.set(null);
    this.kickConfirmId.set(participant.id);
  }

  protected resetKick(): void {
    this.kickConfirmId.set(null);
    this.kickError.set(null);
  }

  protected confirmKick(participant: ParticipantListModel): void {
    if (this.kickingId() || !this.canKick() || participant.userId === this.currentUserId()) {
      return;
    }

    this.kickingId.set(participant.id);
    this.kickError.set(null);
    this.participantApi.kick(participant.id).subscribe({
      next: () => {
        this.kickingId.set(null);
        this.kickConfirmId.set(null);
        this.rows.update((list) => list.filter((row) => row.id !== participant.id));
        this.resetLeave();
        this.chatChanges.notify();
      },
      error: (error: unknown) => {
        this.kickingId.set(null);
        this.kickError.set(problemDetail(error, 'Не удалось выгнать участника'));
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

  private runSearch(login: string): void {
    this.searching.set(true);
    this.userApi.search(login).subscribe({
      next: (body) => {
        this.searching.set(false);
        const participantIds = new Set(this.rows().map((row) => row.userId));
        this.searchResults.set((body.response?.rows ?? []).filter((row) => !participantIds.has(row.id)));
      },
      error: (error: unknown) => {
        this.searching.set(false);
        this.searchError.set(problemDetail(error, 'Не удалось найти пользователей'));
      },
    });
  }

  /** Загружает участников; возвращает отмену запроса. */
  private load(chatId: string): () => void {
    this.loading.set(true);
    this.loadError.set(null);
    const subscription = this.participantApi.getList(chatId).subscribe({
      next: (body) => {
        this.rows.set(body.response?.rows ?? []);
        this.setCapabilityFlags(
          body.response?.canChangeAdmin ?? false,
          body.response?.canGrantParticipant ?? false,
          body.response?.canKick ?? false,
        );
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
          this.setCapabilityFlags(false, false, false);
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
          // ChatAdmin даёт Grant/Revoke participant и admin; снятие админа — все false.
          this.setCapabilityFlags(payload.isAdmin, payload.isAdmin, payload.isAdmin);
        }
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
          this.chatChanges.notify();
        }
      }),
    ];
  }

  private setCapabilityFlags(canChangeAdmin: boolean, canGrantParticipant: boolean, canKick: boolean): void {
    this.canChangeAdmin.set(canChangeAdmin);
    this.canGrantParticipant.set(canGrantParticipant);
    this.canKick.set(canKick);
  }

  private unbindHub(): void {
    for (const subscription of this.hubSubscriptions) {
      subscription.unsubscribe();
    }
    this.hubSubscriptions = [];
    this.boundChatId = undefined;
  }
}
