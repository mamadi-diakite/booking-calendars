import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ApiStatus } from '@booking-calendars/shared/core';
import { Shell } from './shell';

@Component({
  imports: [Shell],
  template: `<bc-shell appName="Booking Calendars" [apiStatus]="status()"
    ><p>Page content</p></bc-shell
  >`,
})
class Host {
  readonly status = signal<ApiStatus>('checking');
}

describe('Shell', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
  });

  it('shows the app name linking home, and the projected content', async () => {
    const fixture = TestBed.createComponent(Host);
    await fixture.whenStable();
    const element = fixture.nativeElement as HTMLElement;

    const home = element.querySelector('header a');
    expect(home?.textContent?.trim()).toBe('Booking Calendars');
    expect(home?.getAttribute('href')).toBe('/');
    expect(element.querySelector('main p')?.textContent).toBe('Page content');
  });

  it.each([
    ['checking', 'API checking…', 'bg-slate-400'],
    ['online', 'API online', 'bg-emerald-500'],
    ['offline', 'API offline', 'bg-rose-500'],
  ] as const)('shows the %s API status', async (status, label, dotClass) => {
    const fixture = TestBed.createComponent(Host);
    fixture.componentInstance.status.set(status);
    await fixture.whenStable();
    const element = fixture.nativeElement as HTMLElement;

    expect(element.querySelector('[data-testid="api-status"]')?.textContent).toBe(label);
    expect(element.querySelector('footer [aria-hidden="true"]')?.classList).toContain(dotClass);
  });
});
