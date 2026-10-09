import { TestBed } from '@angular/core/testing';
import { Icon, IconName } from './icon';

describe('Icon', () => {
  function render(name: IconName): HTMLElement {
    const fixture = TestBed.createComponent(Icon);
    fixture.componentRef.setInput('name', name);
    fixture.detectChanges();
    return fixture.nativeElement;
  }

  it.each<[string, IconName]>([
    ['Plus', 'plus'],
    ['Pencil', 'pencil'],
    ['Trash', 'trash'],
  ])('Icon_Render_With%sName_ShouldDrawSvgHiddenFromScreenReaders', (_, name) => {
    const element = render(name);
    const svg = element.querySelector('svg')!;

    expect(element.getAttribute('aria-hidden')).toBe('true');
    expect(svg.getAttribute('data-icon')).toBe(name);
    expect(svg.querySelectorAll('path').length).toBeGreaterThan(0);
  });
});
