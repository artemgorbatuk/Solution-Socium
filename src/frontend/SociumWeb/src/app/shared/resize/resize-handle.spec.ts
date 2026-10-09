import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ResizeHandle } from './resize-handle';

@Component({
  imports: [ResizeHandle],
  template: `<div appResizeHandle [(width)]="width" [min]="200" [max]="480" (resizeEnd)="ended.push($event)"></div>`,
})
class Host {
  readonly width = signal(260);
  readonly ended: number[] = [];
}

describe('ResizeHandle', () => {
  let fixture: ComponentFixture<Host>;
  let handle: HTMLElement;

  function pointer(type: string, clientX: number): void {
    handle.dispatchEvent(new MouseEvent(type, { clientX, button: 0, bubbles: true }));
    fixture.detectChanges();
  }

  function key(name: string): void {
    handle.dispatchEvent(new KeyboardEvent('keydown', { key: name, bubbles: true }));
    fixture.detectChanges();
  }

  beforeEach(() => {
    fixture = TestBed.createComponent(Host);
    fixture.detectChanges();
    handle = fixture.nativeElement.querySelector('[appResizeHandle]');
  });

  it('Handle_Render_WithBounds_ShouldExposeSeparatorAria', () => {
    expect(handle.getAttribute('role')).toBe('separator');
    expect(handle.getAttribute('aria-valuenow')).toBe('260');
    expect(handle.getAttribute('aria-valuemin')).toBe('200');
    expect(handle.getAttribute('aria-valuemax')).toBe('480');
  });

  it('Handle_Drag_WithPointerInBounds_ShouldChangeWidthAndEmitOnRelease', () => {
    pointer('pointerdown', 100);
    pointer('pointermove', 150);

    expect(fixture.componentInstance.width()).toBe(310);
    expect(handle.classList).toContain('dragging');

    pointer('pointerup', 150);

    expect(handle.classList).not.toContain('dragging');
    expect(fixture.componentInstance.ended).toEqual([310]);
  });

  it('Handle_Drag_WithPointerOutOfBounds_ShouldClampToMinAndMax', () => {
    pointer('pointerdown', 100);
    pointer('pointermove', 1000);
    expect(fixture.componentInstance.width()).toBe(480);

    pointer('pointermove', -1000);
    expect(fixture.componentInstance.width()).toBe(200);
  });

  it('Handle_PointerMove_WithoutPress_ShouldKeepWidth', () => {
    pointer('pointermove', 500);

    expect(fixture.componentInstance.width()).toBe(260);
    expect(fixture.componentInstance.ended).toEqual([]);
  });

  it('Handle_Drag_WithWidthAboveMax_ShouldStartFromClampedWidth', () => {
    fixture.componentInstance.width.set(700);
    fixture.detectChanges();
    expect(handle.getAttribute('aria-valuenow')).toBe('480');

    key('ArrowLeft');
    expect(fixture.componentInstance.width()).toBe(464);

    pointer('pointerdown', 100);
    pointer('pointermove', 90);
    expect(fixture.componentInstance.width()).toBe(454);
  });

  it('Handle_Keydown_WithArrowsHomeAndEnd_ShouldResizeByStepAndToBounds', () => {
    key('ArrowRight');
    expect(fixture.componentInstance.width()).toBe(276);

    key('ArrowLeft');
    key('ArrowLeft');
    expect(fixture.componentInstance.width()).toBe(244);

    key('End');
    expect(fixture.componentInstance.width()).toBe(480);

    key('Home');
    expect(fixture.componentInstance.width()).toBe(200);
    expect(fixture.componentInstance.ended).toEqual([276, 260, 244, 480, 200]);
  });
});
