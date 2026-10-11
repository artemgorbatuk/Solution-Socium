import { Component, computed, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ThemeToggle } from '../../layout/theme-toggle/theme-toggle';
import { problemDetail } from '../../shared/api/api-response';
import { userEmailMaxLength } from '../../users/user.models';
import { AccountApi } from '../account-api';

/** После регистрации: проверьте почту и при необходимости вышлите письмо ещё раз. */
@Component({
  selector: 'app-check-email-page',
  imports: [ThemeToggle, RouterLink],
  templateUrl: './check-email-page.html',
  styleUrls: ['../auth-page.css', './check-email-page.css'],
})
export class CheckEmailPage {
  private readonly accountApi = inject(AccountApi);

  /** Email с экрана регистрации (query). */
  readonly email = input<string>('');

  protected readonly emailMaxLength = userEmailMaxLength;
  protected readonly draftEmail = signal<string | null>(null);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly success = signal<string | null>(null);
  /** Query без `email` через withComponentInputBinding даёт `undefined`, не default `''`. */
  protected readonly currentEmail = computed(() => this.draftEmail() ?? this.email() ?? '');
  protected readonly canSubmit = computed(() => !this.saving() && !!this.currentEmail().trim());

  protected onEmailInput(event: Event): void {
    this.draftEmail.set((event.target as HTMLInputElement).value);
    this.success.set(null);
  }

  protected resend(event: Event): void {
    event.preventDefault();
    if (!this.canSubmit()) {
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    this.success.set(null);
    this.accountApi.resendConfirmation({ email: this.currentEmail().trim() }).subscribe({
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
