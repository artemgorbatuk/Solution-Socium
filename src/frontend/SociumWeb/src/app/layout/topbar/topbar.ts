import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { ThemeToggle } from '../theme-toggle/theme-toggle';
import { TopbarTitle } from './topbar-title';

@Component({
  selector: 'app-topbar',
  imports: [ThemeToggle],
  template: `
    @if (title.text(); as text) {
      <button type="button" class="close" aria-label="Закрыть чат" title="Закрыть чат" (click)="close()">×</button>
      <h1 class="title" [title]="text">{{ text }}</h1>
    }
    <app-theme-toggle />
  `,
  styleUrl: './topbar.css',
  host: { role: 'banner' },
})
export class Topbar {
  private readonly router = inject(Router);

  protected readonly title = inject(TopbarTitle);

  protected close(): void {
    void this.router.navigateByUrl('/');
  }
}
