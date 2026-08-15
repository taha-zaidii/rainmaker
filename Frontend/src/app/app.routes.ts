import { Routes } from '@angular/router';

import { ShellComponent } from './layout/shell.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'recruitment/dashboard' },

  // ── Public careers site ─────────────────────────────────────────────────
  // Deliberately OUTSIDE the portal shell: no sidebar, no top bar, no auth.
  // This is the only surface anonymous internet traffic reaches, and the
  // backend endpoints behind it are [AllowAnonymous] and return published
  // requisitions only.
  {
    path: 'careers',
    loadComponent: () =>
      import('./features/careers/careers.component').then((m) => m.CareersComponent),
  },
  {
    path: 'careers/job/:id',
    loadComponent: () =>
      import('./features/careers/job-detail.component').then((m) => m.JobDetailComponent),
  },
  {
    path: 'recruitment',
    component: ShellComponent,
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/recruitment/dashboard/dashboard.component').then(
            (m) => m.DashboardComponent,
          ),
      },
      {
        path: 'ai-settings',
        loadComponent: () =>
          import('./features/recruitment/ai-settings/ai-settings.component').then(
            (m) => m.AiSettingsComponent,
          ),
      },
      {
        path: 'job-create',
        loadComponent: () =>
          import('./features/recruitment/job-create/job-create.component').then(
            (m) => m.JobCreateComponent,
          ),
      },
      {
        path: 'job-requisitions',
        loadComponent: () =>
          import(
            './features/recruitment/job-requisitions/job-requisitions.component'
          ).then((m) => m.JobRequisitionsComponent),
      },
      {
        path: 'jobs',
        loadComponent: () =>
          import(
            './features/recruitment/job-requisitions/job-requisitions.component'
          ).then((m) => m.JobRequisitionsComponent),
      },
      {
        path: 'applications',
        loadComponent: () =>
          import(
            './features/recruitment/applications-management/applications-management.component'
          ).then((m) => m.ApplicationsManagementComponent),
      },
      {
        path: 'upload-resume',
        loadComponent: () =>
          import(
            './features/recruitment/upload-resume/upload-resume.component'
          ).then((m) => m.UploadResumeComponent),
      },
      {
        path: 'application-details',
        loadComponent: () =>
          import(
            './features/recruitment/application-details/application-details.component'
          ).then((m) => m.ApplicationDetailsComponent),
      },
      // Designed but not built. Routed to an honest placeholder rather than
      // left to 404 — a dead link in a nav rail reads as a bug.
      {
        path: ':section',
        loadComponent: () =>
          import('./features/recruitment/coming-soon.component').then(
            (m) => m.ComingSoonComponent,
          ),
      },
    ],
  },
  {
    path: 'admin',
    component: ShellComponent,
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/admin/admin-dashboard.component').then(
            (m) => m.AdminDashboardComponent,
          ),
      },
      {
        path: 'system-setup',
        loadComponent: () =>
          import('./features/admin/system-setup/system-setup.component').then(
            (m) => m.SystemSetupComponent,
          ),
      },
      {
        path: '**',
        loadComponent: () =>
          import('./features/admin/admin-dashboard.component').then(
            (m) => m.AdminDashboardComponent,
          ),
      },
    ],
  },
  {
    path: 'hrms',
    component: ShellComponent,
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/hrms/hrms-dashboard.component').then(
            (m) => m.HrmsDashboardComponent,
          ),
      },
      {
        path: '**',
        loadComponent: () =>
          import('./features/hrms/hrms-dashboard.component').then(
            (m) => m.HrmsDashboardComponent,
          ),
      },
    ],
  },
  { path: '**', redirectTo: 'recruitment/dashboard' },
];
