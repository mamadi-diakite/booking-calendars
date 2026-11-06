import { Routes } from '@angular/router';
import { authGuard, guestGuard } from '@booking-calendars/shared/core';
import { ChangePasswordPage } from './auth/change-password-page';
import { LoginPage } from './auth/login-page';
import { RegisterPage } from './auth/register-page';
import { TimeZonePage } from './auth/time-zone-page';
import { BlockedPeriodsPage } from './calendars/blocked-periods-page';
import { CalendarEditorPage } from './calendars/calendar-editor-page';
import { CalendarsPage } from './calendars/calendars-page';
import { HomePage } from './home/home-page';

export const routes: Routes = [
  { path: '', component: HomePage, title: 'Booking Calendars' },
  {
    path: 'login',
    component: LoginPage,
    canActivate: [guestGuard],
    title: 'Log in · Booking Calendars',
  },
  {
    path: 'register',
    component: RegisterPage,
    canActivate: [guestGuard],
    title: 'Create an account · Booking Calendars',
  },
  {
    path: 'account/password',
    component: ChangePasswordPage,
    canActivate: [authGuard],
    title: 'Change password · Booking Calendars',
  },
  {
    path: 'account/time-zone',
    component: TimeZonePage,
    canActivate: [authGuard],
    title: 'Time zone · Booking Calendars',
  },
  {
    path: 'calendars',
    component: CalendarsPage,
    canActivate: [authGuard],
    title: 'Calendars · Booking Calendars',
  },
  {
    path: 'calendars/new',
    component: CalendarEditorPage,
    canActivate: [authGuard],
    title: 'New calendar · Booking Calendars',
  },
  {
    path: 'calendars/:id',
    component: CalendarEditorPage,
    canActivate: [authGuard],
    title: 'Edit calendar · Booking Calendars',
  },
  {
    path: 'blocked-periods',
    component: BlockedPeriodsPage,
    canActivate: [authGuard],
    title: 'Blocked periods · Booking Calendars',
  },
  { path: '**', redirectTo: '' },
];
