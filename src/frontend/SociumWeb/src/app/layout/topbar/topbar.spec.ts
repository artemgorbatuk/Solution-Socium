import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { Topbar } from './topbar';
import { TopbarTitle } from './topbar-title';

describe('Topbar', () => {
  afterEach(() => delete document.documentElement.dataset['theme']);

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
  });

  it('Title_Render_WithoutPageTitle_ShouldShowOnlyThemeToggle', () => {
    const fixture = TestBed.createComponent(Topbar);
    fixture.detectChanges();
    const element: HTMLElement = fixture.nativeElement;

    expect(element.querySelector('h1')).toBeNull();
    expect(element.querySelector('[aria-label="Закрыть чат"]')).toBeNull();
    expect(element.querySelector('app-theme-toggle')).not.toBeNull();
  });

  it('Title_Set_WithChatName_ShouldShowCloseAndTitleThenHideAfterReset', () => {
    const fixture = TestBed.createComponent(Topbar);
    const title = TestBed.inject(TopbarTitle);
    const element: HTMLElement = fixture.nativeElement;

    title.text.set('Общий');
    fixture.detectChanges();
    expect(element.querySelector('h1')!.textContent).toBe('Общий');
    expect(element.querySelector('[aria-label="Закрыть чат"] + h1')).not.toBeNull();

    title.text.set(null);
    fixture.detectChanges();
    expect(element.querySelector('h1')).toBeNull();
    expect(element.querySelector('[aria-label="Закрыть чат"]')).toBeNull();
  });

  it('Close_Click_WithOpenChat_ShouldNavigateHome', () => {
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    const fixture = TestBed.createComponent(Topbar);
    TestBed.inject(TopbarTitle).text.set('Общий');
    fixture.detectChanges();

    fixture.nativeElement.querySelector('[aria-label="Закрыть чат"]').click();

    expect(navigate).toHaveBeenCalledWith('/');
  });
});
