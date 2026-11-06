import { meetingLengthLabel, WEEK_DAYS, weekDayName } from './calendars.models';

describe('calendar labels', () => {
  it.each([
    [15, '15 min'],
    [30, '30 min'],
    [60, '1 hour'],
  ])('names a meeting of %i minutes', (minutes, label) => {
    expect(meetingLengthLabel(minutes)).toBe(label);
  });

  it('lists the week from Monday with the API day numbers', () => {
    expect(WEEK_DAYS.map((weekDay) => weekDay.day)).toEqual([1, 2, 3, 4, 5, 6, 0]);
    expect(weekDayName(0)).toBe('Sunday');
    expect(weekDayName(3)).toBe('Wednesday');
    expect(weekDayName(9)).toBe('Day 9');
  });
});
