import { formatNewMessagesLabel, isNearBottom } from './feed-scroll';

describe('feed-scroll', () => {
  it('NearBottom_Check_WithDistanceWithinThreshold_ShouldBeTrue', () => {
    expect(isNearBottom({ scrollTop: 900, scrollHeight: 1000, clientHeight: 100 }, 100)).toBe(true);
  });

  it('NearBottom_Check_WithDistanceAboveThreshold_ShouldBeFalse', () => {
    expect(isNearBottom({ scrollTop: 0, scrollHeight: 1000, clientHeight: 400 }, 100)).toBe(false);
  });

  it('Label_Format_WithOne_ShouldUseShortText', () => {
    expect(formatNewMessagesLabel(1)).toBe('↓ новые');
  });

  it('Label_Format_WithMany_ShouldIncludeCount', () => {
    expect(formatNewMessagesLabel(3)).toBe('↓ 3 новых');
  });
});
