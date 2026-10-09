import { TestBed } from '@angular/core/testing';
import { Theme, themeStorageKey } from './theme';

describe('Theme', () => {
  const root = document.documentElement;

  beforeEach(() => localStorage.clear());

  afterEach(() => {
    localStorage.clear();
    delete root.dataset['theme'];
  });

  it('Theme_Create_WithNoSavedTheme_ShouldApplyLight', () => {
    const theme = TestBed.inject(Theme);

    expect(theme.current()).toBe('light');
    expect(root.dataset['theme']).toBe('light');
  });

  it('Theme_Create_WithSavedDarkTheme_ShouldApplyDark', () => {
    localStorage.setItem(themeStorageKey, 'dark');

    const theme = TestBed.inject(Theme);

    expect(theme.current()).toBe('dark');
    expect(root.dataset['theme']).toBe('dark');
  });

  it('Theme_Create_WithUnknownSavedValue_ShouldApplyLight', () => {
    localStorage.setItem(themeStorageKey, 'purple');

    const theme = TestBed.inject(Theme);

    expect(theme.current()).toBe('light');
    expect(root.dataset['theme']).toBe('light');
  });

  it('Theme_Toggle_WithLightTheme_ShouldSwitchToDarkAndBackAndSave', () => {
    const theme = TestBed.inject(Theme);

    theme.toggle();
    expect(theme.current()).toBe('dark');
    expect(root.dataset['theme']).toBe('dark');
    expect(localStorage.getItem(themeStorageKey)).toBe('dark');

    theme.toggle();
    expect(theme.current()).toBe('light');
    expect(root.dataset['theme']).toBe('light');
    expect(localStorage.getItem(themeStorageKey)).toBe('light');
  });
});
