import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { LoginResponse, SessionStore } from '@booking-calendars/shared/core';
import { App } from '../app/app';
import { appConfig } from '../app/app.config';

export const demoSession: LoginResponse = {
  accessToken: 'token-1',
  expiresAtUtc: '2999-01-01T00:00:00Z',
  user: {
    id: 'user-1',
    email: 'lena.fischer@example.com',
    displayName: 'Lena Fischer',
    timeZone: 'Europe/Paris',
  },
};

/** The whole app on a fake API, with helpers to drive it like a user. */
export class AppHarness {
  readonly http: HttpTestingController;
  readonly router: Router;
  readonly fixture: ComponentFixture<App>;

  constructor() {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [...appConfig.providers, provideHttpClientTesting()],
    });
    this.http = TestBed.inject(HttpTestingController);
    this.router = TestBed.inject(Router);
    this.fixture = TestBed.createComponent(App);
    this.http.expectOne('/api/health').flush({ status: 'ok' });
  }

  get element(): HTMLElement {
    return this.fixture.nativeElement as HTMLElement;
  }

  signIn(): void {
    TestBed.inject(SessionStore).start(demoSession);
  }

  async open(url: string): Promise<void> {
    await this.router.navigateByUrl(url);
    await this.fixture.whenStable();
  }

  async type(id: string, value: string): Promise<void> {
    const input = this.element.querySelector<HTMLInputElement>(`#${id}`)!;
    input.value = value;
    input.dispatchEvent(new Event('input'));
    input.dispatchEvent(new Event('blur'));
    await this.fixture.whenStable();
  }

  async select(id: string, value: string): Promise<void> {
    const select = this.element.querySelector<HTMLSelectElement>(`#${id}`)!;
    select.value = value;
    select.dispatchEvent(new Event('change'));
    await this.fixture.whenStable();
  }

  async check(id: string, checked: boolean): Promise<void> {
    const input = this.element.querySelector<HTMLInputElement>(`#${id}`)!;
    input.checked = checked;
    input.dispatchEvent(new Event('change'));
    await this.fixture.whenStable();
  }

  async click(selector: string): Promise<void> {
    this.element.querySelector<HTMLElement>(selector)!.click();
    await this.fixture.whenStable();
  }

  /** Submits the form that holds the given element, for pages with several forms. */
  async submitForm(selector: string): Promise<void> {
    this.element
      .querySelector(selector)!
      .closest('form')!
      .querySelector<HTMLButtonElement>('button[type="submit"]')!
      .click();
    await this.fixture.whenStable();
  }

  async submit(): Promise<void> {
    this.element.querySelector<HTMLButtonElement>('form button[type="submit"]')!.click();
    await this.fixture.whenStable();
  }

  async settle(): Promise<void> {
    await this.fixture.whenStable();
  }

  text(selector: string): string | undefined {
    return this.element.querySelector(selector)?.textContent?.trim();
  }

  texts(selector: string): string[] {
    return [...this.element.querySelectorAll(selector)].map((node) => node.textContent!.trim());
  }

  button(selector = 'form button[type="submit"]'): HTMLButtonElement {
    return this.element.querySelector<HTMLButtonElement>(selector)!;
  }
}
