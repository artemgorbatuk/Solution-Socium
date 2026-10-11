import { MessageListModel } from './message.models';
import { buildFeedRows, formatDayLabel, localDayKey } from './day-separator';

describe('day-separator', () => {
  const now = new Date(2026, 9, 11, 18, 0);

  function message(id: string, createdAt: string): MessageListModel {
    return {
      id,
      text: id,
      createdAt,
      senderUserId: 'u1',
      senderName: 'Анна',
    };
  }

  it('Label_Format_WithToday_ShouldReturnSegodnya', () => {
    expect(formatDayLabel(new Date(2026, 9, 11, 9, 0).toISOString(), now)).toBe('Сегодня');
  });

  it('Label_Format_WithYesterday_ShouldReturnVchera', () => {
    expect(formatDayLabel(new Date(2026, 9, 10, 23, 59).toISOString(), now)).toBe('Вчера');
  });

  it('Label_Format_WithSameYear_ShouldOmitYear', () => {
    expect(formatDayLabel(new Date(2026, 8, 9, 12, 0).toISOString(), now)).toBe('9 сентября');
  });

  it('Label_Format_WithOtherYear_ShouldIncludeYear', () => {
    const label = formatDayLabel(new Date(2025, 9, 11, 12, 0).toISOString(), now);
    expect(label).toContain('11 октября 2025');
  });

  it('DayKey_Local_WithUtcNearMidnight_ShouldUseBrowserCalendarDay', () => {
    // 2026-10-10T22:00:00Z → в UTC+3 это 11 октября; ключ зависит от TZ среды.
    const iso = '2026-10-10T22:00:00.000Z';
    const local = new Date(iso);
    expect(localDayKey(iso)).toBe(`${local.getFullYear()}-${local.getMonth() + 1}-${local.getDate()}`);
  });

  it('FeedRows_Build_WithTwoDays_ShouldInsertSeparatorBeforeEachDay', () => {
    const rows = buildFeedRows(
      [
        message('m1', new Date(2026, 9, 9, 12, 0).toISOString()),
        message('m2', new Date(2026, 9, 9, 13, 0).toISOString()),
        message('m3', new Date(2026, 9, 10, 10, 0).toISOString()),
      ],
      now,
    );

    expect(rows.map((row) => (row.kind === 'day' ? `day:${row.label}` : row.message.id))).toEqual([
      'day:9 октября',
      'm1',
      'm2',
      'day:Вчера',
      'm3',
    ]);
  });

  it('FeedRows_Build_WithSingleDay_ShouldHaveOneSeparator', () => {
    const rows = buildFeedRows(
      [
        message('m1', new Date(2026, 9, 11, 10, 0).toISOString()),
        message('m2', new Date(2026, 9, 11, 11, 0).toISOString()),
      ],
      now,
    );

    expect(rows.filter((row) => row.kind === 'day')).toHaveLength(1);
    expect(rows[0]).toEqual({ kind: 'day', dayKey: '2026-10-11', label: 'Сегодня' });
  });

  it('FeedRows_Build_WithPrependedOlderDay_ShouldRecalculateWithoutDuplicate', () => {
    const older = message('m0', new Date(2026, 9, 9, 8, 0).toISOString());
    const sameDay = message('m1', new Date(2026, 9, 9, 12, 0).toISOString());
    const today = message('m2', new Date(2026, 9, 11, 12, 0).toISOString());

    const before = buildFeedRows([sameDay, today], now);
    const after = buildFeedRows([older, sameDay, today], now);

    expect(before.filter((row) => row.kind === 'day').map((row) => row.kind === 'day' && row.label)).toEqual([
      '9 октября',
      'Сегодня',
    ]);
    expect(after.filter((row) => row.kind === 'day').map((row) => row.kind === 'day' && row.label)).toEqual([
      '9 октября',
      'Сегодня',
    ]);
    expect(after.filter((row) => row.kind === 'day')).toHaveLength(2);
  });
});
