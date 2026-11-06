import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { authInterceptor } from './auth.interceptor';
import { SessionStore } from './session.store';

describe('authInterceptor', () => {
  let client: HttpClient;
  let http: HttpTestingController;
  let session: SessionStore;
  let router: Router;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    client = TestBed.inject(HttpClient);
    http = TestBed.inject(HttpTestingController);
    session = TestBed.inject(SessionStore);
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
  });

  afterEach(() => http.verify());

  function signIn(): void {
    session.start({
      accessToken: 'token-1',
      expiresAtUtc: '2999-01-01T00:00:00Z',
      user: {
        id: 'user-1',
        email: 'lena.fischer@example.com',
        displayName: 'Lena Fischer',
        timeZone: 'Europe/Paris',
      },
    });
  }

  it('sends requests unchanged when nobody is signed in', () => {
    client.get('/api/requests').subscribe();

    expect(http.expectOne('/api/requests').request.headers.has('Authorization')).toBe(false);
  });

  it('adds the access token of the signed-in user', () => {
    signIn();
    client.get('/api/requests').subscribe();

    expect(http.expectOne('/api/requests').request.headers.get('Authorization')).toBe(
      'Bearer token-1',
    );
  });

  it('ends the session and goes to login when the token is refused', async () => {
    signIn();
    const result = firstValueFrom(client.get('/api/requests'));

    http.expectOne('/api/requests').flush(null, { status: 401, statusText: 'Unauthorized' });

    await expect(result).rejects.toMatchObject({ status: 401 });
    expect(session.token()).toBeNull();
    expect(router.navigate).toHaveBeenCalledWith(['/login'], { queryParams: { expired: true } });
  });

  it('passes other errors through and keeps the session', async () => {
    signIn();
    const result = firstValueFrom(client.get('/api/requests'));

    http.expectOne('/api/requests').flush(null, { status: 404, statusText: 'Not Found' });

    await expect(result).rejects.toMatchObject({ status: 404 });
    expect(session.token()).toBe('token-1');
    expect(router.navigate).not.toHaveBeenCalled();
  });
});
