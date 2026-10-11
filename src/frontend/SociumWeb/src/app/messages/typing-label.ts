/** Idle без ввода → StopTyping (мс). */
export const typingIdleMs = 3000;
/** Повторный StartTyping пока печатает (мс). */
export const typingRefreshMs = 2000;
/** Сброс у получателя без нового typingStarted (мс). */
export const typingExpireMs = 3500;

export interface TypingUser {
  userId: string;
  name: string;
}

export function formatTypingLabel(users: TypingUser[]): string | null {
  if (users.length === 0) {
    return null;
  }
  if (users.length === 1) {
    return `${users[0].name} печатает…`;
  }
  if (users.length === 2) {
    return `${users[0].name} и ${users[1].name} печатают…`;
  }
  return 'Несколько человек печатают…';
}
