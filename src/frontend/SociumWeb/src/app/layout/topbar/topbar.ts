import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Auth } from '../../account/auth';
import { ParticipantsPanelState } from '../../participants/participants-panel-state';
import { Icon } from '../../shared/icon/icon';
import { ThemeToggle } from '../theme-toggle/theme-toggle';
import { TopbarTitle } from './topbar-title';

@Component({
  selector: 'app-topbar',
  imports: [ThemeToggle, Icon],
  template: `
    @if (title.text(); as text) {
      <button
        type="button"
        class="close"
        [attr.aria-label]="title.closeLabel()"
        [title]="title.closeLabel()"
        (click)="close()"
      >
        ×
      </button>
      <h1 class="title" [title]="text">{{ text }}</h1>
    }
    @if (participantsPanel.available()) {
      <button
        type="button"
        class="icon participants"
        aria-label="Участники"
        title="Участники"
        [attr.aria-pressed]="participantsPanel.open()"
        (click)="participantsPanel.toggle()"
      >
        <app-icon name="users" />
      </button>
    }
    @if (participantsPanel.requestsAvailable()) {
      <button
        type="button"
        class="icon requests"
        aria-label="Заявки"
        title="Заявки"
        [attr.aria-pressed]="participantsPanel.requestsOpen()"
        (click)="participantsPanel.toggleRequests()"
      >
        <app-icon name="inbox" />
        @if (participantsPanel.pendingJoinRequestCount(); as count) {
          @if (count > 0) {
            <span class="badge" aria-label="Заявок: {{ count }}">{{ count }}</span>
          }
        }
      </button>
    }
    @if (auth.user()?.canManageUsers) {
      <button type="button" class="icon users-admin" aria-label="Пользователи" title="Пользователи" (click)="openUsers()">
        <app-icon name="shield" />
      </button>
    }
    <app-theme-toggle />
  `,
  styleUrl: './topbar.css',
  host: { role: 'banner' },
})
export class Topbar {
  private readonly router = inject(Router);

  protected readonly title = inject(TopbarTitle);
  protected readonly participantsPanel = inject(ParticipantsPanelState);
  protected readonly auth = inject(Auth);

  protected close(): void {
    void this.router.navigateByUrl('/');
  }

  protected openUsers(): void {
    void this.router.navigateByUrl('/users');
  }
}
