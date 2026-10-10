import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { signInAs } from '../../account/auth.testing';
import { Shell } from './shell';

describe('Shell', () => {
  afterEach(() => delete document.documentElement.dataset['theme']);

  beforeEach(async () => {
    localStorage.clear();
    await TestBed.configureTestingModule({
      imports: [Shell],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();
    signInAs('u1', 'Иван');
  });

  it('Layout_Render_WithSession_ShouldShowSidebarWithAccountTopbarAndContent', () => {
    const fixture = TestBed.createComponent(Shell);
    fixture.detectChanges();
    const element: HTMLElement = fixture.nativeElement;

    expect(element.querySelector('app-sidebar app-account-menu')!.textContent).toContain('Иван');
    expect(element.querySelector('.main > [role=banner] [aria-label="Включить тёмную тему"]')).not.toBeNull();
    expect(element.querySelector('.main > main.content')).not.toBeNull();
  });
});
