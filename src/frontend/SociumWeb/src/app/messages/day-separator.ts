import { MessageListModel } from './message.models';
import { isSameLocalDay } from './message-time';

const sameYearFormat = new Intl.DateTimeFormat('ru-RU', { day: 'numeric', month: 'long' });
const fullDateFormat = new Intl.DateTimeFormat('ru-RU', { day: 'numeric', month: 'long', year: 'numeric' });

export type FeedRow =
  | { kind: 'day'; dayKey: string; label: string }
  | { kind: 'message'; message: MessageListModel };

/** Локальный ключ календарного дня (Y-M-D в TZ браузера). */
export function localDayKey(createdAt: string): string {
  const date = new Date(createdAt);
  return `${date.getFullYear()}-${date.getMonth() + 1}-${date.getDate()}`;
}

/** Подпись разделителя: Сегодня / Вчера / дата ru-RU. */
export function formatDayLabel(createdAt: string, now: Date = new Date()): string {
  const date = new Date(createdAt);
  if (isSameLocalDay(date, now)) {
    return 'Сегодня';
  }

  const yesterday = new Date(now.getFullYear(), now.getMonth(), now.getDate() - 1);
  if (isSameLocalDay(date, yesterday)) {
    return 'Вчера';
  }

  return date.getFullYear() === now.getFullYear()
    ? sameYearFormat.format(date)
    : fullDateFormat.format(date);
}

/** Строки ленты: разделитель перед первым сообщением каждого дня. */
export function buildFeedRows(messages: MessageListModel[], now: Date = new Date()): FeedRow[] {
  const rows: FeedRow[] = [];
  let previousKey: string | null = null;

  for (const message of messages) {
    const dayKey = localDayKey(message.createdAt);
    if (dayKey !== previousKey) {
      rows.push({ kind: 'day', dayKey, label: formatDayLabel(message.createdAt, now) });
      previousKey = dayKey;
    }
    rows.push({ kind: 'message', message });
  }

  return rows;
}

export function feedRowTrack(row: FeedRow): string {
  return row.kind === 'day' ? `day:${row.dayKey}` : row.message.id;
}
