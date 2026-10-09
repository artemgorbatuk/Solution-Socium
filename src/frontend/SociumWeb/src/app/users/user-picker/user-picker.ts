import { Component, ElementRef, Injector, afterNextRender, computed, inject, signal, viewChild } from '@angular/core';
import { problemDetail } from '../../shared/api/api-response';
import { CurrentUser } from '../current-user';
import { UserApi } from '../user-api';
import { UserListModel, userLoginMaxLength, userNameMaxLength } from '../user.models';

@Component({
  selector: 'app-user-picker',
  templateUrl: './user-picker.html',
  styleUrl: './user-picker.css',
  host: {
    '(keydown.escape)': 'onEscape()',
    '(document:click)': 'onDocumentClick($event)',
  },
})
export class UserPicker {
  private readonly userApi = inject(UserApi);
  private readonly currentUser = inject(CurrentUser);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly injector = inject(Injector);

  protected readonly loginMaxLength = userLoginMaxLength;
  protected readonly nameMaxLength = userNameMaxLength;

  protected readonly users = signal<UserListModel[]>([]);
  protected readonly loading = signal(false);
  protected readonly loadError = signal<string | null>(null);
  protected readonly open = signal(false);
  protected readonly current = computed(() => this.users().find((user) => user.id === this.currentUser.id()) ?? null);
  /** Пока список грузится, сохранённый выбор ещё не проверен — подпись не мигает «Выберите пользователя». */
  protected readonly label = computed(() => {
    if (this.current()) {
      return this.current()!.name;
    }
    return this.loading() && this.currentUser.id() ? 'Загрузка…' : 'Выберите пользователя';
  });

  protected readonly adding = signal(false);
  protected readonly draftLogin = signal('');
  protected readonly draftName = signal('');
  protected readonly saving = signal(false);
  protected readonly addError = signal<string | null>(null);
  protected readonly canAdd = computed(() => !this.saving() && !!this.draftLogin().trim() && !!this.draftName().trim());

  private readonly loginInput = viewChild<ElementRef<HTMLInputElement>>('loginInput');

  constructor() {
    this.loadUsers();
  }

  protected initial(user: UserListModel | null): string {
    return user ? user.name.charAt(0).toLocaleUpperCase() : '?';
  }

  protected toggle(): void {
    this.open.update((value) => !value);
    this.cancelAdd();
  }

  protected select(user: UserListModel): void {
    this.currentUser.select(user.id);
    this.open.set(false);
  }

  protected startAdd(): void {
    this.draftLogin.set('');
    this.draftName.set('');
    this.addError.set(null);
    this.adding.set(true);
    afterNextRender(() => this.loginInput()?.nativeElement.focus(), { injector: this.injector });
  }

  protected cancelAdd(): void {
    if (this.saving()) {
      return;
    }
    this.adding.set(false);
    this.addError.set(null);
  }

  protected onLoginInput(event: Event): void {
    this.draftLogin.set((event.target as HTMLInputElement).value);
  }

  protected onNameInput(event: Event): void {
    this.draftName.set((event.target as HTMLInputElement).value);
  }

  protected add(event: Event): void {
    event.preventDefault();
    if (!this.canAdd()) {
      return;
    }

    const login = this.draftLogin().trim();
    const name = this.draftName().trim();
    this.saving.set(true);
    this.addError.set(null);
    this.userApi.create({ login, name }).subscribe({
      next: () => {
        this.saving.set(false);
        this.adding.set(false);
        this.open.set(false);
        this.loadUsers(login.toLowerCase());
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.addError.set(problemDetail(error, 'Не удалось добавить пользователя'));
      },
    });
  }

  protected onEscape(): void {
    if (this.adding()) {
      this.cancelAdd();
    } else {
      this.open.set(false);
    }
  }

  /** `composedPath` собран до обработчиков, поэтому клик по кнопке, которую шаблон уже убрал, не закрывает панель. */
  protected onDocumentClick(event: MouseEvent): void {
    if (this.open() && !event.composedPath().includes(this.host.nativeElement)) {
      this.open.set(false);
      this.cancelAdd();
    }
  }

  /** `selectLogin` — только что добавленный пользователь, он сразу становится текущим. */
  private loadUsers(selectLogin?: string): void {
    this.loading.set(true);
    this.loadError.set(null);
    this.userApi.getList().subscribe({
      next: (body) => {
        const users = body.response?.rows ?? [];
        this.users.set(users);
        this.loading.set(false);
        const added = selectLogin ? users.find((user) => user.login === selectLogin) : undefined;
        if (added) {
          this.currentUser.select(added.id);
        } else if (this.currentUser.id() && !this.current()) {
          this.currentUser.clear();
        }
      },
      error: (error: unknown) => {
        this.loadError.set(problemDetail(error, 'Не удалось загрузить пользователей'));
        this.loading.set(false);
      },
    });
  }
}
