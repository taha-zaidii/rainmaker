import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

import { IconComponent } from '../../../shared/icon.component';
import { RecruitmentAiService } from '../../../core/api/recruitment-ai.service';

@Component({
  selector: 'rm-job-requisitions',
  standalone: true,
  imports: [CommonModule, RouterLink, IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-w-[1200px] mx-auto px-6 py-8">
      <!-- Header -->
      <div class="flex flex-col md:flex-row md:items-center justify-between gap-4 pb-6 border-b border-border-light">
        <div>
          <h1 class="rm-page-title text-2xl font-semibold text-ink">Job Requisitions</h1>
          <p class="mt-1 text-sm text-ink-muted">
            Manage created & published Job Descriptions of record in Rainmaker HRMS.
          </p>
        </div>
        <div class="flex items-center gap-3">
          <a routerLink="/recruitment/job-create" class="rm-btn-ai flex items-center gap-2">
            <rm-icon name="sparkles" [size]="16" /> Create AI Job Requisition
          </a>
        </div>
      </div>

      <!-- Requisitions Table -->
      <div class="mt-8 bg-surface rounded-2xl border border-border overflow-hidden shadow-sm">
        <div class="px-6 py-4 border-b border-border flex items-center justify-between">
          <h2 class="text-base font-semibold text-ink">Published & Active Requisitions</h2>
          <span class="text-xs px-3 py-1 rounded-full bg-primary-tint text-primary font-medium">
            Stored in Database
          </span>
        </div>

        <div class="overflow-x-auto">
          <table class="w-full text-left text-sm text-ink">
            <thead class="bg-surface-alt text-xs font-semibold text-ink-muted uppercase border-b border-border">
              <tr>
                <th class="px-6 py-3">Code / ID</th>
                <th class="px-6 py-3">Job Title</th>
                <th class="px-6 py-3">Department</th>
                <th class="px-6 py-3">Vacancies</th>
                <th class="px-6 py-3">Status</th>
                <th class="px-6 py-3 text-right">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-border">
              @for (req of requisitions; track req.id) {
                <tr class="hover:bg-surface-alt/50 transition">
                  <td class="px-6 py-4 font-mono text-xs font-semibold text-primary">
                    {{ req.code }}
                  </td>
                  <td class="px-6 py-4 font-semibold text-ink">
                    {{ req.title }}
                  </td>
                  <td class="px-6 py-4 text-ink-muted">{{ req.department }}</td>
                  <td class="px-6 py-4 text-ink-muted">{{ req.vacancies }}</td>
                  <td class="px-6 py-4">
                    <span
                      class="px-2.5 py-1 rounded-full text-xs font-semibold"
                      [ngClass]="{
                        'bg-green-100 text-green-700': req.status === 'Published',
                        'bg-amber-100 text-amber-700': req.status === 'Draft'
                      }"
                    >
                      {{ req.status }}
                    </span>
                  </td>
                  <td class="px-6 py-4 text-right flex items-center justify-end gap-3">
                    <a
                      [routerLink]="['/recruitment/applications']"
                      [queryParams]="{ requisitionId: req.id }"
                      class="rm-btn-secondary text-xs py-1.5 px-3 flex items-center gap-1"
                    >
                      <rm-icon name="file-text" [size]="14" /> Applications / Parse Resume
                    </a>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      </div>
    </div>
  `,
})
export class JobRequisitionsComponent {
  protected readonly requisitions = [
    {
      id: 1,
      code: 'REQ-2026-001',
      title: 'Senior Frontend Developer',
      department: 'Information Technology',
      vacancies: 1,
      status: 'Published',
    },
    {
      id: 2,
      code: 'REQ-2026-002',
      title: 'Team Lead - IT',
      department: 'Information Technology',
      vacancies: 2,
      status: 'Published',
    },
  ];
}
