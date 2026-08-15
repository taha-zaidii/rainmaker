import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

/**
 * The switch used across AI Feature Settings, extracted so every screen that
 * needs an on/off control shares one implementation instead of re-deriving
 * the same markup. Behaviour (not just look) is shared: role="switch",
 * keyboard-operable via the native button element, disabled state included.
 */
@Component({
  selector: 'rm-toggle',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      type="button"
      role="switch"
      [attr.aria-checked]="checked()"
      [attr.aria-label]="label() || null"
      [disabled]="disabled()"
      (click)="onToggle()"
      class="w-11 h-6 rounded-full shrink-0 relative transition-colors duration-[var(--duration-base)]"
      [class.bg-primary]="checked()"
      [class.bg-line-strong]="!checked()"
    >
      <span
        class="absolute top-0.5 w-5 h-5 rounded-full bg-white shadow-sm transition-[left] duration-[var(--duration-base)] ease-[var(--ease-standard)]"
        [style.left.px]="checked() ? 22 : 2"
      ></span>
    </button>
  `,
})
export class RmToggleComponent {
  readonly checked = input(false);
  readonly disabled = input(false);
  readonly label = input<string>('');
  readonly checkedChange = output<boolean>();

  protected onToggle(): void {
    if (this.disabled()) return;
    this.checkedChange.emit(!this.checked());
  }
}
