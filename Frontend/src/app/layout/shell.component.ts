import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { filter, map } from 'rxjs';

import { IconComponent, IconName } from '../shared/icon.component';
import { ThemeService } from '../core/theme/theme.service';
import { AppearanceService } from '../core/theme/appearance.service';
import { SessionService } from '../core/auth/session.service';

interface RailItem {
  icon: IconName;
  label: string;
  /** Only Recruitment and System Administration are built. The rest are
   *  shown so the shell reads as the real ERP it lives inside, but they are
   *  visibly not-yet-available rather than dead links that fail silently. */
  route?: string;
}

interface ModuleNavItem {
  icon: IconName;
  label: string;
  route: string;
}

interface ModuleChrome {
  title: string;
  subtitle: string;
  nav: ModuleNavItem[];
}

/** One entry per top-level route segment the shell can host. Keyed by the
 *  first URL segment so a new module only ever means one new entry here. */
const MODULE_CHROME: Record<string, ModuleChrome> = {
  recruitment: {
    title: 'Recruitment',
    subtitle: 'Management',
    nav: [
      { icon: 'dashboard', label: 'Dashboard', route: '/recruitment/dashboard' },
      { icon: 'plus-circle', label: 'Create Requisition', route: '/recruitment/job-create' },
      { icon: 'list', label: 'Job Requisitions', route: '/recruitment/jobs' },
      { icon: 'user-plus', label: 'Applications', route: '/recruitment/applications' },
      { icon: 'calendar', label: 'Interviews', route: '/recruitment/interviews' },
      { icon: 'clipboard-check', label: 'Evaluation', route: '/recruitment/evaluation' },
      { icon: 'settings', label: 'AI Settings', route: '/recruitment/ai-settings' },
    ],
  },
  admin: {
    title: 'System Admin',
    subtitle: 'Configuration',
    nav: [
      { icon: 'dashboard', label: 'Overview', route: '/admin/dashboard' },
      { icon: 'settings', label: 'System Setup', route: '/admin/system-setup' },
      { icon: 'users', label: 'Users & Roles', route: '/admin/users' },
    ],
  },
  hrms: {
    title: 'Human Resource',
    subtitle: 'Management',
    nav: [{ icon: 'dashboard', label: 'Overview', route: '/hrms/dashboard' }],
  },
};

const DEFAULT_CHROME: ModuleChrome = MODULE_CHROME['recruitment'];

@Component({
  selector: 'rm-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, IconComponent, NgTemplateOutlet],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <!-- Shared across both layouts so the icon strip, notification dot and
         theme toggle are one implementation, not two that can drift apart. -->
    <ng-template #headerActions>
      <button class="p-2 rounded-full hover:bg-surface-muted transition-colors duration-[var(--duration-fast)]" title="Home">
        <rm-icon name="home" />
      </button>
      <button class="p-2 rounded-full hover:bg-surface-muted transition-colors duration-[var(--duration-fast)]" title="Tasks">
        <rm-icon name="clipboard-check" />
      </button>
      <button
        class="p-2 rounded-full hover:bg-surface-muted transition-colors duration-[var(--duration-fast)] relative"
        title="Notifications"
      >
        <rm-icon name="bell" />
        <!-- No permanent dot. It appears only when there is genuinely
             something to see — a badge that is always lit teaches people
             to ignore it. -->
        @if (session.isExpired()) {
          <span class="absolute top-1.5 right-1.5 w-2 h-2 rounded-full bg-danger ring-2 ring-surface"></span>
        }
      </button>
      <a
        routerLink="/admin/system-setup"
        class="p-2 rounded-full hover:bg-surface-muted transition-colors duration-[var(--duration-fast)]"
        title="System Setup — theme, layout and appearance"
      >
        <rm-icon name="settings" />
      </a>
      <button
        class="p-2 rounded-full hover:bg-surface-muted transition-colors duration-[var(--duration-fast)]"
        (click)="theme.toggle()"
        [title]="'Theme: ' + theme.preference() + ' (click for ' + theme.nextLabel() + ')'"
      >
        <rm-icon [name]="theme.preference() === 'system' ? 'monitor' : theme.isDark() ? 'moon' : 'sun'" />
      </button>
    </ng-template>

    <ng-template #userInfo>
      <div class="text-right leading-tight hidden lg:block">
        <div class="text-sm font-medium text-ink">{{ session.displayName() }}</div>
        <div class="text-11 text-ink-muted">Company {{ session.companyId() }}</div>
      </div>
      <div class="w-9 h-9 rounded-full bg-primary-tint text-primary grid place-items-center text-sm font-semibold shrink-0">
        {{ session.initials() }}
      </div>
    </ng-template>

    @if (appearance.navLayout() === 'horizontal') {
      <!-- ══════════════════════════════════════════════════════════════════
           Horizontal, detached — two floating glass bars instead of a rail.
           Both live in one sticky wrapper so they move as a single unit and
           neither needs a hand-computed top offset from the other. ═══════ -->
      <div class="min-h-screen flex flex-col">
        <div class="sticky top-0 z-20 px-4 pt-4 pb-2 flex flex-col gap-2">
          <!-- Bar 1 — brand, module switcher, global actions -->
          <div class="rm-card !rounded-full shadow-md flex items-center gap-2 pl-3 pr-2 h-16 shrink-0">
            <a routerLink="/recruitment" class="shrink-0 pl-1 pr-2" aria-label="Rainmaker home">
              <img src="assets/logo-rainmaker.png" alt="Rainmaker" width="120" height="23" class="h-[23px] w-auto" />
            </a>
            <div class="h-7 w-px bg-line shrink-0"></div>
            <nav class="flex items-center gap-1 overflow-x-auto rm-scroll-thin min-w-0" aria-label="Modules">
              @for (item of rail; track item.label) {
                <a
                  [routerLink]="item.route ?? null"
                  [class.pointer-events-none]="!item.route"
                  [class.opacity-40]="!item.route"
                  routerLinkActive="bg-primary text-white shadow-sm"
                  class="shrink-0 flex items-center gap-2 h-10 px-3.5 rounded-full text-sm font-medium text-ink-soft hover:bg-surface-muted transition-colors duration-[var(--duration-fast)]"
                  [attr.title]="item.route ? item.label : item.label + ' — not part of this module'"
                >
                  <rm-icon [name]="item.icon" [size]="17" />
                  <span class="hidden xl:inline">{{ item.label }}</span>
                </a>
              }
            </nav>
            <div class="flex-1"></div>
            <div class="flex items-center gap-1 text-ink-muted shrink-0">
              <ng-container [ngTemplateOutlet]="headerActions" />
            </div>
            <div class="h-7 w-px bg-line mx-1 shrink-0"></div>
            <div class="flex items-center gap-2.5 shrink-0">
              <ng-container [ngTemplateOutlet]="userInfo" />
            </div>
          </div>

          <!-- Bar 2 — this module's own navigation -->
          <div class="rm-card !rounded-full shadow-sm flex items-center gap-1 px-3 h-12 shrink-0 overflow-x-auto rm-scroll-thin">
            <span class="text-13 font-semibold text-ink-muted uppercase tracking-[0.06em] px-2 shrink-0">
              {{ chrome().title }}
            </span>
            <div class="h-5 w-px bg-line shrink-0 mr-1"></div>
            @for (item of chrome().nav; track item.route) {
              <a
                [routerLink]="item.route"
                routerLinkActive="bg-primary-tint text-primary font-medium"
                class="shrink-0 flex items-center gap-2 h-9 px-3.5 rounded-full text-sm text-ink-soft hover:bg-surface-muted transition-colors duration-[var(--duration-fast)]"
              >
                <rm-icon [name]="item.icon" [size]="16" />
                <span>{{ item.label }}</span>
              </a>
            }
          </div>
        </div>

        <main class="flex-1 px-4 pb-4">
          <router-outlet />
        </main>
      </div>
    } @else {
      <!-- ══════════════════════════════════════════════════════════════════
           Sidebar — the original vertical rail + panel layout. ═══════════ -->
      <div class="flex h-screen overflow-hidden">
        <!-- ── Module rail ───────────────────────────────────────────────── -->
        <nav
          class="w-[88px] shrink-0 rm-glass-rail flex flex-col items-center py-3 gap-1 overflow-y-auto rm-scroll-thin"
          aria-label="Modules"
        >
          @for (item of rail; track item.label) {
            <a
              [routerLink]="item.route ?? null"
              [class.pointer-events-none]="!item.route"
              [class.opacity-45]="!item.route"
              routerLinkActive="bg-white/15"
              class="w-[72px] rounded-2xl py-2.5 flex flex-col items-center gap-1.5 text-white/90 hover:bg-white/10 transition-colors duration-[var(--duration-fast)]"
              [attr.title]="item.route ? item.label : item.label + ' — not part of this module'"
            >
              <rm-icon [name]="item.icon" [size]="22" />
              <span class="text-10 leading-tight text-center px-1">{{ item.label }}</span>
            </a>
          }
        </nav>

        <!-- ── Module navigation ─────────────────────────────────────────── -->
        <aside
          class="shrink-0 rm-glass-panel flex flex-col overflow-hidden transition-[width] duration-[var(--duration-base)] ease-[var(--ease-standard)]"
          [style.width]="appearance.sidebarMode() === 'collapsed' ? '72px' : '248px'"
          [attr.aria-label]="chrome().title"
        >
          <div class="px-5 pt-5 pb-4 flex items-center justify-between gap-2">
            @if (appearance.sidebarMode() === 'expanded') {
              <div class="min-w-0">
                <div class="text-17 font-semibold text-ink leading-6 truncate">{{ chrome().title }}</div>
                <div class="text-11 font-semibold uppercase tracking-[0.08em] text-ink-muted mt-0.5">
                  {{ chrome().subtitle }}
                </div>
              </div>
            }
            <button
              class="p-1.5 rounded-full text-ink-muted hover:bg-surface-muted hover:text-ink-soft transition-colors duration-[var(--duration-fast)] shrink-0"
              [class.mx-auto]="appearance.sidebarMode() === 'collapsed'"
              (click)="appearance.toggleSidebarMode()"
              [attr.title]="appearance.sidebarMode() === 'collapsed' ? 'Expand sidebar' : 'Collapse sidebar'"
            >
              <rm-icon [name]="appearance.sidebarMode() === 'collapsed' ? 'chevron-right' : 'chevron-left'" [size]="16" />
            </button>
          </div>

          <div class="px-3 flex flex-col gap-0.5">
            @for (item of chrome().nav; track item.route) {
              <a
                [routerLink]="item.route"
                routerLinkActive="bg-primary-tint text-primary font-medium"
                class="flex items-center gap-3 h-10 px-3 rounded-full text-sm text-ink-soft hover:bg-surface-muted transition-colors duration-[var(--duration-fast)]"
                [class.justify-center]="appearance.sidebarMode() === 'collapsed'"
                [attr.title]="appearance.sidebarMode() === 'collapsed' ? item.label : null"
              >
                <rm-icon [name]="item.icon" [size]="18" />
                @if (appearance.sidebarMode() === 'expanded') {
                  <span class="truncate">{{ item.label }}</span>
                }
              </a>
            }
          </div>

          @if (appearance.sidebarMode() === 'expanded') {
            <div class="mt-auto p-3">
              <div class="rounded-2xl bg-ai-tint border border-ai-border p-3">
                <div class="flex items-center gap-2 text-ai-deep">
                  <rm-icon name="sparkles" [size]="16" />
                  <span class="text-xs font-semibold">AI-assisted</span>
                </div>
                <p class="mt-1.5 text-11 leading-4 text-ai-deep/80">
                  Anything marked in orange was suggested by the AI. You review and
                  approve it.
                </p>
              </div>
            </div>
          }
        </aside>

        <!-- ── Workspace ─────────────────────────────────────────────────── -->
        <div class="flex-1 flex flex-col min-w-0 overflow-y-auto rm-scroll-thin">
          <header class="h-16 shrink-0 sticky top-0 z-10 rm-scroll-edge shadow-xs flex items-center gap-4 px-6">
            <a routerLink="/recruitment" class="shrink-0" aria-label="Rainmaker home">
              <img
                src="assets/logo-rainmaker.png"
                alt="Rainmaker"
                width="136"
                height="26"
                class="h-[26px] w-auto"
              />
            </a>
            <span class="text-line-strong">/</span>
            <span class="text-11 font-semibold uppercase tracking-[0.1em] text-ink-muted">
              HRMS Enterprise
            </span>

            <div class="flex-1"></div>

            <div class="flex items-center gap-1 text-ink-muted">
              <ng-container [ngTemplateOutlet]="headerActions" />
            </div>

            <div class="h-8 w-px bg-line mx-1"></div>

            <div class="flex items-center gap-2.5">
              <ng-container [ngTemplateOutlet]="userInfo" />
            </div>
          </header>

          <main class="flex-1">
            <router-outlet />
          </main>
        </div>
      </div>
    }
  `,
})
export class ShellComponent {
  protected readonly theme = inject(ThemeService);
  protected readonly appearance = inject(AppearanceService);
  protected readonly session = inject(SessionService);
  private readonly router = inject(Router);

  private readonly urlSegment = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map((e) => e.urlAfterRedirects.split('/').filter(Boolean)[0] ?? ''),
    ),
    { initialValue: this.router.url.split('/').filter(Boolean)[0] ?? '' },
  );

  /** The secondary nav's whole title/subtitle/item set, swapped by module —
   *  the shell is one component shared by every module rather than a
   *  recruitment-only shell that admin happens to also render inside. */
  protected readonly chrome = computed<ModuleChrome>(
    () => MODULE_CHROME[this.urlSegment()] ?? DEFAULT_CHROME,
  );

  protected readonly rail: RailItem[] = [
    { icon: 'dashboard', label: 'Dashboard' },
    { icon: 'settings', label: 'System Admin', route: '/admin' },
    { icon: 'users', label: 'Human Resource' },
    { icon: 'inventory', label: 'Inventory' },
    { icon: 'cart', label: 'Procurement' },
    { icon: 'chart', label: 'Reports' },
    { icon: 'briefcase', label: 'Recruitment', route: '/recruitment' },
  ];
}
