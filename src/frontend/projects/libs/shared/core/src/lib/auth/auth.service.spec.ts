import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideAppEnvironment } from '@booking-calendars/shared/environment';
import { firstValueFrom } from 'rxjs';
import { LoginResponse } from './auth.models';
import { AuthService } from './auth.service';
import { SessionStore } from './session.store';

const response: LoginResponse = {
  accessToken: 'token-1',
  expiresAtUtc: '2999-01-01T00:00:00Z',
  user: {
    id: 'user-1',
    email: 'lena.fischer@example.com',
    displayName: 'Lena Fischer',
    timeZone: 'Europe/Paris',
  },
};

describe('AuthService', () => {
  let service: AuthService;
  let session: SessionStore;
  let http: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideAppEnvironment({ apiBaseUrl: '/api' }),
      ],
    });
    service = TestBed.inject(AuthService);
    session = TestBed.inject(SessionStore);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('logs in and starts the session', async () => {
    const user = firstValueFrom(
      service.login({ email: 'lena.fischer@example.com', password: 'x' }),
    );

    const request = http.expectOne('/api/auth/login');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ email: 'lena.fischer@example.com', password: 'x' });
    request.flush(response);

    expect(await user).toEqual(response.user);
    expect(session.token()).toBe('token-1');
  });

  it('registers and starts the session', async () => {
    const body = {
      displayName: 'Lena',
      email: 'a@example.com',
      password: 'p',
      confirmPassword: 'p',
      timeZone: 'Europe/Paris',
    };
    const user = firstValueFrom(service.register(body));

    const request = http.expectOne('/api/auth/register');
    expect(request.request.body).toEqual(body);
    request.flush(response, { status: 201, statusText: 'Created' });

    expect(await user).toEqual(response.user);
    expect(session.user()?.displayName).toBe('Lena Fischer');
  });

  it('does not start a session when login fails', async () => {
    const user = firstValueFrom(service.login({ email: 'a@example.com', password: 'x' }));

    http.expectOne('/api/auth/login').flush(null, { status: 401, statusText: 'Unauthorized' });

    await expect(user).rejects.toBeTruthy();
    expect(session.token()).toBeNull();
  });

  it('changes the password', async () => {
    const body = { currentPassword: 'a', newPassword: 'b', confirmPassword: 'b' };
    const done = firstValueFrom(service.changePassword(body), { defaultValue: undefined });

    const request = http.expectOne('/api/auth/change-password');
    expect(request.request.body).toEqual(body);
    request.flush(null, { status: 204, statusText: 'No Content' });

    await expect(done).resolves.toBeNull();
  });

  it('changes the time zone and keeps the new user in the session', async () => {
    session.start(response);
    const updated = { ...response.user, timeZone: 'America/New_York' };
    const user = firstValueFrom(service.changeTimeZone({ timeZone: 'America/New_York' }));

    const request = http.expectOne('/api/auth/time-zone');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ timeZone: 'America/New_York' });
    request.flush(updated);

    expect(await user).toEqual(updated);
    expect(session.user()?.timeZone).toBe('America/New_York');
  });

  it('logs out by clearing the session', () => {
    session.start(response);

    service.logout();

    expect(session.token()).toBeNull();
  });
});
