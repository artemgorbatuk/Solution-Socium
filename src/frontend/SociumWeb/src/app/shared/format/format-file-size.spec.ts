import { formatFileSize } from './format-file-size';

describe('formatFileSize', () => {
  it('Format_WithBytesUnderKilobyte_ShouldUseBytesUnit', () => {
    expect(formatFileSize(0)).toBe('0 Б');
    expect(formatFileSize(512)).toBe('512 Б');
    expect(formatFileSize(1023)).toBe('1023 Б');
  });

  it('Format_WithKilobytesAndMegabytes_ShouldPickUnit', () => {
    expect(formatFileSize(17564)).toBe('17,2 КБ');
    expect(formatFileSize(108345)).toBe('105,8 КБ');
    expect(formatFileSize(5 * 1024 * 1024)).toBe('5 МБ');
  });

  it('Format_WithInvalid_ShouldReturnZeroBytes', () => {
    expect(formatFileSize(-1)).toBe('0 Б');
    expect(formatFileSize(Number.NaN)).toBe('0 Б');
  });
});
