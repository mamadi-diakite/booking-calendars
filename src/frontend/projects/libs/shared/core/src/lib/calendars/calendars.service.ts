import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { APP_ENVIRONMENT } from '@booking-calendars/shared/environment';
import { Observable } from 'rxjs';
import {
  BlockedPeriod,
  BlockedPeriodRequest,
  Calendar,
  CalendarRequest,
  WeeklyHoursRange,
} from './calendars.models';

/** The calendars of the signed-in owner and their blocked periods. */
@Injectable({ providedIn: 'root' })
export class CalendarsService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = inject(APP_ENVIRONMENT).apiBaseUrl;
  private readonly calendarsUrl = `${this.apiBaseUrl}/calendars`;
  private readonly blockedPeriodsUrl = `${this.apiBaseUrl}/blocked-periods`;

  /** Oldest first. */
  list(): Observable<Calendar[]> {
    return this.http.get<Calendar[]>(this.calendarsUrl);
  }

  get(id: string): Observable<Calendar> {
    return this.http.get<Calendar>(`${this.calendarsUrl}/${id}`);
  }

  create(request: CalendarRequest): Observable<Calendar> {
    return this.http.post<Calendar>(this.calendarsUrl, request);
  }

  update(id: string, request: CalendarRequest): Observable<Calendar> {
    return this.http.put<Calendar>(`${this.calendarsUrl}/${id}`, request);
  }

  setPaused(id: string, isPaused: boolean): Observable<Calendar> {
    return this.http.put<Calendar>(`${this.calendarsUrl}/${id}/paused`, { isPaused });
  }

  /** Replaces every range of the calendar. */
  setWeeklyHours(id: string, ranges: readonly WeeklyHoursRange[]): Observable<Calendar> {
    return this.http.put<Calendar>(`${this.calendarsUrl}/${id}/weekly-hours`, { ranges });
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.calendarsUrl}/${id}`);
  }

  /** By start date. */
  listBlockedPeriods(): Observable<BlockedPeriod[]> {
    return this.http.get<BlockedPeriod[]>(this.blockedPeriodsUrl);
  }

  createBlockedPeriod(request: BlockedPeriodRequest): Observable<BlockedPeriod> {
    return this.http.post<BlockedPeriod>(this.blockedPeriodsUrl, request);
  }

  deleteBlockedPeriod(id: string): Observable<void> {
    return this.http.delete<void>(`${this.blockedPeriodsUrl}/${id}`);
  }
}
