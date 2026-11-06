import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideAppEnvironment } from '@booking-calendars/shared/environment';
import { firstValueFrom, Observable } from 'rxjs';
import { CalendarRequest } from './calendars.models';
import { CalendarsService } from './calendars.service';

const request: CalendarRequest = {
  title: 'Intro call',
  description: null,
  locationNote: null,
  slug: null,
  meetingLengths: [{ minutes: 30, bufferBeforeMinutes: 5, bufferAfterMinutes: 5 }],
};

describe('CalendarsService', () => {
  let service: CalendarsService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideAppEnvironment({ apiBaseUrl: '/api' }),
      ],
    });
    service = TestBed.inject(CalendarsService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  async function expectCall<T>(
    call: Observable<T>,
    method: string,
    url: string,
    body: unknown,
    response: object | null = {},
  ): Promise<void> {
    const result = firstValueFrom(call, { defaultValue: null });
    const sent = http.expectOne(url);
    expect(sent.request.method).toBe(method);
    expect(sent.request.body).toEqual(body);
    sent.flush(response);
    expect(await result).toEqual(response);
  }

  it('reads calendars', async () => {
    await expectCall(service.list(), 'GET', '/api/calendars', null, []);
    await expectCall(service.get('c1'), 'GET', '/api/calendars/c1', null, { id: 'c1' });
  });

  it('creates, updates and deletes calendars', async () => {
    await expectCall(service.create(request), 'POST', '/api/calendars', request);
    await expectCall(service.update('c1', request), 'PUT', '/api/calendars/c1', request);
    await expectCall(service.delete('c1'), 'DELETE', '/api/calendars/c1', null, null);
  });

  it('pauses calendars and sets their weekly hours', async () => {
    const ranges = [{ day: 1, start: '09:00', end: '12:00' }];

    await expectCall(service.setPaused('c1', true), 'PUT', '/api/calendars/c1/paused', {
      isPaused: true,
    });
    await expectCall(
      service.setWeeklyHours('c1', ranges),
      'PUT',
      '/api/calendars/c1/weekly-hours',
      { ranges },
    );
  });

  it('reads, creates and deletes blocked periods', async () => {
    const period = {
      calendarId: null,
      startDate: '2030-01-01',
      endDate: '2030-01-02',
      label: null,
    };

    await expectCall(service.listBlockedPeriods(), 'GET', '/api/blocked-periods', null, []);
    await expectCall(service.createBlockedPeriod(period), 'POST', '/api/blocked-periods', period);
    await expectCall(
      service.deleteBlockedPeriod('b1'),
      'DELETE',
      '/api/blocked-periods/b1',
      null,
      null,
    );
  });
});
