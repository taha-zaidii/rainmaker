import { Injectable, computed, signal } from '@angular/core';

import { environment } from '../../../environments/environment';

interface JwtClaims {
  unique_name?: string;
  UserName?: string;
  CompanyID?: string;
  exp?: number;
  [key: string]: unknown;
}

/**
 * Who is signed in, read from the bearer token rather than hardcoded.
 *
 * The shell previously displayed a fixed name and role, which is fine in a
 * mock and actively misleading in a running app — it would keep saying
 * "Sumaira Butt" no matter who was logged in. Reading the token means the
 * header is correct the moment real authentication replaces the dev token,
 * with no further change.
 */
@Injectable({ providedIn: 'root' })
export class SessionService {
  private readonly claims = signal<JwtClaims | null>(decode(environment.devAuthToken));

  readonly userName = computed(() => {
    const c = this.claims();
    return c?.UserName || c?.unique_name || 'Signed out';
  });

  /** "superadmin" → "Superadmin". Good enough until real profile data exists. */
  readonly displayName = computed(() => {
    const name = this.userName();
    return name.charAt(0).toUpperCase() + name.slice(1);
  });

  readonly initials = computed(() =>
    this.displayName()
      .split(/[\s._-]+/)
      .filter(Boolean)
      .slice(0, 2)
      .map((p) => p[0]!.toUpperCase())
      .join(''),
  );

  readonly companyId = computed(() => {
    const raw = this.claims()?.CompanyID;
    return raw ? Number(raw) : environment.companyId;
  });

  /** True when the token is past its expiry — worth surfacing before a wall
   *  of 401s makes it look like the backend is down. */
  readonly isExpired = computed(() => {
    const exp = this.claims()?.exp;
    return typeof exp === 'number' ? exp * 1000 < Date.now() : false;
  });
}

function decode(token: string): JwtClaims | null {
  try {
    const payload = token.split('.')[1];
    if (!payload) {
      return null;
    }
    const json = atob(payload.replace(/-/g, '+').replace(/_/g, '/'));
    return JSON.parse(json) as JwtClaims;
  } catch {
    // A malformed token is a configuration problem, not a crash.
    return null;
  }
}
