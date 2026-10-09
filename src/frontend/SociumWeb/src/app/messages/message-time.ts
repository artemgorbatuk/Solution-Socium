const timeFormat = new Intl.DateTimeFormat('ru-RU', { hour: '2-digit', minute: '2-digit' });
const dateTimeFormat = new Intl.DateTimeFormat('ru-RU', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
});
const fullFormat = new Intl.DateTimeFormat('ru-RU', { dateStyle: 'full', timeStyle: 'medium' });

/** Время сообщения в местном часовом поясе: «15:02» за сегодня, иначе «08.10.2026, 15:02». */
export function messageTime(createdAt: string, now: Date = new Date()): string {
  const date = new Date(createdAt);
  return isSameDay(date, now) ? timeFormat.format(date) : dateTimeFormat.format(date);
}

/** Полная дата для всплывающей подсказки. */
export function messageFullTime(createdAt: string): string {
  return fullFormat.format(new Date(createdAt));
}

function isSameDay(left: Date, right: Date): boolean {
  return (
    left.getFullYear() === right.getFullYear() && left.getMonth() === right.getMonth() && left.getDate() === right.getDate()
  );
}
