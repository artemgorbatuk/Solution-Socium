import { Component, computed, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { ThemeToggle } from '../../layout/theme-toggle/theme-toggle';
import { problemDetail } from '../../shared/api/api-response';
import { AccountApi } from '../account-api';
import { passwordMaxLength, passwordMinLength } from '../account.models';

/** Новый пароль по токену из письма. */
@Component({
  selector: 'app-reset-password-page',
  imports: [ThemeToggle, RouterLink],
  templateUrl: './reset-password-page.html',
  styleUrls: ['../auth-page.css', './reset-password-page.css'],
})
export class ResetPasswordPage {
  private readonly accountApi = inject(AccountApi);
  private readonly router = inject(Router);

  readonly token = input<string>('');

  protected readonly passwordMinLength = passwordMinLength;
  protected readonly passwordMaxLength = passwordMaxLength;
  protected readonly password = signal('');
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly canSubmit = computed(
    () => !this.saving() && !!this.token().trim() && this.password().length >= passwordMinLength,
  );

  protected onPasswordInput(event: Event): void {
    this.password.set((event.target as HTMLInputElement).value);
  }

  protected submit(event: Event): void {
    event.preventDefault();
    if (!this.canSubmit()) {
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    this.accountApi.resetPassword({ token: this.token().trim(), password: this.password() }).subscribe({
      next: () => void this.router.navigate(['/login'], { queryParams: { reset: '1' } }),
      error: (error: unknown) => {
        this.saving.set(false);
        this.error.set(problemDetail(error, 'Не удалось сбросить пароль'));
      },
    });
  }
}
