import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { ParticipantsPanelState } from '../../participants/participants-panel-state';
import { Icon } from '../../shared/icon/icon';
import { ThemeToggle } from '../theme-toggle/theme-toggle';
import { TopbarTitle } from './topbar-title';

@Component({
  selector: 'app-topbar',
  imports: [ThemeToggle, Icon],
  template: `
    @if (title.text(); as text) {
      <button type="button" class="close" aria-label="Закрыть чат" title="Закрыть чат" (click)="close()">×</button>
      <h1 class="title" [title]="text">{{ text }}</h1>
    }
    @if (participantsPanel.available()) {
      <button
        type="button"
        class="icon"
        aria-label="Участники"
        title="Участники"
        [attr.aria-pressed]="participantsPanel.open()"
        (click)="participantsPanel.toggle()"
      >
        <app-icon name="users" />
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

  protected close(): void {
    void this.router.navigateByUrl('/');
  }
}
