/** Порог «у низа» ленты (px). */
export const nearBottomThresholdPx = 100;

export function distanceToBottom(feed: Pick<HTMLElement, 'scrollTop' | 'scrollHeight' | 'clientHeight'>): number {
  return feed.scrollHeight - feed.scrollTop - feed.clientHeight;
}

export function isNearBottom(
  feed: Pick<HTMLElement, 'scrollTop' | 'scrollHeight' | 'clientHeight'>,
  thresholdPx = nearBottomThresholdPx,
): boolean {
  return distanceToBottom(feed) <= thresholdPx;
}

/** Подпись кнопки «↓ новые». */
export function formatNewMessagesLabel(count: number): string {
  if (count <= 0) {
    return '';
  }
  if (count === 1) {
    return '↓ новые';
  }
  return `↓ ${count} новых`;
}
