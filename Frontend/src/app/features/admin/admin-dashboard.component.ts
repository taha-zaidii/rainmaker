import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IconComponent } from '../../shared/icon.component';

@Component({
  selector: 'rm-admin-dashboard',
  standalone: true,
  imports: [IconComponent, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-w-[720px] mx-auto px-6 py-16 text-center">
      <div class="w-14 h-14 mx-auto rounded-2xl bg-primary-tint text-primary grid place-items-center">
        <rm-icon name="settings" [size]="26" />
      </div>
      <h1 class="mt-5 rm-page-title">Admin Console</h1>
      <p class="mt-2 text-sm text-ink-muted leading-6">
        The admin portal is a separate shell module. This enforces strict boundary separation from HRMS and Recruitment.
      </p>
      <div class="mt-6">
        <a routerLink="/admin/system-setup" class="rm-btn-primary">
          <rm-icon name="palette" [size]="16" /> Open System Setup
        </a>
      </div>
    </div>
  `,
})
export class AdminDashboardComponent {}
