/** The IANA time zone of the browser, or `UTC` when the browser does not tell. */
export function browserTimeZone(): string {
  return Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC';
}

/** Every IANA time zone the browser knows, with `UTC` and the given current zone, sorted. */
export function timeZoneOptions(current: string): string[] {
  return [...new Set(['UTC', current, ...Intl.supportedValuesOf('timeZone')])].sort();
}
