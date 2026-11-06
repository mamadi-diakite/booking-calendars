import { Component, inject } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import {
  AuthService,
  browserTimeZone,
  FormSubmission,
  timeZoneOptions,
} from '@booking-calendars/shared/core';
import { FieldErrors } from '@booking-calendars/shared/ui';

@Component({
  selector: 'bc-register-page',
  imports: [ReactiveFormsModule, RouterLink, FieldErrors],
  template: `
    <form class="form-card" [formGroup]="form" (ngSubmit)="register()" novalidate>
      <h1 class="text-2xl font-bold tracking-tight">Create an account</h1>

      @if (submission.errors().form; as message) {
        <p class="alert-error" role="alert">{{ message }}</p>
      }

      <p class="text-sm text-slate-600">
        Owners share booking links for their calendars; anyone can book a time with them.
      </p>

      <div>
        <label class="form-label" for="displayName">Your name</label>
        <input
          id="displayName"
          class="form-input"
          formControlName="displayName"
          autocomplete="name"
        />
        <bc-field-errors [messages]="submission.messagesFor('displayName')" />
      </div>

      <div>
        <label class="form-label" for="email">Email</label>
        <input
          id="email"
          class="form-input"
          type="email"
          formControlName="email"
          autocomplete="email"
        />
        <bc-field-errors [messages]="submission.messagesFor('email')" />
      </div>

      <div>
        <label class="form-label" for="password">Password</label>
        <input
          id="password"
          class="form-input"
          type="password"
          formControlName="password"
          autocomplete="new-password"
        />
        <p class="mt-1 text-xs text-slate-500">At least 8 characters, with a letter and a digit.</p>
        <bc-field-errors [messages]="submission.messagesFor('password')" />
      </div>

      <div>
        <label class="form-label" for="confirmPassword">Confirm password</label>
        <input
          id="confirmPassword"
          class="form-input"
          type="password"
          formControlName="confirmPassword"
          autocomplete="new-password"
        />
        <bc-field-errors [messages]="submission.messagesFor('confirmPassword')" />
      </div>

      <div>
        <label class="form-label" for="timeZone">Time zone</label>
        <select id="timeZone" class="form-input" formControlName="timeZone">
          @for (zone of timeZones; track zone) {
            <option [value]="zone">{{ zone }}</option>
          }
        </select>
        <p class="mt-1 text-xs text-slate-500">Your weekly hours are read in this time zone.</p>
        <bc-field-errors [messages]="submission.messagesFor('timeZone')" />
      </div>

      <button class="button-primary" type="submit" [disabled]="submission.pending()">
        Create my account
      </button>

      <p class="text-center text-sm text-slate-600">
        Already have an account?
        <a routerLink="/login" class="font-medium text-teal-600">Log in</a>
      </p>
    </form>
  `,
})
export class RegisterPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly form = inject(NonNullableFormBuilder).group({
    displayName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
    confirmPassword: ['', Validators.required],
    timeZone: [browserTimeZone(), Validators.required],
  });
  protected readonly timeZones = timeZoneOptions(this.form.controls.timeZone.value);
  protected readonly submission = new FormSubmission(this.form, {
    displayName: 'Enter your name.',
    email: 'Enter a valid email address.',
    password: 'Choose a password.',
    confirmPassword: 'Confirm your password.',
    timeZone: 'Choose a time zone.',
  });

  protected register(): void {
    this.submission.submit(
      () => this.auth.register(this.form.getRawValue()),
      () => void this.router.navigateByUrl('/'),
    );
  }
}
