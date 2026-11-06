import { browserTimeZone, timeZoneOptions } from './time-zones';

describe('time zones', () => {
  afterEach(() => vi.restoreAllMocks());

  it('reads the time zone of the browser', () => {
    vi.spyOn(Intl.DateTimeFormat.prototype, 'resolvedOptions').mockReturnValue({
      timeZone: 'Europe/Berlin',
    } as Intl.ResolvedDateTimeFormatOptions);

    expect(browserTimeZone()).toBe('Europe/Berlin');
  });

  it('falls back to UTC when the browser has no time zone', () => {
    vi.spyOn(Intl.DateTimeFormat.prototype, 'resolvedOptions').mockReturnValue({
      timeZone: '',
    } as Intl.ResolvedDateTimeFormatOptions);

    expect(browserTimeZone()).toBe('UTC');
  });

  it('lists the known zones once each, with UTC and the current one, sorted', () => {
    vi.spyOn(Intl, 'supportedValuesOf').mockReturnValue(['Europe/Paris', 'America/New_York']);

    expect(timeZoneOptions('Etc/GMT+5')).toEqual([
      'America/New_York',
      'Etc/GMT+5',
      'Europe/Paris',
      'UTC',
    ]);
    expect(timeZoneOptions('Europe/Paris')).toEqual(['America/New_York', 'Europe/Paris', 'UTC']);
  });
});
