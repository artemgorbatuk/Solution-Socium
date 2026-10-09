import { Injectable, signal } from '@angular/core';

export const participantsPanelOpenStorageKey = 'socium.participants.open';

/**
 * Кнопка «Участники» в верхней полосе и панель в окне чата: окно чата включает кнопку для участника и выключает при уходе;
 * открыта ли панель — выбор пользователя, запоминается между перезагрузками.
 */
@Injectable({ providedIn: 'root' })
export class ParticipantsPanelState {
  private readonly openSignal = signal(localStorage.getItem(participantsPanelOpenStorageKey) === 'true');

  readonly available = signal(false);
  readonly open = this.openSignal.asReadonly();

  toggle(): void {
    this.setOpen(!this.openSignal());
  }

  close(): void {
    this.setOpen(false);
  }

  private setOpen(open: boolean): void {
    this.openSignal.set(open);
    localStorage.setItem(participantsPanelOpenStorageKey, String(open));
  }
}
