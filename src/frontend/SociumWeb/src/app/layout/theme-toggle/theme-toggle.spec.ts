import { ComponentFixture, TestBed } from '@angular/core/testing';
import { themeStorageKey } from '../../shared/theme/theme';
import { ThemeToggle } from './theme-toggle';

describe('ThemeToggle', () => {
  let fixture: ComponentFixture<ThemeToggle>;
  let element: HTMLElement;

  function toggleButton(): HTMLButtonElement {
    return element.querySelector<HTMLButtonElement>('button')!;
  }

  beforeEach(() => {
    localStorage.clear();
    fixture = TestBed.createComponent(ThemeToggle);
    element = fixture.nativeElement;
    fixture.detectChanges();
  });

  afterEach(() => {
    localStorage.clear();
    delete document.documentElement.dataset['theme'];
  });

  it('Toggle_Render_WithLightTheme_ShouldOfferDarkTheme', () => {
    expect(toggleButton().getAttribute('aria-label')).toBe('Включить тёмную тему');
    expect(toggleButton().textContent).toContain('☾');
  });

  it('Toggle_Click_WithLightTheme_ShouldApplyDarkAndOfferLight', () => {
    toggleButton().click();
    fixture.detectChanges();

    expect(document.documentElement.dataset['theme']).toBe('dark');
    expect(localStorage.getItem(themeStorageKey)).toBe('dark');
    expect(toggleButton().getAttribute('aria-label')).toBe('Включить светлую тему');
    expect(toggleButton().textContent).toContain('☀');
  });
});
