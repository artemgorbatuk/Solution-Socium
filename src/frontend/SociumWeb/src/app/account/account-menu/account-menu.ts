import { Component, ElementRef, Injector, afterNextRender, computed, inject, signal, viewChild } from '@angular/core';
import { Router } from '@angular/router';
import { problemDetail } from '../../shared/api/api-response';
import { userNameMaxLength } from '../../users/user.models';
import { AccountApi } from '../account-api';
import { Auth } from '../auth';
import { loginPath } from '../login-redirect';

/** Профиль внизу боковой панели: имя текущего пользователя, правка своего имени и выход. */
@Component({
  selector: 'app-account-menu',
  templateUrl: './account-menu.html',
  styleUrl: './account-menu.css',
  host: {
    '(keydown.escape)': 'onEscape()',
    '(document:click)': 'onDocumentClick($event)',
  },
})
export class AccountMenu {
  private readonly auth = inject(Auth);
  private readonly accountApi = inject(AccountApi);
  private readonly router = inject(Router);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly injector = inject(Injector);

  protected readonly nameMaxLength = userNameMaxLength;

  protected readonly user = this.auth.user;
  protected readonly initial = computed(() => this.user()?.name.charAt(0).toLocaleUpperCase() ?? '?');
  protected readonly open = signal(false);

  protected readonly editing = signal(false);
  protected readonly draftName = signal('');
  protected readonly saving = signal(false);
  protected readonly editError = signal<string | null>(null);
  protected readonly canSave = computed(() => !this.saving() && !!this.draftName().trim());

  private readonly nameInput = viewChild<ElementRef<HTMLInputElement>>('nameInput');

  protected toggle(): void {
    this.open.update((value) => !value);
    this.cancelEdit();
  }

  protected startEdit(): void {
    this.draftName.set(this.user()?.name ?? '');
    this.editError.set(null);
    this.editing.set(true);
    afterNextRender(() => this.nameInput()?.nativeElement.select(), { injector: this.injector });
  }

  protected cancelEdit(): void {
    if (this.saving()) {
      return;
    }
    this.editing.set(false);
    this.editError.set(null);
  }

  protected onNameInput(event: Event): void {
    this.draftName.set((event.target as HTMLInputElement).value);
  }

  protected save(event: Event): void {
    event.preventDefault();
    if (!this.canSave()) {
      return;
    }

    this.saving.set(true);
    this.editError.set(null);
    this.accountApi.updateProfile({ name: this.draftName().trim() }).subscribe({
      next: (body) => {
        this.auth.updateProfile(body.response!);
        this.saving.set(false);
        this.editing.set(false);
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.editError.set(problemDetail(error, 'Не удалось изменить имя'));
      },
    });
  }

  protected logout(): void {
    this.auth.logout().subscribe(() => void this.router.navigateByUrl(loginPath));
  }

  protected onEscape(): void {
    if (this.editing()) {
      this.cancelEdit();
    } else {
      this.open.set(false);
    }
  }

  /** `composedPath` собран до обработчиков, поэтому клик по кнопке, которую шаблон уже убрал, не закрывает панель. */
  protected onDocumentClick(event: MouseEvent): void {
    if (this.open() && !event.composedPath().includes(this.host.nativeElement)) {
      this.open.set(false);
      this.cancelEdit();
    }
  }
}
