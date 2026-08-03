import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { IconComponent } from '../../../shared/icon.component';
import { RecruitmentAiService } from '../../../core/api/recruitment-ai.service';
import { environment } from '../../../../environments/environment';

export interface ParsedProfileData {
  candidateName?: string;
  email?: string;
  phone?: string;
  totalExperienceYears?: number;
  summary?: string;
  skills?: string[];
  education?: string[];
  workExperience?: string[];
  rawJson?: string;
}

@Component({
  selector: 'rm-applications-management',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-w-[1200px] mx-auto px-6 py-8">
      <!-- Header -->
      <div class="flex flex-col md:flex-row md:items-center justify-between gap-4 pb-6 border-b border-border-light">
        <div>
          <h1 class="rm-page-title text-2xl font-semibold text-ink">Applications Management & AI Resume Parser</h1>
          <p class="mt-1 text-sm text-ink-muted">
            View submitted applications, upload initial candidate resumes, and run automated AI extraction.
          </p>
        </div>
        <div class="flex items-center gap-3">
          <a routerLink="/recruitment/job-create" class="rm-btn-ai flex items-center gap-2">
            <rm-icon name="sparkles" [size]="16" /> Create Job Requisition
          </a>
        </div>
      </div>

      <!-- Resume Parsing Tool Card -->
      <div class="mt-8 bg-surface rounded-2xl border border-border p-6 shadow-sm">
        <div class="flex items-center justify-between gap-4 mb-4">
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-xl bg-primary-tint text-primary grid place-items-center">
              <rm-icon name="file-text" [size]="20" />
            </div>
            <div>
              <h2 class="text-base font-semibold text-ink">AI Candidate Resume Parser</h2>
              <p class="text-xs text-ink-muted">
                Parse 1st candidate / sample resume attached to a Requisition using Multinet AI
              </p>
            </div>
          </div>
          <span class="text-xs px-2.5 py-1 rounded-full bg-ai-tint text-ai font-medium">
            Multinet AI Engine
          </span>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-3 gap-4 mt-4">
          <div>
            <label class="block text-xs font-medium text-ink-muted mb-1">Job Requisition ID</label>
            <input
              type="number"
              [(ngModel)]="requisitionId"
              placeholder="e.g. 1"
              class="w-full px-3 py-2 text-sm bg-surface-alt border border-border rounded-lg focus:outline-none focus:border-primary"
            />
          </div>

          <div class="md:col-span-2">
            <label class="block text-xs font-medium text-ink-muted mb-1">
              Resume Storage URL or File Path *
            </label>
            <input
              type="text"
              [(ngModel)]="resumeUrl"
              placeholder="https://storage.rainmaker.pk/resumes/sample_candidate_resume.pdf"
              class="w-full px-3 py-2 text-sm bg-surface-alt border border-border rounded-lg focus:outline-none focus:border-primary"
            />
          </div>
        </div>

        <div class="mt-4 flex items-center justify-between">
          <p class="text-xs text-ink-muted">
            Enter a storage URL or file path (PDF/Docx/Image) to parse candidate profile details instantly.
          </p>
          <button
            (click)="parseResume()"
            [disabled]="isParsing() || !resumeUrl.trim()"
            class="rm-btn-primary flex items-center gap-2"
          >
            @if (isParsing()) {
              <rm-icon name="sparkles" [size]="16" class="animate-spin" /> Parsing Resume...
            } @else {
              <rm-icon name="sparkles" [size]="16" /> Parse Candidate Resume
            }
          </button>
        </div>

        @if (error()) {
          <div class="mt-4 p-3 rounded-lg bg-red-50 text-red-600 text-xs border border-red-200">
            {{ error() }}
          </div>
        }
      </div>

      <!-- AI Parsed Result Display -->
      @if (parsedProfile(); as profile) {
        <div class="mt-8 bg-surface rounded-2xl border border-border-ai p-6 shadow-md bg-gradient-to-br from-white to-amber-50/30">
          <div class="flex items-start justify-between pb-4 border-b border-border">
            <div class="flex items-center gap-3">
              <div class="w-12 h-12 rounded-full bg-ai-tint text-ai grid place-items-center font-bold text-lg">
                {{ profile.candidateName?.substring(0, 1) || 'C' }}
              </div>
              <div>
                <h3 class="text-lg font-bold text-ink">
                  {{ profile.candidateName || 'Candidate Profile Extracted' }}
                </h3>
                <div class="flex flex-wrap items-center gap-3 mt-1 text-xs text-ink-muted">
                  @if (profile.email) {
                    <span>📧 {{ profile.email }}</span>
                  }
                  @if (profile.phone) {
                    <span>📞 {{ profile.phone }}</span>
                  }
                  @if (profile.totalExperienceYears) {
                    <span class="font-medium text-ink">
                      💼 {{ profile.totalExperienceYears }} Years Experience
                    </span>
                  }
                </div>
              </div>
            </div>
            <div class="flex items-center gap-3">
              <span class="text-xs font-semibold px-3 py-1 rounded-full bg-green-100 text-green-700">
                Parsed & Verified
              </span>
              <a
                [routerLink]="['/recruitment/application-details']"
                [queryParams]="{
                  name: profile.candidateName,
                  email: profile.email,
                  phone: profile.phone,
                  skills: profile.skills?.join(','),
                  resumeUrl: resumeUrl
                }"
                class="rm-btn-primary text-xs px-3 py-1.5 flex items-center gap-1"
              >
                <rm-icon name="file-text" [size]="14" /> View Full Application Details
              </a>
            </div>
          </div>

          @if (profile.summary) {
            <div class="mt-4">
              <h4 class="text-xs font-semibold text-ink-muted uppercase tracking-wider mb-1">
                Candidate Summary
              </h4>
              <p class="text-sm text-ink leading-relaxed">{{ profile.summary }}</p>
            </div>
          }

          @if (profile.skills && profile.skills.length > 0) {
            <div class="mt-4">
              <h4 class="text-xs font-semibold text-ink-muted uppercase tracking-wider mb-2">
                Extracted Skills
              </h4>
              <div class="flex flex-wrap gap-1.5">
                @for (skill of profile.skills; track skill) {
                  <span class="px-2.5 py-1 text-xs rounded-md bg-surface-alt border border-border text-ink">
                    {{ skill }}
                  </span>
                }
              </div>
            </div>
          }

          @if (profile.education && profile.education.length > 0) {
            <div class="mt-4">
              <h4 class="text-xs font-semibold text-ink-muted uppercase tracking-wider mb-2">
                Education & Qualifications
              </h4>
              <ul class="list-disc list-inside text-sm text-ink space-y-1">
                @for (edu of profile.education; track edu) {
                  <li>{{ edu }}</li>
                }
              </ul>
            </div>
          }
        </div>
      }

      <!-- Applications List Table -->
      <div class="mt-8 bg-surface rounded-2xl border border-border overflow-hidden">
        <div class="px-6 py-4 border-b border-border flex items-center justify-between">
          <h3 class="text-base font-semibold text-ink">Recent Candidate Applications</h3>
          <span class="text-xs text-ink-muted">Applications Portal</span>
        </div>

        <div class="overflow-x-auto">
          <table class="w-full text-left text-sm text-ink">
            <thead class="bg-surface-alt text-xs font-semibold text-ink-muted uppercase border-b border-border">
              <tr>
                <th class="px-6 py-3">Applicant Name</th>
                <th class="px-6 py-3">Applied Position</th>
                <th class="px-6 py-3">Email / Contact</th>
                <th class="px-6 py-3">Resume Status</th>
                <th class="px-6 py-3 text-right">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-border">
              @for (app of sampleApplications; track app.id) {
                <tr class="hover:bg-surface-alt/50 transition">
                  <td class="px-6 py-4 font-medium">{{ app.name }}</td>
                  <td class="px-6 py-4 text-ink-muted">{{ app.position }}</td>
                  <td class="px-6 py-4 text-ink-muted">{{ app.email }}</td>
                  <td class="px-6 py-4">
                    <span
                      class="px-2.5 py-1 rounded-full text-xs font-medium"
                      [ngClass]="{
                        'bg-green-100 text-green-700': app.parsed,
                        'bg-amber-100 text-amber-700': !app.parsed
                      }"
                    >
                      {{ app.parsed ? 'AI Parsed' : 'Pending Parse' }}
                    </span>
                  </td>
                  <td class="px-6 py-4 text-right flex items-center justify-end gap-3">
                    <a
                      [routerLink]="['/recruitment/application-details']"
                      [queryParams]="{
                        name: app.name,
                        email: app.email,
                        jobTitle: app.position,
                        resumeUrl: app.resumePath
                      }"
                      class="text-xs font-medium text-ink-soft hover:text-primary flex items-center gap-1"
                    >
                      <rm-icon name="file-text" [size]="14" /> View Details
                    </a>
                    <button
                      (click)="parseSample(app.resumePath)"
                      class="text-xs font-medium text-primary hover:underline flex items-center gap-1"
                    >
                      <rm-icon name="sparkles" [size]="14" /> Parse Candidate Resume
                    </button>
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
export class ApplicationsManagementComponent {
  private readonly api = inject(RecruitmentAiService);

  protected requisitionId = 1;
  protected resumeUrl = 'https://ai.rainmaker.pk/samples/senior_frontend_resume.pdf';
  protected isParsing = signal(false);
  protected error = signal<string | null>(null);
  protected parsedProfile = signal<ParsedProfileData | null>(null);

  protected readonly sampleApplications = [
    {
      id: 1,
      name: 'Sarah Jenkins',
      position: 'Senior Frontend Developer',
      email: 'sarah.jenkins@example.com',
      parsed: true,
      resumePath: 'https://ai.rainmaker.pk/samples/senior_frontend_resume.pdf',
    },
    {
      id: 2,
      name: 'Muhammad Tariq',
      position: 'Team Lead - IT',
      email: 'tariq.m@example.com',
      parsed: false,
      resumePath: 'https://ai.rainmaker.pk/samples/team_lead_resume.pdf',
    },
  ];

  protected parseResume(): void {
    if (!this.resumeUrl.trim()) return;

    this.isParsing.set(true);
    this.error.set(null);
    this.parsedProfile.set(null);

    this.api
      .parseResume({
        companyId: environment.companyId,
        jobRequisitionId: this.requisitionId,
        jobApplicationId: 1,
        resumeFilePath: this.resumeUrl.trim(),
      })
      .subscribe({
        next: (res) => {
          this.isParsing.set(false);
          if (res.isSuccess && res.data) {
            let parsedDataObj: any = {};
            try {
              parsedDataObj = typeof res.data.parsedDataJson === 'string'
                ? JSON.parse(res.data.parsedDataJson)
                : (res.data.parsedDataJson || {});
            } catch {
              parsedDataObj = {};
            }

            this.parsedProfile.set({
              candidateName: res.data.candidateName || parsedDataObj.full_name || parsedDataObj.name || 'Extracted Candidate',
              email: res.data.email || parsedDataObj.email,
              phone: res.data.phoneNumber || parsedDataObj.phone,
              totalExperienceYears: res.data.totalExperienceYears || parsedDataObj.total_experience_years,
              summary: parsedDataObj.summary || 'Resume parsed successfully via Multinet AI service.',
              skills: parsedDataObj.skills || res.data.skills || [],
              education: parsedDataObj.education || [],
              workExperience: parsedDataObj.work_experience || [],
            });
          } else {
            this.error.set(res.message || 'Failed to parse candidate resume.');
          }
        },
        error: (err) => {
          this.isParsing.set(false);
          this.error.set(err.message || 'Error executing AI resume parsing.');
        },
      });
  }

  protected parseSample(url: string): void {
    this.resumeUrl = url;
    this.parseResume();
  }
}
