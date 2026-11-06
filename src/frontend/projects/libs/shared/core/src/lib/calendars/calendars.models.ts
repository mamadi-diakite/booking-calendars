/** Meeting lengths a calendar can offer, in minutes. */
export const MEETING_LENGTHS = [15, 30, 60] as const;

export const MAX_BUFFER_MINUTES = 120;

/** Day numbers sent by the API (0 = Sunday), listed from Monday. */
export const WEEK_DAYS: readonly { readonly day: number; readonly name: string }[] = [
  { day: 1, name: 'Monday' },
  { day: 2, name: 'Tuesday' },
  { day: 3, name: 'Wednesday' },
  { day: 4, name: 'Thursday' },
  { day: 5, name: 'Friday' },
  { day: 6, name: 'Saturday' },
  { day: 0, name: 'Sunday' },
];

export interface MeetingLength {
  readonly minutes: number;
  readonly bufferBeforeMinutes: number;
  readonly bufferAfterMinutes: number;
}

/** One range of weekly hours; times use `HH:mm` in the owner's time zone. */
export interface WeeklyHoursRange {
  readonly day: number;
  readonly start: string;
  readonly end: string;
}

export interface Calendar {
  readonly id: string;
  readonly title: string;
  readonly description: string | null;
  readonly locationNote: string | null;
  readonly slug: string;
  /** Address of the public page, e.g. `/book/intro-call`. */
  readonly publicPath: string;
  readonly isPaused: boolean;
  readonly meetingLengths: readonly MeetingLength[];
  readonly weeklyHours: readonly WeeklyHoursRange[];
  readonly createdAtUtc: string;
}

/** An empty slug keeps the current one, or builds one from the title for a new calendar. */
export interface CalendarRequest {
  readonly title: string;
  readonly description: string | null;
  readonly locationNote: string | null;
  readonly slug: string | null;
  readonly meetingLengths: readonly MeetingLength[];
}

/** Whole days from `startDate` to `endDate` included, as `yyyy-MM-dd`. */
export interface BlockedPeriod {
  readonly id: string;
  /** `null` for a period on all calendars. */
  readonly calendarId: string | null;
  readonly startDate: string;
  readonly endDate: string;
  readonly label: string | null;
}

export interface BlockedPeriodRequest {
  readonly calendarId: string | null;
  readonly startDate: string;
  readonly endDate: string;
  readonly label: string | null;
}

/** `1 hour` for 60 minutes, `15 min` otherwise. */
export function meetingLengthLabel(minutes: number): string {
  return minutes === 60 ? '1 hour' : `${minutes} min`;
}

export function weekDayName(day: number): string {
  return WEEK_DAYS.find((weekDay) => weekDay.day === day)?.name ?? `Day ${day}`;
}
