import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { IconComponent, IconName } from '../shared/icon.component';
import { ThemeService } from '../core/theme/theme.service';
import { SessionService } from '../core/auth/session.service';

interface RailItem {
  icon: IconName;
  label: string;
  /** Only Recruitment is built. The rest are shown so the shell reads as the
   *  real ERP it lives inside, but they are visibly not-yet-available rather
   *  than dead links that fail silently. */
  route?: string;
}

interface ModuleNavItem {
  icon: IconName;
  label: string;
  route: string;
}

@Component({
  selector: 'rm-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex h-screen overflow-hidden bg-canvas">
      <!-- ── Module rail ───────────────────────────────────────────────── -->
      <nav
        class="w-[88px] shrink-0 bg-primary flex flex-col items-center py-3 gap-1 overflow-y-auto rm-scroll-thin"
        aria-label="Modules"
      >
        @for (item of rail; track item.label) {
          <a
            [routerLink]="item.route ?? null"
            [class.pointer-events-none]="!item.route"
            [class.opacity-45]="!item.route"
            routerLinkActive="bg-white/15"
            class="w-[72px] rounded-lg py-2.5 flex flex-col items-center gap-1.5 text-white/90 hover:bg-white/10 transition-colors"
            [attr.title]="item.route ? item.label : item.label + ' — not part of this module'"
          >
            <rm-icon [name]="item.icon" [size]="22" />
            <span class="text-[10px] leading-tight text-center px-1">{{ item.label }}</span>
          </a>
        }
      </nav>

      <!-- ── Module navigation ─────────────────────────────────────────── -->
      <aside
        class="w-[248px] shrink-0 bg-surface border-r border-line flex flex-col"
        aria-label="Recruitment"
      >
        <div class="px-5 pt-5 pb-4">
          <div class="text-[17px] font-semibold text-ink leading-6">Recruitment</div>
          <div class="text-[11px] font-semibold uppercase tracking-[0.08em] text-ink-muted mt-0.5">
            Management
          </div>
        </div>

        <div class="px-3 flex flex-col gap-0.5">
          @for (item of moduleNav; track item.route) {
            <a
              [routerLink]="item.route"
              routerLinkActive="bg-primary-tint text-primary font-medium"
              class="flex items-center gap-3 h-10 px-3 rounded-lg text-sm text-ink-soft hover:bg-surface-muted transition-colors"
            >
              <rm-icon [name]="item.icon" [size]="18" />
              <span>{{ item.label }}</span>
            </a>
          }
        </div>

        <div class="mt-auto p-3">
          <div class="rounded-lg bg-ai-tint border border-ai-border p-3">
            <div class="flex items-center gap-2 text-ai-deep">
              <rm-icon name="sparkles" [size]="16" />
              <span class="text-xs font-semibold">AI-assisted</span>
            </div>
            <p class="mt-1.5 text-[11px] leading-4 text-ai-deep/80">
              Anything marked in orange was suggested by the AI. You review and
              approve it.
            </p>
          </div>
        </div>
      </aside>

      <!-- ── Workspace ─────────────────────────────────────────────────── -->
      <div class="flex-1 flex flex-col min-w-0">
        <header
          class="h-16 shrink-0 bg-surface border-b border-line flex items-center gap-4 px-6"
        >
          <a routerLink="/recruitment" class="text-[19px] font-bold text-primary tracking-tight">
            rainmaker
          </a>
          <span class="text-line-strong">/</span>
          <span class="text-[11px] font-semibold uppercase tracking-[0.1em] text-ink-muted">
            HRMS Enterprise
          </span>

          <div class="flex-1"></div>

          <div class="flex items-center gap-1 text-ink-muted">
            <button class="p-2 rounded-lg hover:bg-surface-muted transition-colors" title="Home">
              <rm-icon name="home" />
            </button>
            <button class="p-2 rounded-lg hover:bg-surface-muted transition-colors" title="Tasks">
              <rm-icon name="clipboard-check" />
            </button>
            <button
              class="p-2 rounded-lg hover:bg-surface-muted transition-colors relative"
              title="Notifications"
            >
              <rm-icon name="bell" />
              <!-- No permanent dot. It appears only when there is genuinely
                   something to see — a badge that is always lit teaches people
                   to ignore it. -->
              @if (session.isExpired()) {
                <span
                  class="absolute top-1.5 right-1.5 w-2 h-2 rounded-full bg-danger ring-2 ring-surface"
                ></span>
              }
            </button>
            <button
              class="p-2 rounded-lg hover:bg-surface-muted transition-colors"
              (click)="theme.toggle()"
              [title]="'Theme: ' + theme.preference() + ' (click for ' + theme.nextLabel() + ')'"
            >
              <rm-icon [name]="theme.preference() === 'system' ? 'monitor' : theme.isDark() ? 'moon' : 'sun'" />
            </button>
          </div>

          <div class="h-8 w-px bg-line mx-1"></div>

          <div class="flex items-center gap-2.5">
            <div class="text-right leading-tight">
              <div class="text-sm font-medium text-ink">{{ session.displayName() }}</div>
              <div class="text-[11px] text-ink-muted">Company {{ session.companyId() }}</div>
            </div>
            <div
              class="w-9 h-9 rounded-full bg-primary-tint text-primary grid place-items-center text-sm font-semibold"
            >
              {{ session.initials() }}
            </div>
          </div>
        </header>

        <main class="flex-1 overflow-y-auto rm-scroll-thin">
          <router-outlet />
        </main>
      </div>
    </div>
  `,
})
export class ShellComponent {
  protected readonly theme = inject(ThemeService);
  protected readonly session = inject(SessionService);

  protected readonly rail: RailItem[] = [
    { icon: 'dashboard', label: 'Dashboard' },
    { icon: 'settings', label: 'System Admin' },
    { icon: 'users', label: 'Human Resource' },
    { icon: 'inventory', label: 'Inventory' },
    { icon: 'cart', label: 'Procurement' },
    { icon: 'chart', label: 'Reports' },
    { icon: 'briefcase', label: 'Recruitment', route: '/recruitment' },
  ];

  protected readonly moduleNav: ModuleNavItem[] = [
    { icon: 'dashboard', label: 'Dashboard', route: '/recruitment/dashboard' },
    { icon: 'plus-circle', label: 'Create Requisition', route: '/recruitment/job-create' },
    { icon: 'list', label: 'Job Requisitions', route: '/recruitment/jobs' },
    { icon: 'user-plus', label: 'Applications', route: '/recruitment/applications' },
    { icon: 'calendar', label: 'Interviews', route: '/recruitment/interviews' },
    { icon: 'clipboard-check', label: 'Evaluation', route: '/recruitment/evaluation' },
    { icon: 'settings', label: 'AI Settings', route: '/recruitment/ai-settings' },
  ];
}
