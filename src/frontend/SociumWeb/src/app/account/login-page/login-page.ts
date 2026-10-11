import { Component, computed, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { ThemeToggle } from '../../layout/theme-toggle/theme-toggle';
import { problemDetail } from '../../shared/api/api-response';
import { UserApi } from '../../users/user-api';
import { userEmailMaxLength, userLoginMaxLength, userNameMaxLength } from '../../users/user.models';
import { passwordMaxLength } from '../account.models';
import { Auth } from '../auth';
import { safeReturnUrl } from '../login-redirect';

type Mode = 'login' | 'register';

/** Вход и регистрация на одной карточке; после регистрации — «Проверьте почту», без автологина. */
@Component({
  selector: 'app-login-page',
  imports: [ThemeToggle, RouterLink],
  templateUrl: './login-page.html',
  styleUrls: ['../auth-page.css', './login-page.css'],
})
export class LoginPage {
  private readonly auth = inject(Auth);
  private readonly userApi = inject(UserApi);
  private readonly router = inject(Router);

  /** Адрес, с которого отправили на вход (query-параметр). */
  readonly returnUrl = input<string>();
  /** После подтверждения email — `confirmed=1`; после сброса пароля — `reset=1`. */
  readonly confirmed = input<string>();
  readonly reset = input<string>();

  protected readonly loginMaxLength = userLoginMaxLength;
  protected readonly nameMaxLength = userNameMaxLength;
  protected readonly emailMaxLength = userEmailMaxLength;
  protected readonly passwordMaxLength = passwordMaxLength;

  protected readonly mode = signal<Mode>('login');
  protected readonly email = signal('');
  protected readonly login = signal('');
  protected readonly name = signal('');
  protected readonly password = signal('');
  protected readonly rememberMe = signal(false);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly info = computed(() => {
    if (this.confirmed() === '1') {
      return 'Почта подтверждена, войдите';
    }
    if (this.reset() === '1') {
      return 'Пароль изменён, войдите';
    }
    return null;
  });

  protected readonly registering = computed(() => this.mode() === 'register');
  protected readonly canSubmit = computed(
    () =>
      !this.saving() &&
      !!this.login().trim() &&
      !!this.password() &&
      (!this.registering() || (!!this.name().trim() && !!this.email().trim())),
  );

  protected switchMode(mode: Mode): void {
    this.mode.set(mode);
    this.error.set(null);
  }

  protected onInput(field: 'email' | 'login' | 'name' | 'password', event: Event): void {
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

    this.saving.set(true);
    this.error.set(null);

    if (this.registering()) {
      const email = this.email().trim();
      this.userApi.create({ email, login, name: this.name().trim(), password }).subscribe({
        next: () => {
          this.saving.set(false);
          void this.router.navigate(['/check-email'], { queryParams: { email } });
        },
        error: (error: unknown) => {
          this.saving.set(false);
          this.error.set(problemDetail(error, 'Не удалось зарегистрироваться'));
        },
      });
      return;
    }

    this.auth.login({ login, password, rememberMe }).subscribe({
      next: () => {
        this.saving.set(false);
        void this.router.navigateByUrl(safeReturnUrl(this.returnUrl()));
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.error.set(problemDetail(error, 'Не удалось войти'));
      },
    });
  }
}
