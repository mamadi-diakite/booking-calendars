import { Component, inject, signal } from '@angular/core';
import {
  FormControl,
  FormGroup,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  Calendar,
  CalendarRequest,
  CalendarsService,
  FormSubmission,
  LoadState,
  MAX_BUFFER_MINUTES,
  MEETING_LENGTHS,
  meetingLengthLabel,
  SessionStore,
  WEEK_DAYS,
  WeeklyHoursRange,
  withLoadState,
} from '@booking-calendars/shared/core';
import { FieldErrors } from '@booking-calendars/shared/ui';

/** The day travels as text because select options hold strings. */
type RangeForm = FormGroup<{
  day: FormControl<string>;
  start: FormControl<string>;
  end: FormControl<string>;
}>;

/** Creates a calendar, or edits its details, meeting lengths, buffers and weekly hours. */
@Component({
  selector: 'bc-calendar-editor-page',
  imports: [ReactiveFormsModule, RouterLink, FieldErrors],
  template: `
    <section class="mx-auto max-w-3xl space-y-8">
      <a routerLink="/calendars" class="link text-sm">Back to calendars</a>

      @let current = state();
      @if (current.status === 'loading') {
        <p class="text-slate-600" role="status">Loading the calendar…</p>
      } @else if (current.status === 'missing') {
        <p class="alert-error" role="alert">This calendar does not exist or is not yours.</p>
      } @else if (current.status === 'error') {
        <p class="alert-error" role="alert">
          The calendar could not be loaded. Please try again later.
        </p>
      } @else {
        <h1 class="text-2xl font-bold tracking-tight">
          {{ calendar() ? 'Edit calendar' : 'New calendar' }}
        </h1>

        <form class="panel" [formGroup]="details" (ngSubmit)="saveDetails()" novalidate>
          <h2 class="text-lg font-semibold">Details</h2>
          @if (detailsNotice(); as message) {
            <p class="alert-success" role="status">{{ message }}</p>
          }
          @if (detailsSubmission.errors().form; as message) {
            <p class="alert-error" role="alert">{{ message }}</p>
          }

          <div>
            <label class="form-label" for="title">Title</label>
            <input id="title" class="form-input" formControlName="title" />
            <bc-field-errors [messages]="detailsSubmission.messagesFor('title')" />
          </div>

          <div>
            <label class="form-label" for="description">Description</label>
            <textarea
              id="description"
              class="form-input"
              rows="3"
              formControlName="description"
            ></textarea>
            <bc-field-errors [messages]="detailsSubmission.messagesFor('description')" />
          </div>

          <div>
            <label class="form-label" for="locationNote">Location note</label>
            <input
              id="locationNote"
              class="form-input"
              formControlName="locationNote"
              placeholder="Video call, phone, office address…"
            />
            <bc-field-errors [messages]="detailsSubmission.messagesFor('locationNote')" />
          </div>

          <div>
            <label class="form-label" for="slug">Public link</label>
            <div class="mt-1 flex items-center gap-1 text-sm text-slate-500">
              <span>/book/</span>
              <input id="slug" class="form-input mt-0" formControlName="slug" />
            </div>
            <p class="mt-1 text-xs text-slate-500">
              Leave empty to build it from the title. Lowercase letters, digits and hyphens.
            </p>
            <bc-field-errors [messages]="detailsSubmission.messagesFor('slug')" />
          </div>

          <fieldset class="space-y-3" formArrayName="lengths">
            <legend class="form-label">Meeting lengths and buffers</legend>
            <p class="text-xs text-slate-500">
              Offer one or more lengths. Buffers keep free time before and after each meeting, up to
              {{ maxBuffer }} minutes.
            </p>
            @for (length of details.controls.lengths.controls; track $index) {
              <div
                class="flex flex-wrap items-center gap-4 rounded-md border border-slate-200 p-3"
                [formGroupName]="$index"
              >
                <label class="flex w-28 items-center gap-2 font-medium">
                  <input
                    type="checkbox"
                    [id]="'length-' + length.controls.minutes.value + '-offered'"
                    formControlName="offered"
                  />
                  {{ lengthLabel(length.controls.minutes.value) }}
                </label>
                <label class="flex items-center gap-2 text-sm">
                  Before
                  <input
                    type="number"
                    class="w-20 rounded-md border border-slate-300 px-2 py-1"
                    min="0"
                    [max]="maxBuffer"
                    [id]="'length-' + length.controls.minutes.value + '-before'"
                    formControlName="bufferBeforeMinutes"
                  />
                  min
                </label>
                <label class="flex items-center gap-2 text-sm">
                  After
                  <input
                    type="number"
                    class="w-20 rounded-md border border-slate-300 px-2 py-1"
                    min="0"
                    [max]="maxBuffer"
                    [id]="'length-' + length.controls.minutes.value + '-after'"
                    formControlName="bufferAfterMinutes"
                  />
                  min
                </label>
              </div>
            }
            <bc-field-errors
              [messages]="detailsSubmission.errors().fields['meetingLengths'] ?? []"
            />
          </fieldset>

          <button
            class="button-primary sm:w-auto"
            type="submit"
            [disabled]="detailsSubmission.pending()"
          >
            {{ calendar() ? 'Save details' : 'Create calendar' }}
          </button>
        </form>

        @if (calendar()) {
          <form class="panel" [formGroup]="hours" (ngSubmit)="saveHours()" novalidate>
            <h2 class="text-lg font-semibold">Weekly hours</h2>
            <p class="text-sm text-slate-600" data-testid="time-zone">
              Times are in your time zone, {{ timeZone }}.
              <a routerLink="/account/time-zone" class="link">Change it</a>
            </p>
            @if (hoursNotice(); as message) {
              <p class="alert-success" role="status">{{ message }}</p>
            }
            @if (hoursSubmission.errors().form; as message) {
              <p class="alert-error" role="alert">{{ message }}</p>
            }

            <div class="space-y-2" formArrayName="ranges">
              @for (range of hours.controls.ranges.controls; track range) {
                <div class="flex flex-wrap items-center gap-3" [formGroupName]="$index">
                  <select
                    class="rounded-md border border-slate-300 px-2 py-1"
                    [id]="'range-' + $index + '-day'"
                    aria-label="Day"
                    formControlName="day"
                  >
                    @for (weekDay of weekDays; track weekDay.day) {
                      <option [value]="weekDay.day">{{ weekDay.name }}</option>
                    }
                  </select>
                  <input
                    type="time"
                    class="rounded-md border border-slate-300 px-2 py-1"
                    [id]="'range-' + $index + '-start'"
                    aria-label="From"
                    formControlName="start"
                  />
                  <span class="text-sm text-slate-500">to</span>
                  <input
                    type="time"
                    class="rounded-md border border-slate-300 px-2 py-1"
                    [id]="'range-' + $index + '-end'"
                    aria-label="To"
                    formControlName="end"
                  />
                  <button
                    type="button"
                    class="text-sm font-medium text-rose-700 hover:underline"
                    [id]="'range-' + $index + '-remove'"
                    (click)="removeRange($index)"
                  >
                    Remove
                  </button>
                </div>
              } @empty {
                <p class="text-sm text-slate-600">
                  No weekly hours: nobody can book this calendar.
                </p>
              }
            </div>
            <bc-field-errors [messages]="hoursSubmission.errors().fields['ranges'] ?? []" />

            <div class="flex flex-wrap gap-3">
              <button type="button" class="button-secondary" id="add-range" (click)="addRange()">
                Add a time range
              </button>
              <button
                class="button-primary sm:w-auto"
                type="submit"
                [disabled]="hoursSubmission.pending()"
              >
                Save weekly hours
              </button>
            </div>
          </form>
        }
      }
    </section>
  `,
})
export class CalendarEditorPage {
  private readonly calendars = inject(CalendarsService);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly route = inject(ActivatedRoute).snapshot;
  private readonly id = this.route.paramMap.get('id');

  protected readonly maxBuffer = MAX_BUFFER_MINUTES;
  protected readonly weekDays = WEEK_DAYS;
  protected readonly lengthLabel = meetingLengthLabel;
  protected readonly timeZone = inject(SessionStore).user()?.timeZone ?? 'UTC';
  protected readonly calendar = signal<Calendar | null>(null);
  protected readonly state = signal<LoadState<Calendar | null>>(
    this.id ? { status: 'loading' } : { status: 'ready', value: null },
  );
  protected readonly detailsNotice = signal<string | null>(
    this.route.queryParamMap.has('created') ? 'Calendar created. Now set its weekly hours.' : null,
  );
  protected readonly hoursNotice = signal<string | null>(null);

  protected readonly details = this.formBuilder.group({
    title: ['', Validators.required],
    description: [''],
    locationNote: [''],
    slug: [''],
    lengths: this.formBuilder.array(
      MEETING_LENGTHS.map((minutes) =>
        this.formBuilder.group({
          minutes: [minutes as number],
          offered: [minutes === 30],
          bufferBeforeMinutes: this.formBuilder.control<number | null>(0),
          bufferAfterMinutes: this.formBuilder.control<number | null>(0),
        }),
      ),
    ),
  });
  protected readonly detailsSubmission = new FormSubmission(this.details, {
    title: 'Enter a title.',
  });

  protected readonly hours = this.formBuilder.group({
    ranges: this.formBuilder.array<RangeForm>([]),
  });
  protected readonly hoursSubmission = new FormSubmission(this.hours, {});

  constructor() {
    if (this.id) {
      withLoadState(this.calendars.get(this.id)).subscribe((state) => {
        if (state.status === 'ready') {
          this.show(state.value);
        }
        this.state.set(state);
      });
    }
  }

  protected saveDetails(): void {
    const current = this.calendar();
    this.detailsNotice.set(null);
    this.detailsSubmission.submit(
      () =>
        current
          ? this.calendars.update(current.id, this.detailsRequest())
          : this.calendars.create(this.detailsRequest()),
      (saved) => {
        if (current) {
          this.show(saved);
          this.detailsNotice.set('Calendar saved.');
        } else {
          void this.router.navigate(['/calendars', saved.id], { queryParams: { created: true } });
        }
      },
    );
  }

  protected addRange(): void {
    this.hours.controls.ranges.push(this.rangeGroup({ day: 1, start: '09:00', end: '17:00' }));
  }

  protected removeRange(index: number): void {
    this.hours.controls.ranges.removeAt(index);
  }

  protected saveHours(): void {
    const ranges: WeeklyHoursRange[] = this.hours.controls.ranges.getRawValue().map((range) => ({
      day: Number(range.day),
      start: range.start,
      end: range.end,
    }));
    this.hoursNotice.set(null);
    this.hoursSubmission.submit(
      () => this.calendars.setWeeklyHours(this.calendar()!.id, ranges),
      (saved) => {
        this.show(saved);
        this.hoursNotice.set('Weekly hours saved.');
      },
    );
  }

  private detailsRequest(): CalendarRequest {
    const value = this.details.getRawValue();
    return {
      title: value.title,
      description: value.description || null,
      locationNote: value.locationNote || null,
      slug: value.slug || null,
      meetingLengths: value.lengths
        .filter((length) => length.offered)
        .map((length) => ({
          minutes: length.minutes,
          bufferBeforeMinutes: length.bufferBeforeMinutes ?? 0,
          bufferAfterMinutes: length.bufferAfterMinutes ?? 0,
        })),
    };
  }

  private show(calendar: Calendar): void {
    this.calendar.set(calendar);
    this.details.patchValue({
      title: calendar.title,
      description: calendar.description ?? '',
      locationNote: calendar.locationNote ?? '',
      slug: calendar.slug,
    });
    for (const length of this.details.controls.lengths.controls) {
      const offered = calendar.meetingLengths.find(
        (m) => m.minutes === length.controls.minutes.value,
      );
      length.patchValue({
        offered: offered !== undefined,
        bufferBeforeMinutes: offered?.bufferBeforeMinutes ?? 0,
        bufferAfterMinutes: offered?.bufferAfterMinutes ?? 0,
      });
    }
    this.hours.controls.ranges.clear();
    for (const range of calendar.weeklyHours) {
      this.hours.controls.ranges.push(this.rangeGroup(range));
    }
  }

  private rangeGroup(range: WeeklyHoursRange): RangeForm {
    return this.formBuilder.group({
      day: [String(range.day)],
      start: [range.start],
      end: [range.end],
    });
  }
}
