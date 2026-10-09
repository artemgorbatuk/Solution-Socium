import { isSubmitKey } from './submit-key';

describe('isSubmitKey', () => {
  it('Key_Check_WithEnter_ShouldSubmit', () => {
    expect(isSubmitKey(new KeyboardEvent('keydown', { key: 'Enter' }))).toBe(true);
  });

  it('Key_Check_WithShiftEnterOrImeOrOtherKey_ShouldNotSubmit', () => {
    expect(isSubmitKey(new KeyboardEvent('keydown', { key: 'Enter', shiftKey: true }))).toBe(false);
    expect(isSubmitKey(new KeyboardEvent('keydown', { key: 'Enter', isComposing: true }))).toBe(false);
    expect(isSubmitKey(new KeyboardEvent('keydown', { key: 'a' }))).toBe(false);
  });
});
