import { DOCUMENT, Injectable, inject, signal } from '@angular/core';

export type ThemeName = 'light' | 'dark';

/** Same key and values are read by the inline script in index.html before Angular starts. */
export const themeStorageKey = 'socium.theme';

@Injectable({ providedIn: 'root' })
export class Theme {
  private readonly document = inject(DOCUMENT);

  readonly current = signal<ThemeName>(this.restore());

  constructor() {
    this.apply(this.current());
  }

  toggle(): void {
    const next: ThemeName = this.current() === 'light' ? 'dark' : 'light';
    this.current.set(next);
    this.apply(next);
    localStorage.setItem(themeStorageKey, next);
  }

  private restore(): ThemeName {
    return localStorage.getItem(themeStorageKey) === 'dark' ? 'dark' : 'light';
  }

  private apply(theme: ThemeName): void {
    this.document.documentElement.dataset['theme'] = theme;
  }
}
