import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'rm-card',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="bg-surface rounded-2xl border border-border shadow-sm transition-all duration-200"
      [class.p-6]="padding() === 'normal'"
      [class.p-4]="padding() === 'compact'"
      [class.p-0]="padding() === 'none'"
      [class.hover:shadow-md]="hoverable()"
      [class.border-ai-border]="variant() === 'ai'"
      [class.bg-ai-surface]="variant() === 'ai'"
    >
      @if (title()) {
        <div class="flex items-center justify-between pb-4 mb-4 border-b border-border-light">
          <div>
            <h3 class="text-sm font-bold text-ink">{{ title() }}</h3>
            @if (subtitle()) {
              <p class="text-xs text-ink-muted mt-0.5">{{ subtitle() }}</p>
            }
          </div>
          <ng-content select="[card-action]"></ng-content>
        </div>
      }
      <ng-content></ng-content>
    </div>
  `,
})
export class RmCardComponent {
  readonly title = input<string>();
  readonly subtitle = input<string>();
  readonly padding = input<'normal' | 'compact' | 'none'>('normal');
  readonly hoverable = input<boolean>(false);
  readonly variant = input<'default' | 'ai'>('default');
}
