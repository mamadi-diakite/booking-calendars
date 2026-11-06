import { Routes } from '@angular/router';
import { authGuard, guestGuard } from '@booking-calendars/shared/core';
import { ChangePasswordPage } from './auth/change-password-page';
import { LoginPage } from './auth/login-page';
import { RegisterPage } from './auth/register-page';
import { TimeZonePage } from './auth/time-zone-page';
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
  { path: '**', redirectTo: '' },
];
