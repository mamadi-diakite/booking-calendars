import { BlockedPeriod } from '@booking-calendars/shared/core';
import { AppHarness } from '../../testing/app-harness';
import { calendar } from '../../testing/calendar-fixtures';

const holidays: BlockedPeriod = {
  id: 'bp-1',
  calendarId: null,
  startDate: '2026-12-21',
  endDate: '2026-12-25',
  label: 'Holidays',
};
const trainingDay: BlockedPeriod = {
  id: 'bp-2',
  calendarId: 'cal-1',
  startDate: '2026-11-15',
  endDate: '2026-11-15',
  label: null,
};
const unknownCalendar: BlockedPeriod = {
  id: 'bp-3',
  calendarId: 'cal-gone',
  startDate: '2027-01-04',
  endDate: '2027-01-04',
  label: 'Moving',
};

describe('Blocked periods page', () => {
  let app: AppHarness;

  beforeEach(async () => {
    app = new AppHarness();
    app.signIn();
    await app.open('/blocked-periods');
  });

  afterEach(() => app.http.verify());

  async function load(periods: BlockedPeriod[] = [trainingDay, holidays, unknownCalendar]) {
    app.http.expectOne('/api/calendars').flush([calendar()]);
    app.http.expectOne('/api/blocked-periods').flush(periods);
    await app.settle();
  }

  it('lists the periods with their days and calendars', async () => {
    expect(app.text('[role="status"]')).toBe('Loading your blocked periods…');

    await load();

    expect(app.texts('[data-testid="dates"]').map((text) => text.replace(/\s+/g, ' '))).toEqual([
      'Nov 15, 2026',
      'Dec 21, 2026 – Dec 25, 2026',
      'Jan 4, 2027',
    ]);
    expect(app.texts('[data-testid="details"]').map((text) => text.replace(/\s+/g, ' '))).toEqual([
      'Intro call',
      'All calendars · Holidays',
      'A calendar · Moving',
    ]);
    expect(app.texts('#calendarId option')).toEqual(['All my calendars', 'Intro call']);
  });

  it('says when there are no periods', async () => {
    await load([]);

    expect(app.text('[data-testid="periods"]')).toBe('No blocked periods.');
  });

  it('says when the periods cannot be loaded', async () => {
    app.http.expectOne('/api/calendars').flush([]);
    app.http
      .expectOne('/api/blocked-periods')
      .flush(null, { status: 500, statusText: 'Server Error' });
    await app.settle();

    expect(app.text('[role="alert"]')).toBe(
      'Your blocked periods could not be loaded. Please try again later.',
    );
  });

  it('asks for both days', async () => {
    await load();

    await app.submit();

    expect(app.texts('bc-field-errors p')).toEqual([
      'Choose the first day.',
      'Choose the last day.',
    ]);
  });

  it('blocks days on all calendars', async () => {
    await load();
    await app.type('startDate', '2027-02-01');
    await app.type('endDate', '2027-02-03');
    await app.type('label', '  Ski week ');

    await app.submit();
    const request = app.http.expectOne('/api/blocked-periods');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      calendarId: null,
      startDate: '2027-02-01',
      endDate: '2027-02-03',
      label: 'Ski week',
    });
    request.flush({ ...holidays, id: 'bp-9' }, { status: 201, statusText: 'Created' });
    await app.settle();
    await load();

    expect(app.text('[role="status"]')).toBe('The days are blocked.');
    expect(app.element.querySelector<HTMLInputElement>('#startDate')!.value).toBe('');
  });

  it('blocks days on one calendar and shows the API messages', async () => {
    await load();
    await app.select('calendarId', 'cal-1');
    await app.type('startDate', '2026-11-15');
    await app.type('endDate', '2026-11-15');

    await app.submit();
    const request = app.http.expectOne('/api/blocked-periods');
    expect(request.request.body).toEqual({
      calendarId: 'cal-1',
      startDate: '2026-11-15',
      endDate: '2026-11-15',
      label: null,
    });
    request.flush(
      { errors: { startDate: ['This period overlaps the blocked period on 2026-11-15.'] } },
      { status: 400, statusText: 'Bad Request' },
    );
    await app.settle();

    expect(app.texts('bc-field-errors p')).toEqual([
      'This period overlaps the blocked period on 2026-11-15.',
    ]);
  });

  it('shows a general message when blocking fails', async () => {
    await load();
    await app.type('startDate', '2027-02-01');
    await app.type('endDate', '2027-02-01');

    await app.submit();
    app.http
      .expectOne('/api/blocked-periods')
      .flush(null, { status: 500, statusText: 'Server Error' });
    await app.settle();

    expect(app.text('[role="alert"]')).toBe('Something went wrong. Please try again.');
  });

  it('unblocks a period', async () => {
    await load();

    await app.click('[aria-label="Unblock 2026-11-15"]');
    const request = app.http.expectOne('/api/blocked-periods/bp-2');
    expect(request.request.method).toBe('DELETE');
    request.flush(null, { status: 204, statusText: 'No Content' });
    await app.settle();
    await load([holidays]);

    expect(app.texts('[data-testid="dates"]')).toHaveLength(1);
  });

  it('shows why a period could not be unblocked', async () => {
    await load();

    await app.click('[aria-label="Unblock Holidays"]');
    app.http
      .expectOne('/api/blocked-periods/bp-1')
      .flush(null, { status: 500, statusText: 'Server Error' });
    await app.settle();

    expect(app.text('[role="alert"]')).toBe('Something went wrong. Please try again.');
  });
});
