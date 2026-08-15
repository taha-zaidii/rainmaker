import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IconComponent } from '../../shared/icon.component';

@Component({
  selector: 'rm-hrms-dashboard',
  standalone: true,
  imports: [IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-w-[720px] mx-auto px-6 py-16 text-center">
      <div class="w-14 h-14 mx-auto rounded-2xl bg-primary-tint text-primary grid place-items-center">
        <rm-icon name="users" [size]="26" />
      </div>
      <h1 class="mt-5 rm-page-title">HRMS Portal</h1>
      <p class="mt-2 text-sm text-ink-muted leading-6">
        The HRMS portal is a separate shell module. This enforces strict boundary separation from Admin and Recruitment.
      </p>
    </div>
  `,
})
export class HrmsDashboardComponent {}
