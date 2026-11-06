import { Component } from '@angular/core';

@Component({
  selector: 'bc-home-page',
  template: `
    <section class="max-w-2xl space-y-4">
      <h1 class="text-3xl font-bold tracking-tight">Let people book a time with you</h1>
      <p class="text-lg text-slate-600">
        Share a booking link for each kind of meeting, choose your meeting lengths and weekly hours,
        and keep buffers and days off out of reach.
      </p>
    </section>
  `,
})
export class HomePage {}
