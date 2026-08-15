import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IconComponent, IconName } from '../../../icon.component';

@Component({
  selector: 'rm-drawer',
  standalone: true,
  imports: [CommonModule, IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <aside class="rm-card shrink-0 overflow-hidden sticky top-6 rm-anim-spring-in shadow-md transition-[width] duration-[var(--duration-base)] ease-[var(--ease-standard)]" [style.width]="width()">
      <div class="px-5 py-4 flex items-start gap-2.5" [ngClass]="headerClass()">
        @if (icon()) {
          <rm-icon [name]="icon()!" [size]="18" class="shrink-0 mt-0.5" [ngClass]="iconColorClass()" />
        }
        <div class="flex-1 min-w-0">
          <div class="text-sm font-semibold" [ngClass]="titleColorClass()">{{ title() }}</div>
          @if (subtitle()) {
            <div class="text-xs truncate" [ngClass]="subtitleColorClass()">
              {{ subtitle() }}
            </div>
          }
        </div>
        <button
          class="p-1.5 rounded transition-colors"
          [ngClass]="closeBtnClass()"
          (click)="closed.emit()"
          aria-label="Close"
        >
          <rm-icon name="x" [size]="16" />
        </button>
      </div>

      <div class="p-5">
        <ng-content></ng-content>
      </div>
    </aside>
  `
})
export class RmDrawerComponent {
  readonly title = input<string>('');
  readonly subtitle = input<string>('');
  readonly icon = input<IconName | null>(null);
  readonly width = input<string>('420px');
  
  readonly variant = input<'default' | 'ai'>('default');

  readonly closed = output<void>();

  headerClass() {
    return this.variant() === 'ai' ? 'rm-ai-surface' : 'bg-surface border-b border-line';
  }

  iconColorClass() {
    return this.variant() === 'ai' ? 'text-ai' : 'text-ink-muted';
  }

  titleColorClass() {
    return this.variant() === 'ai' ? 'text-ai-deep' : 'text-ink';
  }

  subtitleColorClass() {
    return this.variant() === 'ai' ? 'text-ai-deep/80' : 'text-ink-muted';
  }

  closeBtnClass() {
    return this.variant() === 'ai' 
      ? 'text-ai-deep/70 hover:bg-ai-border' 
      : 'text-ink-muted hover:bg-surface-muted';
  }
}
