import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { IconComponent, IconName } from '../../../icon.component';

export interface SegmentedOption<T extends string = string> {
  value: T;
  label: string;
  icon?: IconName;
}

/**
 * A radiogroup-semantics segmented control — theme mode, density, font
 * scale: anywhere a setting is a small closed set of mutually exclusive
 * options, which reads better as one control than three separate buttons or
 * a dropdown that hides the alternatives.
 *
 * Generic so callers keep their own union type (ThemePreference, Density,
 * ...) instead of everything collapsing to `string`.
 */
@Component({
  selector: 'rm-segmented',
  standalone: true,
  imports: [IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      role="radiogroup"
      [attr.aria-label]="ariaLabel() || null"
      class="inline-flex p-1 gap-1 rounded-full"
      style="background-color: color-mix(in srgb, var(--color-surface-muted) 80%, transparent); backdrop-filter: blur(16px) saturate(1.6);"
    >
      @for (opt of options(); track opt.value) {
        <button
          type="button"
          role="radio"
          [attr.aria-checked]="opt.value === value()"
          (click)="select(opt.value)"
          class="flex items-center gap-1.5 h-8 px-4 rounded-full text-13 font-medium transition-all duration-[var(--duration-fast)] ease-[var(--ease-standard)]"
          [class.bg-surface]="opt.value === value()"
          [class.shadow-sm]="opt.value === value()"
          [class.text-ink]="opt.value === value()"
          [class.text-ink-muted]="opt.value !== value()"
          [class.hover:text-ink-soft]="opt.value !== value()"
        >
          @if (opt.icon) {
            <rm-icon [name]="opt.icon" [size]="15" />
          }
          {{ opt.label }}
        </button>
      }
    </div>
  `,
})
export class RmSegmentedComponent<T extends string = string> {
  readonly options = input.required<SegmentedOption<T>[]>();
  readonly value = input.required<T>();
  readonly ariaLabel = input<string>('');
  readonly valueChange = output<T>();

  protected select(value: T): void {
    if (value !== this.value()) {
      this.valueChange.emit(value);
    }
  }
}
