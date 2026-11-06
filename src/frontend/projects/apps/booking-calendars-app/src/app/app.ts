import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterOutlet } from '@angular/router';
import { ApiHealthService } from '@booking-calendars/shared/core';
import { Shell } from '@booking-calendars/shared/ui';

@Component({
  selector: 'bc-root',
  imports: [RouterOutlet, Shell],
  template: `
    <bc-shell appName="Booking Calendars" [apiStatus]="apiStatus()">
      <router-outlet />
    </bc-shell>
  `,
})
export class App {
  protected readonly apiStatus = toSignal(inject(ApiHealthService).check(), {
    initialValue: 'checking',
  });
}
