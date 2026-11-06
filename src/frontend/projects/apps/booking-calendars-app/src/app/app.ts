import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { ApiHealthService, AuthService, SessionStore } from '@booking-calendars/shared/core';
import { Shell } from '@booking-calendars/shared/ui';

@Component({
  selector: 'bc-root',
  imports: [RouterOutlet, RouterLink, Shell],
  template: `
    <bc-shell appName="Booking Calendars" [apiStatus]="apiStatus()">
      <ng-container shellNav>
        @if (user(); as user) {
          <span class="text-slate-600" data-testid="user-name">{{ user.displayName }}</span>
          <a routerLink="/calendars" class="text-teal-600">Calendars</a>
          <a routerLink="/account/time-zone" class="text-teal-600">Time zone</a>
          <a routerLink="/account/password" class="text-teal-600">Change password</a>
          <button type="button" class="text-teal-600" (click)="logOut()">Log out</button>
        } @else {
          <a routerLink="/login" class="text-teal-600">Log in</a>
          <a routerLink="/register" class="text-teal-600">Create an account</a>
        }
      </ng-container>
      <router-outlet />
    </bc-shell>
  `,
})
export class App {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly user = inject(SessionStore).user;
  protected readonly apiStatus = toSignal(inject(ApiHealthService).check(), {
    initialValue: 'checking',
  });

  protected logOut(): void {
    this.auth.logout();
    void this.router.navigateByUrl('/login');
  }
}
