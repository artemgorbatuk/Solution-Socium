import { Injectable, signal } from '@angular/core';

export const chatCloseLabel = 'Закрыть чат';

/**
 * Заголовок страницы в верхней полосе с крестиком «на главную».
 * Окно чата и разделы (например `/users`) задают его сами и сбрасывают при уходе.
 */
@Injectable({ providedIn: 'root' })
export class TopbarTitle {
  readonly text = signal<string | null>(null);
  readonly closeLabel = signal(chatCloseLabel);

  show(text: string, closeLabel: string): void {
    this.text.set(text);
    this.closeLabel.set(closeLabel);
  }

  clear(): void {
    this.text.set(null);
    this.closeLabel.set(chatCloseLabel);
  }
}
