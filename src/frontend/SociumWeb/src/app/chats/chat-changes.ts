import { Injectable, signal } from '@angular/core';

/** Сообщает окну чата, спискам чатов и панели участников, что изменились чаты, комнаты, состав участников или роли. */
@Injectable({ providedIn: 'root' })
export class ChatChanges {
  private readonly versionSignal = signal(0);

  readonly version = this.versionSignal.asReadonly();

  notify(): void {
    this.versionSignal.update((value) => value + 1);
  }
}
