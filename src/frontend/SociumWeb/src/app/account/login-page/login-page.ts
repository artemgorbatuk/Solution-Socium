import { Component, computed, inject, input, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, switchMap } from 'rxjs';
import { ThemeToggle } from '../../layout/theme-toggle/theme-toggle';
import { problemDetail } from '../../shared/api/api-response';
import { UserApi } from '../../users/user-api';
import { userLoginMaxLength, userNameMaxLength } from '../../users/user.models';
import { AccountProfile, passwordMaxLength } from '../account.models';
import { Auth } from '../auth';
import { safeReturnUrl } from '../login-redirect';

type Mode = 'login' | 'register';

/** Вход и регистрация на одной карточке; после регистрации пользователь сразу входит. */
@Component({
  selector: 'app-login-page',
  imports: [ThemeToggle],
  templateUrl: './login-page.html',
  styleUrl: './login-page.css',
})
export class LoginPage {
  private readonly auth = inject(Auth);
  private readonly userApi = inject(UserApi);
  private readonly router = inject(Router);

  /** Адрес, с которого отправили на вход (query-параметр). */
  readonly returnUrl = input<string>();

  protected readonly loginMaxLength = userLoginMaxLength;
  protected readonly nameMaxLength = userNameMaxLength;
  protected readonly passwordMaxLength = passwordMaxLength;

  protected readonly mode = signal<Mode>('login');
  protected readonly login = signal('');
  protected readonly name = signal('');
  protected readonly password = signal('');
  protected readonly rememberMe = signal(false);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly registering = computed(() => this.mode() === 'register');
  protected readonly canSubmit = computed(
    () => !this.saving() && !!this.login().trim() && !!this.password() && (!this.registering() || !!this.name().trim()),
  );

  protected switchMode(mode: Mode): void {
    this.mode.set(mode);
    this.error.set(null);
  }

  protected onInput(field: 'login' | 'name' | 'password', event: Event): void {
    this[field].set((event.target as HTMLInputElement).value);
  }

  protected onRememberMeChange(event: Event): void {
    this.rememberMe.set((event.target as HTMLInputElement).checked);
  }

  protected submit(event: Event): void {
    event.preventDefault();
    if (!this.canSubmit()) {
      return;
    }

    const login = this.login().trim();
    const password = this.password();
    const rememberMe = this.rememberMe();
    const signIn = () => this.auth.login({ login, password, rememberMe });
    const request: Observable<AccountProfile> = this.registering()
      ? this.userApi.create({ login, name: this.name().trim(), password }).pipe(switchMap(signIn))
      : signIn();

    this.saving.set(true);
    this.error.set(null);
    request.subscribe({
      next: () => void this.router.navigateByUrl(safeReturnUrl(this.returnUrl())),
      error: (error: unknown) => {
        this.saving.set(false);
        this.error.set(problemDetail(error, this.registering() ? 'Не удалось зарегистрироваться' : 'Не удалось войти'));
      },
    });
  }
}
