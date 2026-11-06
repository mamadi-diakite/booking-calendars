import { computed, Injectable, signal } from '@angular/core';
import { CurrentUser, LoginResponse } from './auth.models';

export const SESSION_STORAGE_KEY = 'booking-calendars.session';

/** The signed-in user and their access token, kept in local storage until it expires. */
@Injectable({ providedIn: 'root' })
export class SessionStore {
  private readonly session = signal<LoginResponse | null>(readStoredSession());

  readonly user = computed(() => this.session()?.user ?? null);

  /** The access token, or `null` when nobody is signed in or the session has expired. */
  token(): string | null {
    const session = this.session();
    if (session && isExpired(session)) {
      this.clear();
      return null;
    }

    return session?.accessToken ?? null;
  }

  start(session: LoginResponse): void {
    localStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(session));
    this.session.set(session);
  }

  /** Keeps the token and replaces the user, e.g. after a profile change. Ignored when signed out. */
  updateUser(user: CurrentUser): void {
    const session = this.session();
    if (session) {
      this.start({ ...session, user });
    }
  }

  clear(): void {
    localStorage.removeItem(SESSION_STORAGE_KEY);
    this.session.set(null);
  }
}

function isExpired(session: LoginResponse): boolean {
  return Date.parse(session.expiresAtUtc) <= Date.now();
}

function readStoredSession(): LoginResponse | null {
  try {
    const session = JSON.parse(
      localStorage.getItem(SESSION_STORAGE_KEY) ?? 'null',
    ) as LoginResponse | null;
    return session?.accessToken && !isExpired(session) ? session : null;
  } catch {
    return null;
  }
}
