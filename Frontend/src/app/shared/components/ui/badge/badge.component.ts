import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';

export type RmBadgeTone = 'neutral' | 'success' | 'warning' | 'danger' | 'blue' | 'ai';

@Component({
  selector: 'rm-badge',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span
      class="inline-flex items-center gap-1.5 font-medium rounded-full transition-colors"
      [class.px-2.5]="size() === 'sm'"
      [class.py-0.5]="size() === 'sm'"
      [class.text-xs]="size() === 'sm'"
      [class.px-3]="size() === 'md'"
      [class.py-1]="size() === 'md'"
      [class.text-sm]="size() === 'md'"
      [ngClass]="{
        'bg-surface-muted text-ink-muted border border-border': tone() === 'neutral',
        'bg-emerald-50 text-emerald-700 border border-emerald-200': tone() === 'success',
        'bg-amber-50 text-amber-700 border border-amber-200': tone() === 'warning',
        'bg-red-50 text-red-700 border border-red-200': tone() === 'danger',
        'bg-blue-50 text-blue-700 border border-blue-200': tone() === 'blue',
        'bg-ai-tint text-ai-deep border border-ai-border font-semibold': tone() === 'ai'
      }"
    >
      <ng-content></ng-content>
    </span>
  `,
})
export class RmBadgeComponent {
  readonly tone = input<RmBadgeTone>('neutral');
  readonly size = input<'sm' | 'md'>('sm');
}
