/** Enter без Shift отправляет текст; Shift+Enter и Enter во время набора через IME — нет. */
export function isSubmitKey(event: KeyboardEvent): boolean {
  return event.key === 'Enter' && !event.shiftKey && !event.isComposing;
}
