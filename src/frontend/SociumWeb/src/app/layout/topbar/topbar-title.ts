import { Injectable, signal } from '@angular/core';

/** Название открытого чата в верхней полосе; окно чата задаёт его само и сбрасывает при уходе. */
@Injectable({ providedIn: 'root' })
export class TopbarTitle {
  readonly text = signal<string | null>(null);
}
