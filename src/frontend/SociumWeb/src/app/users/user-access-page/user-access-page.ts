import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { Observable, Subscription, debounceTime, forkJoin, merge } from 'rxjs';
import { ChatHub } from '../../realtime/chat-hub';
import { problemDetail } from '../../shared/api/api-response';
import { AppRoleApi } from '../app-role-api';
import { CurrentUser } from '../current-user';
import { RoleAssignmentApi } from '../role-assignment-api';
import { UserApi } from '../user-api';
import {
  RoleAssignmentChatModel,
  RoleAssignmentListPageResponse,
  RoleAssignmentRoomModel,
  roleNames,
} from '../user-access.models';
import { UserAdminListModel } from '../user.models';
import { userAccessReloadDebounceMs } from '../users-page/users-page';
import { showUsersTopbarTitle } from '../users-topbar-title';

/** Подсказки `title` у неактивных переключателей. */
export const userAccessReasons = {
  self: 'Свои доступы меняют другие админы приложения',
  deleted: 'Пользователь удалён',
  noRights: 'Недостаточно прав',
  lastAppAdmin: 'Последний админ приложения — сначала назначьте другого',
  lastRoomAdmin: 'Последний админ комнаты — сначала назначьте другого',
  lastChatAdmin: 'Последний админ чата — сначала назначьте другого',
  notParticipant: 'Сначала добавьте участником чата',
} as const;

type ToggleKey = `app` | `room:${string}` | `chat:${string}` | `chat-admin:${string}`;

/** Доступы пользователя (`/users/:id`): роль приложения и дерево «комнаты → чаты» с переключателями ролей. */
@Component({
  selector: 'app-user-access-page',
  imports: [RouterLink],
  templateUrl: './user-access-page.html',
  styleUrl: './user-access-page.css',
})
export class UserAccessPage {
  private readonly userApi = inject(UserApi);
  private readonly appRoleApi = inject(AppRoleApi);
  private readonly roleAssignmentApi = inject(RoleAssignmentApi);
  private readonly chatHub = inject(ChatHub);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly currentUserId = inject(CurrentUser).id;

  /** Из маршрута `users/:id`. */
  readonly id = input.required<string>();

  protected readonly user = signal<UserAdminListModel | null>(null);
  protected readonly tree = signal<RoleAssignmentListPageResponse | null>(null);
  protected readonly loading = signal(true);
  protected readonly notFound = signal(false);
  protected readonly loadError = signal<string | null>(null);

  protected readonly onlyWithAccess = signal(true);
  protected readonly search = signal('');
  private readonly collapsedRoomIds = signal<ReadonlySet<string>>(new Set());

  protected readonly pendingKey = signal<ToggleKey | null>(null);
  protected readonly rowError = signal<{ key: ToggleKey; text: string } | null>(null);
  protected readonly kickConfirmChatId = signal<string | null>(null);

  protected readonly isSelf = computed(() => this.user()?.id === this.currentUserId());

  protected readonly visibleRooms = computed(() => {
    const query = this.search().trim().toLocaleLowerCase();
    const onlyWithAccess = this.onlyWithAccess();
    const rooms: RoleAssignmentRoomModel[] = [];
    for (const room of this.tree()?.rooms ?? []) {
      if (onlyWithAccess && !room.isRoomAdmin && !room.chats.some(hasChatAccess)) {
        continue;
      }
      const roomMatches = !query || room.roomName.toLocaleLowerCase().includes(query);
      const chats = room.chats.filter(
        (chat) =>
          (!onlyWithAccess || hasChatAccess(chat)) && (roomMatches || chat.chatName.toLocaleLowerCase().includes(query)),
      );
      if (!roomMatches && !chats.length) {
        continue;
      }
      rooms.push({ ...room, chats });
    }
    return rooms;
  });

  /** Причина, почему переключатель «Админ приложения» неактивен; `null` — активен. */
  protected readonly appAdminReason = computed(() => {
    const user = this.user();
    if (!user) {
      return userAccessReasons.noRights;
    }
    if (user.isDeleted) {
      return userAccessReasons.deleted;
    }
    if (user.isAppAdmin) {
      return user.canRevokeAppAdmin ? null : userAccessReasons.lastAppAdmin;
    }
    if (user.canGrantAppAdmin) {
      return null;
    }
    return this.isSelf() ? userAccessReasons.self : userAccessReasons.noRights;
  });

  /** Общая причина для ролей комнат и чатов: своя карточка, удалённый, нет прав. */
  private readonly manageReason = computed(() => {
    if (this.tree()?.canManage) {
      return null;
    }
    if (this.isSelf()) {
      return userAccessReasons.self;
    }
    return this.user()?.isDeleted ? userAccessReasons.deleted : userAccessReasons.noRights;
  });

  private loadSubscription: Subscription | null = null;

  constructor() {
    showUsersTopbarTitle();
    effect(() => {
      const id = this.id();
      untracked(() => {
        this.user.set(null);
        this.tree.set(null);
        this.rowError.set(null);
        this.kickConfirmChatId.set(null);
        this.load(id);
      });
    });
    void this.chatHub.ensureConnected();
    merge(
      this.chatHub.userAccessChanged$,
      this.chatHub.userChanged$,
      this.chatHub.roomChanged$,
      this.chatHub.chatChanged$,
    )
      .pipe(debounceTime(userAccessReloadDebounceMs), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.load(this.id()));
    this.destroyRef.onDestroy(() => this.loadSubscription?.unsubscribe());
  }

  protected roomAdminReason(room: RoleAssignmentRoomModel): string | null {
    return this.manageReason() ?? (room.isRoomAdmin && room.isLastRoomAdmin ? userAccessReasons.lastRoomAdmin : null);
  }

  protected participantReason(chat: RoleAssignmentChatModel): string | null {
    return this.manageReason() ?? (chat.isParticipant && chat.isLastChatAdmin ? userAccessReasons.lastChatAdmin : null);
  }

  protected chatAdminReason(chat: RoleAssignmentChatModel): string | null {
    const reason = this.manageReason();
    if (reason) {
      return reason;
    }
    if (!chat.isParticipant) {
      return userAccessReasons.notParticipant;
    }
    return chat.isChatAdmin && chat.isLastChatAdmin ? userAccessReasons.lastChatAdmin : null;
  }

  protected isCollapsed(roomId: string): boolean {
    return this.collapsedRoomIds().has(roomId);
  }

  protected toggleCollapsed(roomId: string): void {
    this.collapsedRoomIds.update((ids) => {
      const next = new Set(ids);
      if (!next.delete(roomId)) {
        next.add(roomId);
      }
      return next;
    });
  }

  protected onSearchInput(event: Event): void {
    this.search.set((event.target as HTMLInputElement).value);
  }

  protected onOnlyWithAccessChange(event: Event): void {
    this.onlyWithAccess.set((event.target as HTMLInputElement).checked);
  }

  protected errorFor(key: string): string | null {
    const error = this.rowError();
    return error?.key === key ? error.text : null;
  }

  protected toggleAppAdmin(event: Event): void {
    const user = this.user();
    keepChecked(event, user?.isAppAdmin ?? false);
    if (!user || this.appAdminReason()) {
      return;
    }
    this.submit(
      'app',
      user.isAppAdmin ? this.appRoleApi.revokeAppAdmin(user.id) : this.appRoleApi.grantAppAdmin(user.id),
    );
  }

  protected toggleRoomAdmin(event: Event, room: RoleAssignmentRoomModel): void {
    keepChecked(event, room.isRoomAdmin);
    if (this.roomAdminReason(room)) {
      return;
    }
    const request = { userId: this.id(), roleName: roleNames.roomAdmin, roomId: room.roomId };
    this.submit(
      `room:${room.roomId}`,
      room.isRoomAdmin ? this.roleAssignmentApi.delete(request) : this.roleAssignmentApi.create(request),
    );
  }

  protected toggleParticipant(event: Event, chat: RoleAssignmentChatModel): void {
    keepChecked(event, chat.isParticipant);
    if (this.participantReason(chat)) {
      return;
    }
    if (chat.isParticipant && chat.isChatAdmin) {
      this.rowError.set(null);
      this.kickConfirmChatId.set(chat.chatId);
      return;
    }
    const request = { userId: this.id(), roleName: roleNames.chatParticipant, chatId: chat.chatId };
    this.submit(
      `chat:${chat.chatId}`,
      chat.isParticipant ? this.roleAssignmentApi.delete(request) : this.roleAssignmentApi.create(request),
    );
  }

  protected confirmKick(chat: RoleAssignmentChatModel): void {
    this.kickConfirmChatId.set(null);
    this.submit(
      `chat:${chat.chatId}`,
      this.roleAssignmentApi.delete({ userId: this.id(), roleName: roleNames.chatParticipant, chatId: chat.chatId }),
    );
  }

  protected cancelKick(): void {
    this.kickConfirmChatId.set(null);
  }

  protected toggleChatAdmin(event: Event, chat: RoleAssignmentChatModel): void {
    keepChecked(event, chat.isChatAdmin);
    if (this.chatAdminReason(chat)) {
      return;
    }
    const request = { userId: this.id(), roleName: roleNames.chatAdmin, chatId: chat.chatId };
    this.submit(
      `chat-admin:${chat.chatId}`,
      chat.isChatAdmin ? this.roleAssignmentApi.delete(request) : this.roleAssignmentApi.create(request),
    );
  }

  /** Один запрос на переключатель; после успеха дерево перечитывается — меняются признаки «последний админ» у соседей. */
  private submit(key: ToggleKey, request: Observable<unknown>): void {
    if (this.pendingKey()) {
      return;
    }
    this.pendingKey.set(key);
    this.rowError.set(null);
    request.subscribe({
      next: () => {
        this.pendingKey.set(null);
        this.load(this.id());
      },
      error: (error: unknown) => {
        this.pendingKey.set(null);
        this.rowError.set({ key, text: problemDetail(error, 'Не удалось изменить доступ') });
      },
    });
  }

  private load(id: string): void {
    this.loadSubscription?.unsubscribe();
    this.loading.set(true);
    this.notFound.set(false);
    this.loadError.set(null);
    this.loadSubscription = forkJoin({
      users: this.userApi.getAdminList({ includeDeleted: true, ids: [id] }),
      tree: this.roleAssignmentApi.getList(id),
    }).subscribe({
      next: ({ users, tree }) => {
        const user = users.response?.rows.find((row) => row.id === id) ?? null;
        this.user.set(user);
        this.tree.set(tree.response);
        this.notFound.set(!user);
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.loading.set(false);
        if (error instanceof HttpErrorResponse && error.status === 403) {
          // Сняли свой AppAdmin: страница больше недоступна.
          void this.router.navigateByUrl('/');
          return;
        }
        if (error instanceof HttpErrorResponse && (error.status === 400 || error.status === 404)) {
          this.notFound.set(true);
          return;
        }
        this.loadError.set(problemDetail(error, 'Не удалось загрузить доступы пользователя'));
      },
    });
  }
}

function hasChatAccess(chat: RoleAssignmentChatModel): boolean {
  return chat.isParticipant || chat.isChatAdmin;
}

/** Чекбокс показывает текущую роль: положение меняется только после ответа сервера и перечитывания. */
function keepChecked(event: Event, checked: boolean): void {
  (event.target as HTMLInputElement).checked = checked;
}
