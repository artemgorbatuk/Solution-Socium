import { formatTypingLabel } from './typing-label';

describe('formatTypingLabel', () => {
  it('Label_Format_WithNoUsers_ShouldReturnNull', () => {
    expect(formatTypingLabel([])).toBeNull();
  });

  it('Label_Format_WithOneUser_ShouldUseSingular', () => {
    expect(formatTypingLabel([{ userId: '1', name: 'Анна' }])).toBe('Анна печатает…');
  });

  it('Label_Format_WithTwoUsers_ShouldJoinNames', () => {
    expect(
      formatTypingLabel([
        { userId: '1', name: 'Анна' },
        { userId: '2', name: 'Борис' },
      ]),
    ).toBe('Анна и Борис печатают…');
  });

  it('Label_Format_WithThreeUsers_ShouldUseGeneric', () => {
    expect(
      formatTypingLabel([
        { userId: '1', name: 'Анна' },
        { userId: '2', name: 'Борис' },
        { userId: '3', name: 'Вера' },
      ]),
    ).toBe('Несколько человек печатают…');
  });
});
