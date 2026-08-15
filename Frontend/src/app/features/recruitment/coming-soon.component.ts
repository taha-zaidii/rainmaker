import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';

import { IconComponent } from '../../shared/icon.component';

/**
 * Placeholder for routes whose screens are designed but not built.
 *
 * It names what is missing and why, rather than showing a generic "coming
 * soon" — most of these read from stored procedures that only arrive with the
 * production database backup, and saying so saves the next person the
 * investigation.
 */
@Component({
  selector: 'rm-coming-soon',
  standalone: true,
  imports: [IconComponent, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-w-[720px] mx-auto px-6 py-16 text-center">
      <div class="w-14 h-14 mx-auto rounded-2xl bg-primary-tint text-primary grid place-items-center">
        <rm-icon name="file-text" [size]="26" />
      </div>
      <h1 class="mt-5 rm-page-title capitalize">{{ title() }}</h1>
      <p class="mt-2 text-sm text-ink-muted leading-6">
        This screen is designed but not built yet. It reads from stored procedures that
        arrive with the production database, so it is deliberately not faked against the
        local demo schema — a screen that looks like it works but cannot is worse than one
        that says so.
      </p>
      <div class="mt-6 flex items-center justify-center gap-3">
        <a routerLink="/recruitment/job-create" class="rm-btn-ai">
          <rm-icon name="sparkles" [size]="16" /> Try AI job description
        </a>
        <a routerLink="/recruitment/ai-settings" class="rm-btn-secondary">AI Settings</a>
      </div>
    </div>
  `,
})
export class ComingSoonComponent {
  private readonly route = inject(ActivatedRoute);

  protected readonly title = toSignal(
    this.route.paramMap.pipe(map((p) => (p.get('section') ?? 'Section').replace(/-/g, ' '))),
    { initialValue: 'Section' },
  );
}
