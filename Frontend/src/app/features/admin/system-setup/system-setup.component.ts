import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { IconComponent } from '../../../shared/icon.component';
import { RmSegmentedComponent, SegmentedOption } from '../../../shared/components/ui/segmented/segmented.component';
import { RmToggleComponent } from '../../../shared/components/ui/toggle/toggle.component';
import { ThemeService, ThemePreference } from '../../../core/theme/theme.service';
import {
  AppearanceService,
  BRAND_SWATCHES,
  Density,
  FontScale,
  NavLayout,
} from '../../../core/theme/appearance.service';

/**
 * System Setup — theme, density, typography, brand colour and sidebar
 * layout for the tenant.
 *
 * Everything here applies live (the same instant-apply model the existing
 * theme toggle already uses) and persists to localStorage per company. That
 * is a deliberate scope boundary for this pass: the natural next step is a
 * backend-persisted per-company settings row, the same shape the AI settings
 * screen already uses, so an admin's choice here survives across devices and
 * applies to every user in the org rather than just this browser. Brand
 * colour and logo are conceptually ORG-WIDE (an admin sets the tenant's
 * white-label identity); theme, density, type scale and sidebar mode are
 * conceptually PERSONAL (each user's own comfort setting). Both live in one
 * screen today because there is only one settings store to write to — the
 * moment real multi-user accounts exist, this screen splits along that line.
 */
@Component({
  selector: 'rm-system-setup',
  standalone: true,
  imports: [IconComponent, RmSegmentedComponent, RmToggleComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './system-setup.component.html',
})
export class SystemSetupComponent {
  protected readonly theme = inject(ThemeService);
  protected readonly appearance = inject(AppearanceService);

  protected readonly swatches = BRAND_SWATCHES;

  protected readonly themeOptions: SegmentedOption<ThemePreference>[] = [
    { value: 'light', label: 'Light', icon: 'sun' },
    { value: 'dark', label: 'Dark', icon: 'moon' },
    { value: 'system', label: 'System', icon: 'monitor' },
  ];

  protected readonly densityOptions: SegmentedOption<Density>[] = [
    { value: 'comfortable', label: 'Comfortable' },
    { value: 'compact', label: 'Compact' },
  ];

  protected readonly fontScaleOptions: SegmentedOption<FontScale>[] = [
    { value: 'small', label: 'Small' },
    { value: 'default', label: 'Default' },
    { value: 'large', label: 'Large' },
  ];

  protected readonly navLayoutOptions: SegmentedOption<NavLayout>[] = [
    { value: 'sidebar', label: 'Sidebar', icon: 'sidebar' },
    { value: 'horizontal', label: 'Horizontal', icon: 'top-nav' },
  ];

  protected customHex = '';

  protected onThemeChange(value: ThemePreference): void {
    this.theme.set(value);
  }

  protected onBrandSwatch(hex: string): void {
    this.appearance.setBrandColor(hex);
    this.customHex = '';
  }

  protected onCustomHex(value: string): void {
    this.customHex = value;
    if (/^#[0-9a-fA-F]{6}$/.test(value)) {
      this.appearance.setBrandColor(value);
    }
  }

  protected reset(): void {
    this.theme.set('system');
    this.appearance.resetToDefaults();
    this.customHex = '';
  }
}
