import { Component, OnInit, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { ThemeToggle } from '../../layout/theme-toggle/theme-toggle';
import { problemDetail } from '../../shared/api/api-response';
import { AccountApi } from '../account-api';

/** Ссылка из письма: подтверждает email и отправляет на вход. */
@Component({
  selector: 'app-confirm-email-page',
  imports: [ThemeToggle, RouterLink],
  templateUrl: './confirm-email-page.html',
  styleUrls: ['../auth-page.css', './confirm-email-page.css'],
})
export class ConfirmEmailPage implements OnInit {
  private readonly accountApi = inject(AccountApi);
  private readonly router = inject(Router);

  readonly token = input<string>('');

  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  ngOnInit(): void {
    const token = this.token().trim();
    if (!token) {
      this.loading.set(false);
      this.error.set('Ссылка подтверждения недействительна');
      return;
    }

    this.accountApi.confirmEmail({ token }).subscribe({
      next: () => void this.router.navigate(['/login'], { queryParams: { confirmed: '1' } }),
      error: (error: unknown) => {
        this.loading.set(false);
        this.error.set(problemDetail(error, 'Не удалось подтвердить email'));
      },
    });
  }
}
