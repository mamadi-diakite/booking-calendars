import { TestBed } from '@angular/core/testing';
import { APP_ENVIRONMENT } from '@booking-calendars/shared/environment';
import { AppHarness } from '../testing/app-harness';

describe('App', () => {
  let app: AppHarness;

  beforeEach(() => (app = new AppHarness()));

  afterEach(() => app.http.verify());

  it('uses the API behind the same origin', () => {
    expect(TestBed.inject(APP_ENVIRONMENT).apiBaseUrl).toBe('/api');
  });

  it('shows the home page, the API status and the visitor links', async () => {
    await app.open('/');

    expect(app.text('h1')).toBe('Let people book a time with you');
    expect(app.text('[data-testid="api-status"]')).toBe('API online');
    expect(app.texts('header nav a')).toEqual(['Log in', 'Create an account']);
  });

  it('shows the signed-in user and logs out to the login page', async () => {
    app.signIn();
    await app.open('/');

    expect(app.text('[data-testid="user-name"]')).toBe('Lena Fischer');
    expect(app.texts('header nav a')).toEqual(['Calendars', 'Time zone', 'Change password']);

    app.button('header nav button').click();
    await app.settle();

    expect(app.router.url).toBe('/login');
    expect(app.texts('header nav a')).toEqual(['Log in', 'Create an account']);
  });

  it('sends unknown addresses back home', async () => {
    await app.open('/does-not-exist');

    expect(app.router.url).toBe('/');
  });
});
