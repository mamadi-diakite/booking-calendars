import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  BlockedPeriod,
  Calendar,
  CalendarsService,
  FormSubmission,
  LoadState,
  toFormErrors,
  withLoadState,
} from '@booking-calendars/shared/core';
import { FieldErrors } from '@booking-calendars/shared/ui';
import { forkJoin, switchMap } from 'rxjs';

interface BlockedPeriodsView {
  readonly calendars: Calendar[];
  readonly periods: BlockedPeriod[];
}

/** Whole days the owner is away, on one calendar or on all of them. */
@Component({
  selector: 'bc-blocked-periods-page',
  imports: [DatePipe, ReactiveFormsModule, RouterLink, FieldErrors],
  template: `
    <section class="mx-auto max-w-3xl space-y-8">
      <a routerLink="/calendars" class="link text-sm">Back to calendars</a>
      <h1 class="text-2xl font-bold tracking-tight">Blocked periods</h1>

      @let current = state();
      @if (current.status === 'loading') {
        <p class="text-slate-600" role="status">Loading your blocked periods…</p>
      } @else if (current.status === 'ready') {
        <form class="panel" [formGroup]="form" (ngSubmit)="block()" novalidate>
          <h2 class="text-lg font-semibold">Block days</h2>
          <p class="text-sm text-slate-600">
            No time is offered on these days, from the first to the last included.
          </p>
          @if (created(); as message) {
            <p class="alert-success" role="status">{{ message }}</p>
          }
          @if (submission.errors().form; as message) {
            <p class="alert-error" role="alert">{{ message }}</p>
          }

          <div>
            <label class="form-label" for="calendarId">Calendar</label>
            <select id="calendarId" class="form-input" formControlName="calendarId">
              <option value="">All my calendars</option>
              @for (calendar of current.value.calendars; track calendar.id) {
                <option [value]="calendar.id">{{ calendar.title }}</option>
              }
            </select>
            <bc-field-errors [messages]="submission.messagesFor('calendarId')" />
          </div>

          <div class="grid gap-4 sm:grid-cols-2">
            <div>
              <label class="form-label" for="startDate">First day</label>
              <input id="startDate" class="form-input" type="date" formControlName="startDate" />
              <bc-field-errors [messages]="submission.messagesFor('startDate')" />
            </div>
            <div>
              <label class="form-label" for="endDate">Last day</label>
              <input id="endDate" class="form-input" type="date" formControlName="endDate" />
              <bc-field-errors [messages]="submission.messagesFor('endDate')" />
            </div>
          </div>

          <div>
            <label class="form-label" for="label">Label (optional)</label>
            <input
              id="label"
              class="form-input"
              formControlName="label"
              placeholder="Holidays, training day…"
            />
            <bc-field-errors [messages]="submission.messagesFor('label')" />
          </div>

          <button class="button-primary sm:w-auto" type="submit" [disabled]="submission.pending()">
            Block these days
          </button>
        </form>

        <section class="space-y-4" aria-labelledby="periods-title">
          <h2 id="periods-title" class="text-lg font-semibold">Your blocked periods</h2>
          @if (actionError(); as message) {
            <p class="alert-error" role="alert">{{ message }}</p>
          }
          <ul class="space-y-3" data-testid="periods">
            @for (period of current.value.periods; track period.id) {
              <li
                class="flex flex-wrap items-start justify-between gap-4 rounded-xl border border-slate-200 bg-white p-4 shadow-sm"
              >
                <div class="space-y-1">
                  <p class="font-medium" data-testid="dates">
                    {{ period.startDate | date: 'mediumDate' }}
                    @if (period.endDate !== period.startDate) {
                      – {{ period.endDate | date: 'mediumDate' }}
                    }
                  </p>
                  <p class="text-sm text-slate-600" data-testid="details">
                    {{ calendarName(current.value.calendars, period) }}
                    @if (period.label) {
                      · {{ period.label }}
                    }
                  </p>
                </div>
                <button
                  type="button"
                  class="text-sm font-medium text-rose-700 hover:underline"
                  [attr.aria-label]="'Unblock ' + (period.label ?? period.startDate)"
                  (click)="unblock(period)"
                >
                  Unblock
                </button>
              </li>
            } @empty {
              <li class="text-slate-600">No blocked periods.</li>
            }
          </ul>
        </section>
      } @else {
        <p class="alert-error" role="alert">
          Your blocked periods could not be loaded. Please try again later.
        </p>
      }
    </section>
  `,
})
export class BlockedPeriodsPage {
  private readonly calendars = inject(CalendarsService);
  private readonly version = signal(0);

  protected readonly created = signal<string | null>(null);
  protected readonly actionError = signal<string | null>(null);
  protected readonly form = inject(NonNullableFormBuilder).group({
    calendarId: [''],
    startDate: ['', Validators.required],
    endDate: ['', Validators.required],
    label: [''],
  });
  protected readonly submission = new FormSubmission(this.form, {
    startDate: 'Choose the first day.',
    endDate: 'Choose the last day.',
  });
  protected readonly state = toSignal(
    toObservable(this.version).pipe(
      switchMap(() =>
        withLoadState(
          forkJoin({
            calendars: this.calendars.list(),
            periods: this.calendars.listBlockedPeriods(),
          }),
        ),
      ),
    ),
    { initialValue: { status: 'loading' } as LoadState<BlockedPeriodsView> },
  );

  protected calendarName(calendars: readonly Calendar[], period: BlockedPeriod): string {
    if (period.calendarId === null) {
      return 'All calendars';
    }

    return calendars.find((calendar) => calendar.id === period.calendarId)?.title ?? 'A calendar';
  }

  protected block(): void {
    const value = this.form.getRawValue();
    this.created.set(null);
    this.submission.submit(
      () =>
        this.calendars.createBlockedPeriod({
          calendarId: value.calendarId || null,
          startDate: value.startDate,
          endDate: value.endDate,
          label: value.label.trim() || null,
        }),
      () => {
        this.form.reset();
        this.created.set('The days are blocked.');
        this.reload();
      },
    );
  }

  protected unblock(period: BlockedPeriod): void {
    this.actionError.set(null);
    this.calendars.deleteBlockedPeriod(period.id).subscribe({
      next: () => this.reload(),
      error: (error: unknown) => this.actionError.set(toFormErrors(error).form),
    });
  }

  private reload(): void {
    this.version.update((version) => version + 1);
  }
}
