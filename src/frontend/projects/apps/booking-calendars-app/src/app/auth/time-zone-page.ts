import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  AuthService,
  FormSubmission,
  SessionStore,
  timeZoneOptions,
} from '@booking-calendars/shared/core';
import { FieldErrors } from '@booking-calendars/shared/ui';

@Component({
  selector: 'bc-time-zone-page',
  imports: [ReactiveFormsModule, FieldErrors],
  template: `
    <form class="form-card" [formGroup]="form" (ngSubmit)="save()" novalidate>
      <h1 class="text-2xl font-bold tracking-tight">Time zone</h1>

      @if (saved()) {
        <p class="alert-info" role="status">Your time zone has been saved.</p>
      }
      @if (submission.errors().form; as message) {
        <p class="alert-error" role="alert">{{ message }}</p>
      }

      <div>
        <label class="form-label" for="timeZone">Time zone</label>
        <select id="timeZone" class="form-input" formControlName="timeZone">
          @for (zone of timeZones; track zone) {
            <option [value]="zone">{{ zone }}</option>
          }
        </select>
        <p class="mt-1 text-xs text-slate-500">
          The weekly hours of your calendars are read in this time zone.
        </p>
        <bc-field-errors [messages]="submission.messagesFor('timeZone')" />
      </div>

      <button class="button-primary" type="submit" [disabled]="submission.pending()">
        Save time zone
      </button>
    </form>
  `,
})
export class TimeZonePage {
  private readonly auth = inject(AuthService);

  protected readonly saved = signal(false);
  protected readonly form = inject(NonNullableFormBuilder).group({
    timeZone: [inject(SessionStore).user()?.timeZone ?? 'UTC', Validators.required],
  });
  protected readonly timeZones = timeZoneOptions(this.form.controls.timeZone.value);
  protected readonly submission = new FormSubmission(this.form, {
    timeZone: 'Choose a time zone.',
  });

  protected save(): void {
    this.saved.set(false);
    this.submission.submit(
      () => this.auth.changeTimeZone(this.form.getRawValue()),
      () => this.saved.set(true),
    );
  }
}
