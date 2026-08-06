import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';

export type RmButtonVariant = 'primary' | 'secondary' | 'danger' | 'ai';

@Component({
  selector: 'rm-button',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      [type]="type()"
      [disabled]="disabled() || loading()"
      (click)="btnClick.emit($event)"
      class="inline-flex items-center justify-center gap-2 font-semibold rounded-xl transition-all duration-150 disabled:opacity-50 disabled:cursor-not-allowed cursor-pointer"
      [class.px-3]="size() === 'sm'"
      [class.py-1.5]="size() === 'sm'"
      [class.text-xs]="size() === 'sm'"
      [class.px-4]="size() === 'md'"
      [class.py-2]="size() === 'md'"
      [class.text-sm]="size() === 'md'"
      [class.px-6]="size() === 'lg'"
      [class.py-2.5]="size() === 'lg'"
      [class.text-base]="size() === 'lg'"
      [ngClass]="{
        'bg-primary text-white hover:bg-primary-hover shadow-sm active:scale-[0.98]': variant() === 'primary',
        'bg-surface-alt border border-border text-ink hover:bg-surface-muted active:scale-[0.98]': variant() === 'secondary',
        'bg-red-600 text-white hover:bg-red-700 shadow-sm active:scale-[0.98]': variant() === 'danger',
        'bg-gradient-to-r from-indigo-600 to-primary text-white hover:opacity-90 shadow-sm active:scale-[0.98]': variant() === 'ai'
      }"
    >
      @if (loading()) {
        <div class="w-3.5 h-3.5 border-2 border-current border-t-transparent rounded-full animate-spin"></div>
      }
      <ng-content></ng-content>
    </button>
  `,
})
export class RmButtonComponent {
  readonly variant = input<RmButtonVariant>('primary');
  readonly size = input<'sm' | 'md' | 'lg'>('md');
  readonly type = input<'button' | 'submit' | 'reset'>('button');
  readonly disabled = input<boolean>(false);
  readonly loading = input<boolean>(false);

  readonly btnClick = output<MouseEvent>();
}
