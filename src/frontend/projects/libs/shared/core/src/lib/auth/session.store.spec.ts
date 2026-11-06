import { TestBed } from '@angular/core/testing';
import { LoginResponse } from './auth.models';
import { SESSION_STORAGE_KEY, SessionStore } from './session.store';

const now = new Date('2026-11-06T10:00:00Z');

function session(expiresAtUtc = '2026-11-06T11:00:00Z'): LoginResponse {
  return {
    accessToken: 'token-1',
    expiresAtUtc,
    user: {
      id: 'user-1',
      email: 'lena.fischer@example.com',
      displayName: 'Lena Fischer',
      timeZone: 'Europe/Paris',
    },
  };
}

describe('SessionStore', () => {
  beforeEach(() => {
    vi.useFakeTimers({ now, toFake: ['Date'] });
    localStorage.clear();
  });

  afterEach(() => vi.useRealTimers());

  it('starts with nobody signed in', () => {
    const store = TestBed.inject(SessionStore);

    expect(store.token()).toBeNull();
    expect(store.user()).toBeNull();
  });

  it('keeps a started session in local storage until it is cleared', () => {
    const store = TestBed.inject(SessionStore);

    store.start(session());

    expect(store.token()).toBe('token-1');
    expect(store.user()?.displayName).toBe('Lena Fischer');
    expect(JSON.parse(localStorage.getItem(SESSION_STORAGE_KEY)!)).toEqual(session());

    store.clear();

    expect(store.token()).toBeNull();
    expect(localStorage.getItem(SESSION_STORAGE_KEY)).toBeNull();
  });

  it('replaces the user of the current session only', () => {
    const store = TestBed.inject(SessionStore);
    const user = { ...session().user, timeZone: 'Asia/Tokyo' };

    store.updateUser(user);
    expect(store.user()).toBeNull();

    store.start(session());
    store.updateUser(user);

    expect(store.token()).toBe('token-1');
    expect(store.user()?.timeZone).toBe('Asia/Tokyo');
    expect(JSON.parse(localStorage.getItem(SESSION_STORAGE_KEY)!).user.timeZone).toBe('Asia/Tokyo');
  });

  it('restores a stored session that has not expired', () => {
    localStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(session()));

    expect(TestBed.inject(SessionStore).user()?.email).toBe('lena.fischer@example.com');
  });

  it.each([
    ['an expired session', JSON.stringify(session('2026-11-06T09:59:59Z'))],
    ['a session without token', JSON.stringify({ ...session(), accessToken: '' })],
    ['text that is not JSON', '{oops'],
  ])('ignores %s in storage', (_case, stored) => {
    localStorage.setItem(SESSION_STORAGE_KEY, stored);

    expect(TestBed.inject(SessionStore).token()).toBeNull();
  });

  it('ends the session once it expires', () => {
    const store = TestBed.inject(SessionStore);
    store.start(session('2026-11-06T10:30:00Z'));

    vi.setSystemTime(new Date('2026-11-06T10:30:00Z'));

    expect(store.token()).toBeNull();
    expect(store.user()).toBeNull();
    expect(localStorage.getItem(SESSION_STORAGE_KEY)).toBeNull();
  });
});
