import { Injectable, signal } from '@angular/core';

export const participantsPanelOpenStorageKey = 'socium.participants.open';
export const joinRequestsPanelOpenStorageKey = 'socium.join-requests.open';

/**
 * Кнопки в верхней полосе и боковые панели чата: «Участники» и «Заявки».
 * Одновременно открыта только одна панель; выбор запоминается между перезагрузками.
 */
@Injectable({ providedIn: 'root' })
export class ParticipantsPanelState {
  private readonly openSignal = signal(false);
  private readonly requestsOpenSignal = signal(false);

  readonly available = signal(false);
  /** Кнопка «Заявки» для ChatAdmin. */
  readonly requestsAvailable = signal(false);
  /** Pending-заявки текущего чата для бейджа. */
  readonly pendingJoinRequestCount = signal(0);
  readonly open = this.openSignal.asReadonly();
  readonly requestsOpen = this.requestsOpenSignal.asReadonly();

  constructor() {
    const participantsOpen = localStorage.getItem(participantsPanelOpenStorageKey) === 'true';
    const requestsOpen = localStorage.getItem(joinRequestsPanelOpenStorageKey) === 'true';
    if (participantsOpen) {
      this.setOpen(true);
    } else if (requestsOpen) {
      this.setRequestsOpen(true);
    }
  }

  toggle(): void {
    this.setOpen(!this.openSignal());
  }

  toggleRequests(): void {
    this.setRequestsOpen(!this.requestsOpenSignal());
  }

  close(): void {
    this.setOpen(false);
  }

  closeRequests(): void {
    this.setRequestsOpen(false);
  }

  private setOpen(open: boolean): void {
    this.openSignal.set(open);
    localStorage.setItem(participantsPanelOpenStorageKey, String(open));
    if (open && this.requestsOpenSignal()) {
      this.requestsOpenSignal.set(false);
      localStorage.setItem(joinRequestsPanelOpenStorageKey, 'false');
    }
  }

  private setRequestsOpen(open: boolean): void {
    this.requestsOpenSignal.set(open);
    localStorage.setItem(joinRequestsPanelOpenStorageKey, String(open));
    if (open && this.openSignal()) {
      this.openSignal.set(false);
      localStorage.setItem(participantsPanelOpenStorageKey, 'false');
    }
  }
}
