import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ThemeToggle } from '../../layout/theme-toggle/theme-toggle';
import { problemDetail } from '../../shared/api/api-response';
import { userEmailMaxLength } from '../../users/user.models';
import { AccountApi } from '../account-api';

/** Запрос письма со ссылкой на сброс пароля. */
@Component({
  selector: 'app-forgot-password-page',
  imports: [ThemeToggle, RouterLink],
  templateUrl: './forgot-password-page.html',
  styleUrls: ['../auth-page.css', './forgot-password-page.css'],
})
export class ForgotPasswordPage {
  private readonly accountApi = inject(AccountApi);

  protected readonly emailMaxLength = userEmailMaxLength;
  protected readonly email = signal('');
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly success = signal<string | null>(null);
  protected readonly canSubmit = computed(() => !this.saving() && !!this.email().trim());

  protected onEmailInput(event: Event): void {
    this.email.set((event.target as HTMLInputElement).value);
    this.success.set(null);
  }

  protected submit(event: Event): void {
    event.preventDefault();
    if (!this.canSubmit()) {
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    this.success.set(null);
    this.accountApi.forgotPassword({ email: this.email().trim() }).subscribe({
      next: () => {
        this.saving.set(false);
        this.success.set('Если аккаунт существует, письмо отправлено');
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.error.set(problemDetail(error, 'Не удалось отправить письмо'));
      },
    });
  }
}
