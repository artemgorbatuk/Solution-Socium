import { Directive, ElementRef, computed, inject, input, model, output, signal } from '@angular/core';

/**
 * Ручка изменения ширины соседнего блока: перетаскивание мышью/пальцем и клавиши ←/→/Home/End.
 * Ширина хранится у владельца и связывается через `[(width)]`.
 */
@Directive({
  selector: '[appResizeHandle]',
  host: {
    role: 'separator',
    tabindex: '0',
    'aria-orientation': 'vertical',
    '[attr.aria-valuenow]': 'current()',
    '[attr.aria-valuemin]': 'min()',
    '[attr.aria-valuemax]': 'max()',
    '[class.dragging]': 'dragging()',
    '(pointerdown)': 'onPointerDown($event)',
    '(pointermove)': 'onPointerMove($event)',
    '(pointerup)': 'onPointerUp($event)',
    '(pointercancel)': 'onPointerUp($event)',
    '(keydown)': 'onKeyDown($event)',
  },
})
export class ResizeHandle {
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  readonly width = model.required<number>();
  readonly min = input.required<number>();
  readonly max = input.required<number>();
  readonly step = input(16);
  readonly resizeEnd = output<number>();

  /** Ширина в пределах `min`–`max`: владелец может хранить значение за пределами, если пределы сузились. */
  protected readonly current = computed(() => this.clamp(this.width()));
  protected readonly dragging = signal(false);
  private startX = 0;
  private startWidth = 0;

  protected onPointerDown(event: PointerEvent): void {
    if (event.button !== 0) {
      return;
    }
    event.preventDefault();
    this.host.nativeElement.setPointerCapture?.(event.pointerId);
    this.startX = event.clientX;
    this.startWidth = this.current();
    this.dragging.set(true);
  }

  protected onPointerMove(event: PointerEvent): void {
    if (this.dragging()) {
      this.width.set(this.clamp(this.startWidth + event.clientX - this.startX));
    }
  }

  protected onPointerUp(event: PointerEvent): void {
    if (!this.dragging()) {
      return;
    }
    this.host.nativeElement.releasePointerCapture?.(event.pointerId);
    this.dragging.set(false);
    this.resizeEnd.emit(this.width());
  }

  protected onKeyDown(event: KeyboardEvent): void {
    const next = {
      ArrowLeft: this.current() - this.step(),
      ArrowRight: this.current() + this.step(),
      Home: this.min(),
      End: this.max(),
    }[event.key];

    if (next === undefined) {
      return;
    }
    event.preventDefault();
    this.width.set(this.clamp(next));
    this.resizeEnd.emit(this.width());
  }

  private clamp(value: number): number {
    return Math.min(this.max(), Math.max(this.min(), Math.round(value)));
  }
}
