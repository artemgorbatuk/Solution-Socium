import { Component, computed, inject } from '@angular/core';
import { Theme } from '../../shared/theme/theme';

@Component({
  selector: 'app-theme-toggle',
  templateUrl: './theme-toggle.html',
  styleUrl: './theme-toggle.css',
})
export class ThemeToggle {
  private readonly theme = inject(Theme);

  protected readonly dark = computed(() => this.theme.current() === 'dark');
  protected readonly label = computed(() => (this.dark() ? 'Включить светлую тему' : 'Включить тёмную тему'));

  protected toggle(): void {
    this.theme.toggle();
  }
}
