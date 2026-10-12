import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { Subscription, debounceTime, merge } from 'rxjs';
import { ChatHub } from '../../realtime/chat-hub';
import { problemDetail } from '../../shared/api/api-response';
import { CurrentUser } from '../current-user';
import { UserApi } from '../user-api';
import { UserAdminListModel } from '../user.models';
import { showUsersTopbarTitle } from '../users-topbar-title';

/** Частые события (массовые изменения ролей) объединяются в одно перечитывание. */
export const userAccessReloadDebounceMs = 300;

/** Список всех пользователей для админа приложения (`/users`). */
@Component({
  selector: 'app-users-page',
  imports: [RouterLink],
  templateUrl: './users-page.html',
  styleUrl: './users-page.css',
})
export class UsersPage {
  private readonly userApi = inject(UserApi);
  private readonly chatHub = inject(ChatHub);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly currentUserId = inject(CurrentUser).id;

  protected readonly rows = signal<UserAdminListModel[]>([]);
  protected readonly loading = signal(true);
  protected readonly loadError = signal<string | null>(null);

  protected readonly search = signal('');
  protected readonly showDeleted = signal(false);

  protected readonly filteredRows = computed(() => {
    const query = this.search().trim().toLocaleLowerCase();
    const showDeleted = this.showDeleted();
    return this.rows().filter(
      (row) =>
        (showDeleted || !row.isDeleted) &&
        (!query || row.name.toLocaleLowerCase().includes(query) || row.login.toLocaleLowerCase().includes(query)),
    );
  });

  private loadSubscription: Subscription | null = null;

  constructor() {
    showUsersTopbarTitle();
    this.load();
    void this.chatHub.ensureConnected();
    merge(this.chatHub.userAccessChanged$, this.chatHub.userChanged$)
      .pipe(debounceTime(userAccessReloadDebounceMs), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.load());
    this.destroyRef.onDestroy(() => this.loadSubscription?.unsubscribe());
  }

  protected onSearchInput(event: Event): void {
    this.search.set((event.target as HTMLInputElement).value);
  }

  protected onShowDeletedChange(event: Event): void {
    this.showDeleted.set((event.target as HTMLInputElement).checked);
  }

  private load(): void {
    this.loadSubscription?.unsubscribe();
    this.loading.set(true);
    this.loadError.set(null);
    this.loadSubscription = this.userApi.getAdminList({ includeDeleted: true }).subscribe({
      next: (body) => {
        this.rows.set(body.response?.rows ?? []);
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.loadError.set(problemDetail(error, 'Не удалось загрузить пользователей'));
        this.loading.set(false);
      },
    });
  }
}
