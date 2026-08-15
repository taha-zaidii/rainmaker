import { DOCUMENT } from '@angular/common';
import { Injectable, computed, effect, inject, signal } from '@angular/core';

export type Density = 'comfortable' | 'compact';
export type FontScale = 'small' | 'default' | 'large';
export type SidebarMode = 'expanded' | 'collapsed';
/** 'sidebar' is the current vertical rail + panel. 'horizontal' replaces
 *  both with a pair of detached floating bars at the top of the viewport —
 *  the module switcher and the module's own nav, each its own glass pill —
 *  and gives the content the full width. Common in modern enterprise
 *  shells (SAP Fiori, Oracle Fusion, Salesforce Lightning all offer this
 *  as an alternative to a permanent side rail). */
export type NavLayout = 'sidebar' | 'horizontal';

export interface BrandSwatch {
  name: string;
  hex: string;
}

/** Curated so a tenant always picks an accessible, on-brand blue-family or
 *  distinct hue rather than something that breaks contrast against white
 *  surfaces and status colours. A free hex input still exists for anyone
 *  who needs an exact brand match. */
export const BRAND_SWATCHES: BrandSwatch[] = [
  { name: 'Rainmaker Blue', hex: '#1a56db' },
  { name: 'Slate', hex: '#334155' },
  { name: 'Emerald', hex: '#0f9d58' },
  { name: 'Violet', hex: '#6d28d9' },
  { name: 'Teal', hex: '#0f766e' },
  { name: 'Crimson', hex: '#be123c' },
];

const STORAGE_PREFIX = 'rm-appearance';
const FONT_SCALE_RATIO: Record<FontScale, number> = {
  small: 0.9375,
  default: 1,
  large: 1.125,
};

interface AppearanceState {
  density: Density;
  fontScale: FontScale;
  sidebarMode: SidebarMode;
  navLayout: NavLayout;
  brandColor: string;
}

const DEFAULTS: AppearanceState = {
  density: 'comfortable',
  fontScale: 'default',
  sidebarMode: 'expanded',
  navLayout: 'sidebar',
  brandColor: BRAND_SWATCHES[0].hex,
};

/**
 * Per-tenant appearance: density, type scale, sidebar layout and brand
 * colour. Deliberately separate from ThemeService (light/dark/system) —
 * that one thing is already a complete, well-tested responsibility, and
 * bolting four more concerns onto it would make either harder to change
 * without touching the other.
 *
 * Persisted to localStorage keyed by company, matching how the AI settings
 * screen scopes its own preferences per tenant. This is a client-side
 * foundation: a real deployment would move this to the same per-company
 * settings row the backend already keeps for AI configuration, but that is
 * backend work outside this pass — see System Setup's own note on this.
 */
@Injectable({ providedIn: 'root' })
export class AppearanceService {
  private readonly document = inject(DOCUMENT);
  private readonly root = this.document.documentElement;
  private readonly storageKey = `${STORAGE_PREFIX}:${this.readCompanyIdForKey()}`;

  private readonly state = signal<AppearanceState>(this.readStored());

  readonly density = computed(() => this.state().density);
  readonly fontScale = computed(() => this.state().fontScale);
  readonly sidebarMode = computed(() => this.state().sidebarMode);
  readonly navLayout = computed(() => this.state().navLayout);
  readonly brandColor = computed(() => this.state().brandColor);

  readonly isDirtyFromDefaults = computed(() => {
    const s = this.state();
    return (
      s.density !== DEFAULTS.density ||
      s.fontScale !== DEFAULTS.fontScale ||
      s.sidebarMode !== DEFAULTS.sidebarMode ||
      s.navLayout !== DEFAULTS.navLayout ||
      s.brandColor !== DEFAULTS.brandColor
    );
  });

  constructor() {
    effect(() => {
      const s = this.state();
      this.applyDensity(s.density);
      this.applyFontScale(s.fontScale);
      this.applyBrandColor(s.brandColor);
      try {
        this.document.defaultView?.localStorage.setItem(this.storageKey, JSON.stringify(s));
      } catch {
        // Private browsing or a blocked store — still works this session.
      }
    });
  }

  setDensity(value: Density): void {
    this.withViewTransition(() => this.state.update((s) => ({ ...s, density: value })));
  }

  setFontScale(value: FontScale): void {
    this.withViewTransition(() => this.state.update((s) => ({ ...s, fontScale: value })));
  }

  setSidebarMode(value: SidebarMode): void {
    this.withViewTransition(() => this.state.update((s) => ({ ...s, sidebarMode: value })));
  }

  toggleSidebarMode(): void {
    this.setSidebarMode(this.sidebarMode() === 'expanded' ? 'collapsed' : 'expanded');
  }

  /** Swapping the whole shell structure (rail+panel vs. two floating bars)
   *  is a bigger jump than any other appearance change, so it is the one
   *  most worth cross-fading rather than snapping — see withViewTransition. */
  setNavLayout(value: NavLayout): void {
    this.withViewTransition(() => this.state.update((s) => ({ ...s, navLayout: value })));
  }

  setBrandColor(hex: string): void {
    this.state.update((s) => ({ ...s, brandColor: hex }));
  }

  resetToDefaults(): void {
    this.withViewTransition(() => this.state.set({ ...DEFAULTS }));
  }

  /**
   * Runs a state change inside the browser's View Transition API when it is
   * available, so Angular's resulting DOM swap (a different shell template,
   * not just a class toggle) cross-fades instead of popping. Every appearance
   * change is written to go through here for the same reason a design system
   * has one card component rather than five near-identical ones: consistency
   * comes from a single mechanism, not from remembering to add motion every
   * time. Falls back to an instant change on browsers without support
   * (Firefox, older Safari) — never a hard dependency, just a nicety when
   * the platform offers it.
   *
   * The callback returns a promise that resolves two frames after the state
   * write, not immediately: a signal write does not repaint synchronously,
   * and the View Transition API snapshots "after" the instant the callback
   * resolves. Resolving too early would capture the old frame twice and
   * transition to nothing.
   */
  private withViewTransition(apply: () => void): void {
    const win = this.document.defaultView as (Window & { startViewTransition?: (cb: () => void | Promise<void>) => unknown }) | null | undefined;

    if (typeof win?.startViewTransition !== 'function') {
      apply();
      return;
    }

    win.startViewTransition(
      () =>
        new Promise<void>((resolve) => {
          apply();
          win!.requestAnimationFrame(() => win!.requestAnimationFrame(() => resolve()));
        }),
    );
  }

  private applyDensity(density: Density): void {
    this.root.classList.toggle('density-compact', density === 'compact');
  }

  private applyFontScale(scale: FontScale): void {
    this.root.style.setProperty('--font-scale', String(FONT_SCALE_RATIO[scale]));
  }

  /** Derives hover/tint/deep shades from one picked hex so a tenant only
   *  ever chooses a single brand colour, never five. */
  private applyBrandColor(hex: string): void {
    const { h, s, l } = hexToHsl(hex);
    this.root.style.setProperty('--color-primary', hex);
    this.root.style.setProperty('--color-primary-hover', hslToHex(h, s, Math.max(l - 12, 8)));
    this.root.style.setProperty('--color-primary-deep', hslToHex(h, s, Math.max(l - 20, 5)));
    this.root.style.setProperty('--color-primary-tint', hslToHex(h, Math.min(s, 60), 96));
    this.root.style.setProperty('--color-primary-tint-strong', hslToHex(h, Math.min(s, 65), 90));
  }

  private readCompanyIdForKey(): string {
    try {
      const raw = this.document.defaultView?.localStorage.getItem('rm-theme-company-hint');
      return raw ?? 'default';
    } catch {
      return 'default';
    }
  }

  private readStored(): AppearanceState {
    try {
      const raw = this.document.defaultView?.localStorage.getItem(this.storageKey);
      if (!raw) return { ...DEFAULTS };
      const parsed = JSON.parse(raw) as Partial<AppearanceState>;
      return { ...DEFAULTS, ...parsed };
    } catch {
      return { ...DEFAULTS };
    }
  }
}

function hexToHsl(hex: string): { h: number; s: number; l: number } {
  const clean = hex.replace('#', '');
  const bigint = parseInt(
    clean.length === 3
      ? clean
          .split('')
          .map((c) => c + c)
          .join('')
      : clean,
    16,
  );
  const r = ((bigint >> 16) & 255) / 255;
  const g = ((bigint >> 8) & 255) / 255;
  const b = (bigint & 255) / 255;

  const max = Math.max(r, g, b);
  const min = Math.min(r, g, b);
  let h = 0;
  let s = 0;
  const l = (max + min) / 2;

  if (max !== min) {
    const d = max - min;
    s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
    switch (max) {
      case r:
        h = (g - b) / d + (g < b ? 6 : 0);
        break;
      case g:
        h = (b - r) / d + 2;
        break;
      default:
        h = (r - g) / d + 4;
    }
    h /= 6;
  }

  return { h: h * 360, s: s * 100, l: l * 100 };
}

function hslToHex(h: number, s: number, l: number): string {
  const sNorm = s / 100;
  const lNorm = l / 100;
  const c = (1 - Math.abs(2 * lNorm - 1)) * sNorm;
  const x = c * (1 - Math.abs(((h / 60) % 2) - 1));
  const m = lNorm - c / 2;

  let [r, g, b] = [0, 0, 0];
  if (h < 60) [r, g, b] = [c, x, 0];
  else if (h < 120) [r, g, b] = [x, c, 0];
  else if (h < 180) [r, g, b] = [0, c, x];
  else if (h < 240) [r, g, b] = [0, x, c];
  else if (h < 300) [r, g, b] = [x, 0, c];
  else [r, g, b] = [c, 0, x];

  const toHex = (v: number) =>
    Math.round((v + m) * 255)
      .toString(16)
      .padStart(2, '0');

  return `#${toHex(r)}${toHex(g)}${toHex(b)}`;
}
