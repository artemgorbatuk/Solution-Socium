import { TestBed } from '@angular/core/testing';
import { HomePage } from './home-page';

describe('HomePage', () => {
  it('Home_Render_WithNoChatSelected_ShouldShowWordmarkAndHint', () => {
    const fixture = TestBed.createComponent(HomePage);
    fixture.detectChanges();
    const element: HTMLElement = fixture.nativeElement;

    expect(element.querySelector('h1')!.textContent).toBe('Socium');
    expect(element.textContent).toContain('Выберите чат в панели слева');
  });
});
