import { Injectable, signal } from '@angular/core';

export const collapsedRoomsStorageKey = 'socium.sidebar.collapsedRooms';

/** Раскрытие комнат в боковой панели: по умолчанию комната раскрыта, свёрнутые запоминаются. */
@Injectable({ providedIn: 'root' })
export class RoomExpansion {
  private readonly collapsed = signal<ReadonlySet<string>>(this.restore());

  isExpanded(roomId: string): boolean {
    return !this.collapsed().has(roomId);
  }

  toggle(roomId: string): void {
    if (this.isExpanded(roomId)) {
      this.collapse(roomId);
    } else {
      this.expand(roomId);
    }
  }

  expand(roomId: string): void {
    if (this.isExpanded(roomId)) {
      return;
    }
    const next = new Set(this.collapsed());
    next.delete(roomId);
    this.save(next);
  }

  /** Забывает удалённые комнаты, чтобы ключ в `localStorage` не рос. */
  retain(roomIds: readonly string[]): void {
    const existing = new Set(roomIds);
    const next = new Set([...this.collapsed()].filter((id) => existing.has(id)));
    if (next.size !== this.collapsed().size) {
      this.save(next);
    }
  }

  private collapse(roomId: string): void {
    this.save(new Set(this.collapsed()).add(roomId));
  }

  private save(collapsed: ReadonlySet<string>): void {
    this.collapsed.set(collapsed);
    if (collapsed.size) {
      localStorage.setItem(collapsedRoomsStorageKey, JSON.stringify([...collapsed]));
    } else {
      localStorage.removeItem(collapsedRoomsStorageKey);
    }
  }

  private restore(): ReadonlySet<string> {
    try {
      const saved: unknown = JSON.parse(localStorage.getItem(collapsedRoomsStorageKey) ?? '[]');
      return new Set(Array.isArray(saved) ? saved.filter((id): id is string => typeof id === 'string') : []);
    } catch {
      return new Set();
    }
  }
}
