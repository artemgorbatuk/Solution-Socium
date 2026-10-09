import { messageFullTime, messageTime } from './message-time';

describe('messageTime', () => {
  const now = new Date(2026, 9, 9, 18, 0);

  it('Time_Format_WithTodayMessage_ShouldReturnHoursAndMinutes', () => {
    expect(messageTime(new Date(2026, 9, 9, 15, 2).toISOString(), now)).toBe('15:02');
  });

  it('Time_Format_WithEarlierDayMessage_ShouldReturnDateAndTime', () => {
    expect(messageTime(new Date(2026, 9, 8, 15, 2).toISOString(), now)).toBe('08.10.2026, 15:02');
  });

  it('FullTime_Format_WithMessage_ShouldContainWeekdayMonthAndSeconds', () => {
    const text = messageFullTime(new Date(2026, 9, 9, 15, 2, 7).toISOString());

    expect(text).toContain('пятница');
    expect(text).toContain('9 октября 2026');
    expect(text).toContain('15:02:07');
  });
});
