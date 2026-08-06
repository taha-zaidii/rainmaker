import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'rm-ai-card',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="p-6 rounded-2xl border border-ai-border bg-ai-surface shadow-sm space-y-4">
      <div class="flex items-center justify-between pb-3 border-b border-ai-border/60">
        <div class="flex items-center gap-2">
          <div class="w-7 h-7 rounded-lg bg-ai-tint text-ai grid place-items-center font-bold text-xs">
            ✨
          </div>
          <div>
            <h4 class="text-sm font-bold text-ai-deep">{{ title() }}</h4>
            @if (subtitle()) {
              <p class="text-xs text-ai-deep/80 mt-0.5">{{ subtitle() }}</p>
            }
          </div>
        </div>

        @if (showAdvisoryBadge()) {
          <span class="px-2.5 py-1 rounded-md text-[11px] font-semibold bg-surface border border-border text-ink-muted flex items-center gap-1">
            <span>ℹ️</span> Advisory AI (Review required)
          </span>
        }
      </div>

      <ng-content></ng-content>
    </div>
  `,
})
export class RmAiCardComponent {
  readonly title = input<string>('AI Advisory Insight');
  readonly subtitle = input<string>();
  readonly showAdvisoryBadge = input<boolean>(true);
}
