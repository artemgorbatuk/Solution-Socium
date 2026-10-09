import { Injectable, signal } from '@angular/core';

/** Сообщает открытому окну чата, что чаты или комнаты изменились в боковой панели. */
@Injectable({ providedIn: 'root' })
export class ChatChanges {
  private readonly versionSignal = signal(0);

  readonly version = this.versionSignal.asReadonly();

  notify(): void {
    this.versionSignal.update((value) => value + 1);
  }
}
