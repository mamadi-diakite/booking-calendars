import { TestBed } from '@angular/core/testing';
import { SessionStore } from '@booking-calendars/shared/core';
import { AppHarness } from '../../testing/app-harness';
import { calendar } from '../../testing/calendar-fixtures';
import { CalendarEditorPage } from './calendar-editor-page';

describe('Calendar editor page', () => {
  let app: AppHarness;

  beforeEach(() => {
    app = new AppHarness();
    app.signIn();
  });

  afterEach(() => app.http.verify());

  function input(id: string): HTMLInputElement {
    return app.element.querySelector<HTMLInputElement>(`#${id}`)!;
  }

  async function openExisting(): Promise<void> {
    await app.open('/calendars/cal-1');
    expect(app.text('[role="status"]')).toBe('Loading the calendar…');
    app.http.expectOne('/api/calendars/cal-1').flush(calendar());
    await app.settle();
  }

  describe('new calendar', () => {
    beforeEach(async () => {
      await app.open('/calendars/new');
    });

    it('offers 30 minutes by default and no weekly hours yet', () => {
      expect(app.text('h1')).toBe('New calendar');
      expect([15, 30, 60].map((minutes) => input(`length-${minutes}-offered`).checked)).toEqual([
        false,
        true,
        false,
      ]);
      expect(app.element.querySelector('#add-range')).toBeNull();
    });

    it('asks for a title without calling the API', async () => {
      await app.submit();

      expect(app.texts('bc-field-errors p')).toEqual(['Enter a title.']);
    });

    it('creates the calendar and opens it to set the weekly hours', async () => {
      await app.type('title', 'Portfolio review');
      await app.type('locationNote', 'Studio');
      await app.check('length-15-offered', true);
      await app.type('length-15-before', '5');
      await app.type('length-15-after', '');
      await app.type('length-30-before', '');

      await app.submit();
      const request = app.http.expectOne('/api/calendars');
      expect(request.request.method).toBe('POST');
      expect(request.request.body).toEqual({
        title: 'Portfolio review',
        description: null,
        locationNote: 'Studio',
        slug: null,
        meetingLengths: [
          { minutes: 15, bufferBeforeMinutes: 5, bufferAfterMinutes: 0 },
          { minutes: 30, bufferBeforeMinutes: 0, bufferAfterMinutes: 0 },
        ],
      });
      request.flush(calendar({ id: 'cal-9', title: 'Portfolio review' }));
      await app.settle();

      expect(app.router.url).toBe('/calendars/cal-9?created=true');
      app.http.expectOne('/api/calendars/cal-9').flush(calendar({ id: 'cal-9' }));
      await app.settle();
      expect(app.text('[role="status"]')).toBe('Calendar created. Now set its weekly hours.');
    });

    it('shows the API messages for the link and the meeting lengths', async () => {
      await app.type('title', 'Call');
      await app.type('slug', 'Bad Slug');
      await app.check('length-30-offered', false);

      await app.submit();
      app.http.expectOne('/api/calendars').flush(
        {
          errors: {
            slug: ['Use lowercase letters, digits and single hyphens, 60 characters at most.'],
            meetingLengths: ['Choose at least one meeting length.'],
          },
        },
        { status: 400, statusText: 'Bad Request' },
      );
      await app.settle();

      expect(app.texts('bc-field-errors p')).toEqual([
        'Use lowercase letters, digits and single hyphens, 60 characters at most.',
        'Choose at least one meeting length.',
      ]);
    });
  });

  describe('existing calendar', () => {
    beforeEach(openExisting);

    it('shows its details, lengths, buffers and weekly hours', () => {
      expect(app.text('h1')).toBe('Edit calendar');
      expect(input('title').value).toBe('Intro call');
      expect(app.element.querySelector<HTMLTextAreaElement>('#description')!.value).toBe(
        'A first talk.',
      );
      expect(input('slug').value).toBe('intro-call');
      expect([15, 30, 60].map((minutes) => input(`length-${minutes}-offered`).checked)).toEqual([
        true,
        false,
        true,
      ]);
      expect(input('length-60-before').value).toBe('15');
      expect(input('length-30-after').value).toBe('0');
      expect(app.element.querySelector<HTMLSelectElement>('#range-0-day')!.value).toBe('1');
      expect(input('range-0-start').value).toBe('09:00');
      expect(input('range-0-end').value).toBe('12:00');
      expect(app.text('[data-testid="time-zone"]')).toBe(
        'Times are in your time zone, Europe/Paris. Change it',
      );
    });

    it('saves the details', async () => {
      await app.type('title', 'Intro chat');

      await app.submitForm('#title');
      const request = app.http.expectOne('/api/calendars/cal-1');
      expect(request.request.method).toBe('PUT');
      expect(request.request.body).toMatchObject({
        title: 'Intro chat',
        description: 'A first talk.',
        slug: 'intro-call',
      });
      request.flush(calendar({ title: 'Intro chat', description: null, locationNote: null }));
      await app.settle();

      expect(app.text('[role="status"]')).toBe('Calendar saved.');
      expect(app.element.querySelector<HTMLTextAreaElement>('#description')!.value).toBe('');
    });

    it('edits and saves the weekly hours', async () => {
      await app.click('#add-range');
      await app.select('range-1-day', '3');
      await app.type('range-1-start', '13:00');
      await app.click('#range-0-remove');

      await app.submitForm('#add-range');
      const request = app.http.expectOne('/api/calendars/cal-1/weekly-hours');
      expect(request.request.body).toEqual({
        ranges: [{ day: 3, start: '13:00', end: '17:00' }],
      });
      request.flush(calendar({ weeklyHours: [{ day: 3, start: '13:00', end: '17:00' }] }));
      await app.settle();

      expect(app.text('[role="status"]')).toBe('Weekly hours saved.');
      expect(app.element.querySelector<HTMLSelectElement>('#range-0-day')!.value).toBe('3');
    });

    it('shows why the weekly hours were refused', async () => {
      await app.click('#add-range');

      await app.submitForm('#add-range');
      app.http
        .expectOne('/api/calendars/cal-1/weekly-hours')
        .flush(
          { errors: { ranges: ['Monday: 09:00-12:00 and 09:00-17:00 overlap.'] } },
          { status: 400, statusText: 'Bad Request' },
        );
      await app.settle();

      expect(app.texts('bc-field-errors p')).toEqual([
        'Monday: 09:00-12:00 and 09:00-17:00 overlap.',
      ]);
    });

    it('shows a general message when a save fails', async () => {
      await app.submitForm('#title');
      app.http
        .expectOne('/api/calendars/cal-1')
        .flush(null, { status: 500, statusText: 'Server Error' });
      await app.settle();
      expect(app.texts('[role="alert"]')).toEqual(['Something went wrong. Please try again.']);

      await app.submitForm('#add-range');
      app.http
        .expectOne('/api/calendars/cal-1/weekly-hours')
        .flush(null, { status: 500, statusText: 'Server Error' });
      await app.settle();
      expect(app.texts('[role="alert"]')).toEqual([
        'Something went wrong. Please try again.',
        'Something went wrong. Please try again.',
      ]);
    });

    it('says when no hours are left', async () => {
      await app.click('#range-0-remove');

      expect(app.text('[formarrayname="ranges"]')).toBe(
        'No weekly hours: nobody can book this calendar.',
      );
    });
  });

  it.each([
    [404, 'This calendar does not exist or is not yours.'],
    [500, 'The calendar could not be loaded. Please try again later.'],
  ])('explains a %i when loading', async (status, message) => {
    await app.open('/calendars/cal-1');

    app.http.expectOne('/api/calendars/cal-1').flush(null, { status, statusText: 'Error' });
    await app.settle();

    expect(app.text('[role="alert"]')).toBe(message);
  });

  it('reads the hours in UTC without a signed-in user', async () => {
    TestBed.inject(SessionStore).clear();
    const fixture = TestBed.createComponent(CalendarEditorPage);
    await fixture.whenStable();

    expect((fixture.componentInstance as unknown as { timeZone: string }).timeZone).toBe('UTC');
  });
});
