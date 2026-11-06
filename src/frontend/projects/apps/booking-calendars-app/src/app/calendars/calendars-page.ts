import { DOCUMENT } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import {
  Calendar,
  CalendarsService,
  LoadState,
  meetingLengthLabel,
  toFormErrors,
  withLoadState,
} from '@booking-calendars/shared/core';
import { switchMap } from 'rxjs';

/** The owner's calendars with their public links. */
@Component({
  selector: 'bc-calendars-page',
  imports: [RouterLink],
  template: `
    <section class="space-y-6">
      <div class="flex flex-wrap items-center justify-between gap-4">
        <h1 class="text-2xl font-bold tracking-tight">Calendars</h1>
        <div class="flex flex-wrap gap-3">
          <a routerLink="/blocked-periods" class="button-secondary">Blocked periods</a>
          <a routerLink="/calendars/new" class="button-primary w-auto">New calendar</a>
        </div>
      </div>

      @if (notice(); as message) {
        <p class="alert-success" role="status">{{ message }}</p>
      }
      @if (actionError(); as message) {
        <p class="alert-error" role="alert">{{ message }}</p>
      }

      @let current = state();
      @if (current.status === 'loading') {
        <p class="text-slate-600" role="status">Loading your calendars…</p>
      } @else if (current.status === 'ready') {
        <ul class="space-y-3" data-testid="calendars">
          @for (calendar of current.value; track calendar.id) {
            <li class="panel">
              <div class="flex flex-wrap items-start justify-between gap-4">
                <div class="space-y-1">
                  <p class="text-lg font-semibold" data-testid="title">{{ calendar.title }}</p>
                  <p class="text-sm text-slate-600" data-testid="lengths">
                    {{ lengths(calendar) }}
                  </p>
                  <p class="text-sm">
                    @if (calendar.isPaused) {
                      <span
                        class="rounded-full bg-amber-50 px-2 py-0.5 text-xs font-medium text-amber-800"
                        data-testid="status"
                        >Paused: takes no bookings</span
                      >
                    } @else {
                      <span
                        class="rounded-full bg-emerald-50 px-2 py-0.5 text-xs font-medium text-emerald-800"
                        data-testid="status"
                        >Taking bookings</span
                      >
                    }
                  </p>
                  <p class="text-sm break-all text-slate-500" data-testid="link">
                    {{ publicUrl(calendar) }}
                  </p>
                </div>
                <div class="flex flex-wrap gap-3 text-sm">
                  <button
                    type="button"
                    class="link"
                    [attr.aria-label]="'Copy the link of ' + calendar.title"
                    (click)="copy(calendar)"
                  >
                    Copy link
                  </button>
                  <a class="link" [routerLink]="['/calendars', calendar.id]">Edit</a>
                  <button
                    type="button"
                    class="link"
                    [attr.aria-label]="(calendar.isPaused ? 'Resume ' : 'Pause ') + calendar.title"
                    (click)="togglePause(calendar)"
                  >
                    {{ calendar.isPaused ? 'Resume' : 'Pause' }}
                  </button>
                  <button
                    type="button"
                    class="font-medium text-rose-700 hover:underline"
                    [attr.aria-label]="'Delete ' + calendar.title"
                    (click)="remove(calendar)"
                  >
                    Delete
                  </button>
                </div>
              </div>
            </li>
          } @empty {
            <li class="text-slate-600">No calendars yet. Create your first one.</li>
          }
        </ul>
      } @else {
        <p class="alert-error" role="alert">
          Your calendars could not be loaded. Please try again later.
        </p>
      }
    </section>
  `,
})
export class CalendarsPage {
  private readonly calendars = inject(CalendarsService);
  private readonly origin = inject(DOCUMENT).location.origin;
  private readonly version = signal(0);

  protected readonly notice = signal<string | null>(null);
  protected readonly actionError = signal<string | null>(null);
  protected readonly state = toSignal(
    toObservable(this.version).pipe(switchMap(() => withLoadState(this.calendars.list()))),
    { initialValue: { status: 'loading' } as LoadState<Calendar[]> },
  );

  protected lengths(calendar: Calendar): string {
    return calendar.meetingLengths.map((length) => meetingLengthLabel(length.minutes)).join(' · ');
  }

  protected publicUrl(calendar: Calendar): string {
    return `${this.origin}${calendar.publicPath}`;
  }

  protected copy(calendar: Calendar): void {
    const url = this.publicUrl(calendar);
    this.clearMessages();
    Promise.resolve()
      .then(() => navigator.clipboard.writeText(url))
      .then(
        () => this.notice.set(`Link copied: ${url}`),
        () => this.actionError.set('The link could not be copied. Select it on the page instead.'),
      );
  }

  protected togglePause(calendar: Calendar): void {
    this.clearMessages();
    this.calendars.setPaused(calendar.id, !calendar.isPaused).subscribe({
      next: (updated) => {
        this.notice.set(
          updated.isPaused
            ? `"${updated.title}" is paused and takes no bookings.`
            : `"${updated.title}" takes bookings again.`,
        );
        this.reload();
      },
      error: (error: unknown) => this.actionError.set(toFormErrors(error).form),
    });
  }

  protected remove(calendar: Calendar): void {
    this.clearMessages();
    if (!confirm(`Delete "${calendar.title}" with its weekly hours and blocked periods?`)) {
      return;
    }

    this.calendars.delete(calendar.id).subscribe({
      next: () => {
        this.notice.set(`"${calendar.title}" was deleted.`);
        this.reload();
      },
      error: (error: unknown) => this.actionError.set(toFormErrors(error).form),
    });
  }

  private reload(): void {
    this.version.update((version) => version + 1);
  }

  private clearMessages(): void {
    this.notice.set(null);
    this.actionError.set(null);
  }
}
