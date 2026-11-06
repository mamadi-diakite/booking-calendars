import { Calendar } from '@booking-calendars/shared/core';
import { AppHarness } from '../../testing/app-harness';
import { calendar } from '../../testing/calendar-fixtures';

describe('Calendars page', () => {
  let app: AppHarness;
  const paused = calendar({
    id: 'cal-2',
    title: 'Coaching',
    slug: 'coaching',
    publicPath: '/book/coaching',
    isPaused: true,
    meetingLengths: [{ minutes: 30, bufferBeforeMinutes: 0, bufferAfterMinutes: 0 }],
  });

  beforeEach(async () => {
    app = new AppHarness();
    app.signIn();
    await app.open('/calendars');
  });

  afterEach(() => {
    app.http.verify();
    vi.restoreAllMocks();
  });

  async function load(calendars: Calendar[] = [calendar(), paused]): Promise<void> {
    app.http.expectOne('/api/calendars').flush(calendars);
    await app.settle();
  }

  it('needs a signed-in user', async () => {
    await load();
    app.button('header nav button').click();
    await app.settle();

    await app.open('/calendars');

    expect(app.router.url).toBe('/login?returnUrl=%2Fcalendars');
  });

  it('lists the calendars with their lengths, status and public link', async () => {
    expect(app.text('[role="status"]')).toBe('Loading your calendars…');

    await load();

    expect(app.texts('[data-testid="title"]')).toEqual(['Intro call', 'Coaching']);
    expect(app.texts('[data-testid="lengths"]')).toEqual(['15 min · 1 hour', '30 min']);
    expect(app.texts('[data-testid="status"]')).toEqual([
      'Taking bookings',
      'Paused: takes no bookings',
    ]);
    expect(app.texts('[data-testid="link"]')).toEqual([
      `${location.origin}/book/intro-call`,
      `${location.origin}/book/coaching`,
    ]);
  });

  it('invites to create a first calendar', async () => {
    await load([]);

    expect(app.text('[data-testid="calendars"]')).toBe('No calendars yet. Create your first one.');
  });

  it('says when the calendars cannot be loaded', async () => {
    app.http.expectOne('/api/calendars').flush(null, { status: 500, statusText: 'Server Error' });
    await app.settle();

    expect(app.text('[role="alert"]')).toBe(
      'Your calendars could not be loaded. Please try again later.',
    );
  });

  it('copies the public link', async () => {
    await load();
    const writeText = vi.fn(() => Promise.resolve());
    Object.defineProperty(navigator, 'clipboard', { value: { writeText }, configurable: true });

    await app.click('[aria-label="Copy the link of Intro call"]');
    await app.settle();

    expect(writeText).toHaveBeenCalledWith(`${location.origin}/book/intro-call`);
    expect(app.text('[role="status"]')).toBe(`Link copied: ${location.origin}/book/intro-call`);
  });

  it('explains when the link cannot be copied', async () => {
    await load();
    Object.defineProperty(navigator, 'clipboard', { value: undefined, configurable: true });

    await app.click('[aria-label="Copy the link of Intro call"]');
    await app.settle();

    expect(app.text('[role="alert"]')).toBe(
      'The link could not be copied. Select it on the page instead.',
    );
  });

  it('pauses and resumes a calendar', async () => {
    await load();

    await app.click('[aria-label="Pause Intro call"]');
    const pause = app.http.expectOne('/api/calendars/cal-1/paused');
    expect(pause.request.body).toEqual({ isPaused: true });
    pause.flush(calendar({ isPaused: true }));
    await app.settle();
    expect(app.text('[role="status"]')).toBe('"Intro call" is paused and takes no bookings.');
    await load();

    await app.click('[aria-label="Resume Coaching"]');
    const resume = app.http.expectOne('/api/calendars/cal-2/paused');
    expect(resume.request.body).toEqual({ isPaused: false });
    resume.flush({ ...paused, isPaused: false });
    await app.settle();
    expect(app.text('[role="status"]')).toBe('"Coaching" takes bookings again.');
    await load();
  });

  it('shows why a pause failed', async () => {
    await load();

    await app.click('[aria-label="Pause Intro call"]');
    app.http
      .expectOne('/api/calendars/cal-1/paused')
      .flush({ title: 'Not Found' }, { status: 404, statusText: 'Not Found' });
    await app.settle();

    expect(app.text('[role="alert"]')).toBe('Not Found');
  });

  it('deletes a calendar once confirmed', async () => {
    await load();
    const confirm = vi.spyOn(window, 'confirm').mockReturnValue(true);

    await app.click('[aria-label="Delete Coaching"]');
    expect(confirm).toHaveBeenCalledWith(
      'Delete "Coaching" with its weekly hours and blocked periods?',
    );
    const request = app.http.expectOne('/api/calendars/cal-2');
    expect(request.request.method).toBe('DELETE');
    request.flush(null, { status: 204, statusText: 'No Content' });
    await app.settle();

    expect(app.text('[role="status"]')).toBe('"Coaching" was deleted.');
    await load([calendar()]);
    expect(app.texts('[data-testid="title"]')).toEqual(['Intro call']);
  });

  it('keeps the calendar when the deletion is cancelled or fails', async () => {
    await load();
    const confirm = vi.spyOn(window, 'confirm').mockReturnValue(false);

    await app.click('[aria-label="Delete Coaching"]');
    app.http.expectNone('/api/calendars/cal-2');

    confirm.mockReturnValue(true);
    await app.click('[aria-label="Delete Coaching"]');
    app.http
      .expectOne('/api/calendars/cal-2')
      .flush(null, { status: 500, statusText: 'Server Error' });
    await app.settle();

    expect(app.text('[role="alert"]')).toBe('Something went wrong. Please try again.');
  });
});
