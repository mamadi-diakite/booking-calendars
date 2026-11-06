import { Calendar } from '@booking-calendars/shared/core';

export function calendar(overrides: Partial<Calendar> = {}): Calendar {
  return {
    id: 'cal-1',
    title: 'Intro call',
    description: 'A first talk.',
    locationNote: 'Video call',
    slug: 'intro-call',
    publicPath: '/book/intro-call',
    isPaused: false,
    meetingLengths: [
      { minutes: 15, bufferBeforeMinutes: 5, bufferAfterMinutes: 5 },
      { minutes: 60, bufferBeforeMinutes: 15, bufferAfterMinutes: 15 },
    ],
    weeklyHours: [{ day: 1, start: '09:00', end: '12:00' }],
    createdAtUtc: '2026-11-06T09:00:00Z',
    ...overrides,
  };
}
