import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('App', () => {
  afterEach(() => delete document.documentElement.dataset['theme']);

  beforeEach(async () => {
    localStorage.clear();
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    })
      .compileComponents();
  });

  it('App_Create_WithMockedHttp_ShouldCreateInstance', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('Layout_Render_WithMockedHttp_ShouldShowSidebarTopbarAndContent', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const element: HTMLElement = fixture.nativeElement;

    expect(element.querySelector('app-sidebar')).not.toBeNull();
    expect(element.querySelector('.main > [role=banner] [aria-label="Включить тёмную тему"]')).not.toBeNull();
    expect(element.querySelector('.main > main.content')).not.toBeNull();
  });
});
