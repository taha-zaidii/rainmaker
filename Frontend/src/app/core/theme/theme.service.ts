import { DOCUMENT } from '@angular/common';
import { Injectable, effect, inject, signal } from '@angular/core';

export type ThemePreference = 'light' | 'dark' | 'system';

const STORAGE_KEY = 'rm-theme';

/**
 * Light / dark theming.
 *
 * Three states, not two. "System" is the default and it is a genuinely
 * different mode from light or dark: it keeps following the OS after the user
 * changes it at lunchtime, which a sticky light/dark choice would not. People
 * who want to override still can, and that override persists.
 *
 * The class goes on <html> so the first paint is already correct — toggling it
 * lower down produces a flash of the wrong theme on every navigation.
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);
  private readonly media = this.document.defaultView?.matchMedia(
    '(prefers-color-scheme: dark)',
  );

  readonly preference = signal<ThemePreference>(this.readStoredPreference());

  /** What is actually on screen right now, after resolving "system". */
  readonly isDark = signal(false);

  constructor() {
    this.apply();

    // Keep following the OS while the preference is "system".
    this.media?.addEventListener('change', () => {
      if (this.preference() === 'system') {
        this.apply();
      }
    });

    effect(() => {
      const value = this.preference();
      try {
        this.document.defaultView?.localStorage.setItem(STORAGE_KEY, value);
      } catch {
        // Private browsing or a blocked store — the theme still works for
        // this session, it just will not be remembered.
      }
      this.apply();
    });
  }

  /** What the next click will switch to — surfaced in the button tooltip so
   *  a three-state control is not something you have to discover by clicking. */
  nextLabel(): ThemePreference {
    return { light: 'dark', dark: 'system', system: 'light' }[
      this.preference()
    ] as ThemePreference;
  }

  /** Cycles light → dark → system, which is the whole control in one button. */
  toggle(): void {
    const next: Record<ThemePreference, ThemePreference> = {
      light: 'dark',
      dark: 'system',
      system: 'light',
    };
    this.preference.set(next[this.preference()]);
  }

  set(value: ThemePreference): void {
    this.preference.set(value);
  }

  private apply(): void {
    const preference = this.preference();
    const dark =
      preference === 'dark' || (preference === 'system' && !!this.media?.matches);

    this.document.documentElement.classList.toggle('dark', dark);
    this.document.documentElement.style.colorScheme = dark ? 'dark' : 'light';
    this.isDark.set(dark);
  }

  private readStoredPreference(): ThemePreference {
    try {
      const stored = this.document.defaultView?.localStorage.getItem(STORAGE_KEY);
      if (stored === 'light' || stored === 'dark' || stored === 'system') {
        return stored;
      }
    } catch {
      // Ignore — fall through to the default.
    }
    return 'system';
  }
}
