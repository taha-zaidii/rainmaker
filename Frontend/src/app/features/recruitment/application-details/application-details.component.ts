import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';

import { IconComponent } from '../../../shared/icon.component';
import { RecruitmentService } from '../../../core/api/recruitment.service';
import { RecruitmentAiService } from '../../../core/api/recruitment-ai.service';
import { environment } from '../../../../environments/environment';

export interface ApplicationDetailModel {
  applicationId: number;
  applicationCode: string;
  appliedDate: string;
  status: string;
  // Candidate Information
  fullName: string;
  email: string;
  mobileNumber: string;
  location: string;
  // Job Information
  jobTitle: string;
  department: string;
  requisitionCode: string;
  experienceRequired: string;
  // Professional Information
  currentDesignation: string;
  currentCompany: string;
  experienceYears: string;
  expectedSalary: string;
  preferredLocation: string;
  noticePeriod: string;
  // Skills & Additional Info
  skills: string[];
  education: string;
  dateOfBirth: string;
  currentAddress: string;
  experienceSummary: string;
  coverLetter: string;
  // Resume File
  resumeUrl: string;
}

@Component({
  selector: 'rm-application-details',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-w-[1360px] mx-auto px-6 py-6">
      <!-- Top Action Bar -->
      <div class="flex flex-col md:flex-row md:items-center justify-between gap-4 pb-6 border-b border-border-light">
        <div>
          <a
            routerLink="/recruitment/applications"
            class="inline-flex items-center gap-1.5 text-xs font-semibold text-primary bg-primary-tint/60 hover:bg-primary-tint px-3 py-1.5 rounded-lg mb-3 transition"
          >
            ← Back to Applications
          </a>
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-xl bg-surface-alt border border-border grid place-items-center text-ink shadow-sm">
              <rm-icon name="file-text" [size]="20" />
            </div>
            <div>
              <div class="flex items-center gap-3">
                <h1 class="text-xl font-bold text-ink">Application Details</h1>
                <span
                  class="px-2.5 py-0.5 rounded-full text-xs font-bold uppercase tracking-wider shadow-sm"
                  [ngClass]="{
                    'bg-emerald-100 text-emerald-800 border border-emerald-300': (appData().status || '').toUpperCase() === 'SHORTLISTED',
                    'bg-rose-100 text-rose-800 border border-rose-300': (appData().status || '').toUpperCase() === 'REJECTED',
                    'bg-sky-100 text-sky-800 border border-sky-300': (appData().status || '').toUpperCase() === 'APPLIED' || (appData().status || '').toUpperCase() === 'SCREENING'
                  }"
                >
                  {{ appData().status || 'Applied' }}
                </span>
              </div>
              <p class="text-xs text-ink-muted">
                Application Code: <span class="font-semibold text-ink">{{ appData().applicationCode }}</span>
              </p>
            </div>
          </div>
        </div>

        <div class="flex flex-wrap items-center gap-3">
          <button
            (click)="screenWithAi()"
            [disabled]="screening()"
            class="px-4 py-2.5 text-xs font-semibold rounded-xl bg-gradient-to-r from-indigo-600 via-purple-600 to-primary text-white hover:opacity-95 flex items-center gap-2 shadow-md transition-all active:scale-95 disabled:opacity-50"
          >
            @if (screening()) {
              <div class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
              <span>Screening Profile...</span>
            } @else {
              <rm-icon name="sparkles" [size]="15" />
              <span>Screen Profile with AI</span>
            }
          </button>

          <button
            (click)="updateStatus('Shortlisted')"
            [disabled]="(appData().status || '').toUpperCase() === 'SHORTLISTED'"
            class="px-4 py-2.5 text-xs font-semibold rounded-xl border border-emerald-600 text-emerald-700 hover:bg-emerald-600 hover:text-white flex items-center gap-2 transition-all shadow-sm active:scale-95 disabled:opacity-40"
          >
            <rm-icon name="check" [size]="15" />
            <span>{{ (appData().status || '').toUpperCase() === 'SHORTLISTED' ? 'Shortlisted' : 'Shortlist Candidate' }}</span>
          </button>

          <button
            (click)="updateStatus('Rejected')"
            [disabled]="(appData().status || '').toUpperCase() === 'REJECTED'"
            class="px-4 py-2.5 text-xs font-semibold rounded-xl border border-rose-300 text-rose-700 hover:bg-rose-600 hover:text-white flex items-center gap-2 transition-all shadow-sm active:scale-95 disabled:opacity-40"
          >
            <rm-icon name="x" [size]="15" />
            <span>{{ (appData().status || '').toUpperCase() === 'REJECTED' ? 'Rejected' : 'Reject Application' }}</span>
          </button>
        </div>
      </div>

      <!-- AI Screening Card Banner (If Screened) -->
      @if (screenResult()) {
        <div class="mt-6 rounded-2xl bg-gradient-to-br from-slate-900 via-indigo-950 to-slate-900 text-white p-6 shadow-xl border border-indigo-500/20 space-y-6 relative overflow-hidden">
          <!-- Background Glow Effect -->
          <div class="absolute -top-24 -right-24 w-72 h-72 bg-indigo-500/10 rounded-full blur-3xl pointer-events-none"></div>

          <!-- Hero Banner Top Grid -->
          <div class="flex flex-col md:flex-row md:items-center justify-between gap-6 pb-6 border-b border-indigo-500/20">
            <div class="flex items-center gap-4">
              <!-- Circular Score Gauge -->
              <div
                class="w-16 h-16 rounded-2xl grid place-items-center font-black text-xl shadow-xl transition-all"
                [ngClass]="getScoreGaugeClass(screenResult()?.matchScore ?? null)"
              >
                {{ screenResult()?.matchScore }}%
              </div>

              <div class="space-y-1">
                <div class="flex flex-wrap items-center gap-2.5">
                  <h3 class="text-lg font-bold tracking-tight text-white flex items-center gap-2">
                    <rm-icon name="sparkles" [size]="18" class="text-indigo-400" />
                    AI Candidate Screening Analysis
                  </h3>
                  <span
                    class="px-3 py-1 rounded-full text-xs font-extrabold border shadow-sm"
                    [ngClass]="getScoreBadgeClass(screenResult()?.matchScore ?? null)"
                  >
                    {{ getScoreRatingLabel(screenResult()?.matchScore ?? null) }}
                  </span>
                </div>
                <p class="text-xs text-slate-300 flex items-center gap-2">
                  <span>Threshold: <strong class="text-white">{{ screenResult()?.thresholdUsed ?? 80 }}%</strong></span>
                  <span>•</span>
                  <span>Screened on {{ screenResult()?.screenedOn | date:'mediumDate' }} at {{ screenResult()?.screenedOn | date:'shortTime' }}</span>
                </p>
              </div>
            </div>

            <!-- Advisory Badge & Actions -->
            <div class="flex items-center gap-3">
              <span class="inline-flex items-center gap-1.5 text-xs font-semibold px-3 py-1.5 rounded-xl bg-slate-800/80 border border-slate-700 text-slate-300">
                <rm-icon name="info" [size]="14" class="text-indigo-400" />
                <span>Advisory AI Match Rating</span>
              </span>
            </div>
          </div>

          <!-- Matched vs Missing Skills Cards -->
          <div class="grid grid-cols-1 md:grid-cols-2 gap-5 text-xs">
            <!-- Matched Skills -->
            <div class="space-y-2.5 p-4 rounded-xl bg-slate-800/60 border border-emerald-500/30 backdrop-blur-sm">
              <div class="flex items-center justify-between">
                <span class="font-bold text-emerald-400 flex items-center gap-1.5 text-xs uppercase tracking-wider">
                  <rm-icon name="check" [size]="15" class="text-emerald-400" /> Matched Skills ({{ screenResult()?.matchedSkills?.length || 0 }})
                </span>
              </div>
              <div class="flex flex-wrap gap-1.5 pt-1">
                @for (skill of screenResult()?.matchedSkills; track skill) {
                  <span class="px-2.5 py-1 rounded-lg bg-emerald-500/15 border border-emerald-500/30 text-emerald-200 font-medium text-[12px] flex items-center gap-1">
                    <span>✓</span> {{ skill }}
                  </span>
                } @empty {
                  <span class="text-slate-400 italic">No skills matched directly</span>
                }
              </div>
            </div>

            <!-- Missing Skills -->
            <div class="space-y-2.5 p-4 rounded-xl bg-slate-800/60 border border-rose-500/30 backdrop-blur-sm">
              <div class="flex items-center justify-between">
                <span class="font-bold text-rose-400 flex items-center gap-1.5 text-xs uppercase tracking-wider">
                  <rm-icon name="alert-triangle" [size]="15" class="text-rose-400" /> Missing / Unmentioned Skills ({{ screenResult()?.missingSkills?.length || 0 }})
                </span>
              </div>
              <div class="flex flex-wrap gap-1.5 pt-1">
                @for (skill of screenResult()?.missingSkills; track skill) {
                  <span class="px-2.5 py-1 rounded-lg bg-rose-500/15 border border-rose-500/30 text-rose-200 font-medium text-[12px] flex items-center gap-1">
                    <span>⚠</span> {{ skill }}
                  </span>
                } @empty {
                  <span class="text-emerald-400 font-semibold flex items-center gap-1">
                    <span>✓</span> Complete Skill Match Requirement Satisfied!
                  </span>
                }
              </div>
            </div>
          </div>

          <!-- Rationale & Evidence Statements -->
          @if (screenResult()?.reasons?.length) {
            <div class="space-y-3 pt-2">
              <h4 class="text-xs font-bold text-indigo-300 uppercase tracking-widest flex items-center gap-1.5">
                <rm-icon name="file-text" [size]="14" /> AI Evaluation Rationale & Evidence
              </h4>
              <div class="grid grid-cols-1 gap-2.5">
                @for (reason of screenResult()?.reasons; track reason.detail) {
                  <div
                    class="p-3.5 rounded-xl border text-xs space-y-1.5 transition-all"
                    [ngClass]="{
                      'bg-emerald-950/40 border-emerald-500/30 text-emerald-100': reason.kind === 'match',
                      'bg-amber-950/40 border-amber-500/30 text-amber-100': reason.kind !== 'match'
                    }"
                  >
                    <div class="flex items-center gap-2">
                      <span
                        class="font-extrabold uppercase px-2 py-0.5 rounded-md text-[10px] tracking-wider"
                        [ngClass]="{
                          'bg-emerald-500/30 text-emerald-300 border border-emerald-500/40': reason.kind === 'match',
                          'bg-amber-500/30 text-amber-300 border border-amber-500/40': reason.kind !== 'match'
                        }"
                      >
                        {{ reason.kind === 'match' ? 'CONFIRMED MATCH' : 'GAP / PROBE' }}
                      </span>
                      <span class="font-semibold text-white text-[13px]">{{ reason.detail }}</span>
                    </div>
                    @if (reason.evidence) {
                      <p class="text-slate-300 italic pl-3 border-l-2 border-indigo-400/40 text-[12px] leading-relaxed">
                        "{{ reason.evidence }}"
                      </p>
                    }
                  </div>
                }
              </div>
            </div>
          }
        </div>
      }

      @if (screenError()) {
        <div class="mt-6 p-4 rounded-xl bg-red-50 border border-red-200 text-xs text-red-700 flex items-center gap-2 shadow-sm">
          <rm-icon name="alert-triangle" [size]="16" />
          <span class="font-semibold">{{ screenError() }}</span>
        </div>
      }


      <!-- Main Section with Tabs & Summary Sidebar -->
      <div class="grid grid-cols-1 lg:grid-cols-12 gap-8 mt-6">

        <!-- Left / Main Details Column (8 Cols) -->
        <div class="lg:col-span-8 space-y-6">
          <!-- Tab Navigation Bar (Details | Resume | Timeline) -->
          <div class="flex items-center gap-2 border-b border-border pb-1">
            <button
              (click)="activeTab.set('details')"
              class="px-4 py-2 text-sm font-semibold rounded-t-lg transition flex items-center gap-2"
              [ngClass]="{
                'bg-primary text-white': activeTab() === 'details',
                'text-ink-muted hover:text-ink': activeTab() !== 'details'
              }"
            >
              <rm-icon name="file-text" [size]="16" /> Details
            </button>
            <button
              (click)="activeTab.set('resume')"
              class="px-4 py-2 text-sm font-semibold rounded-t-lg transition flex items-center gap-2"
              [ngClass]="{
                'bg-primary text-white': activeTab() === 'resume',
                'text-ink-muted hover:text-ink': activeTab() !== 'resume'
              }"
            >
              <rm-icon name="file-text" [size]="16" /> Resume
            </button>
            <button
              (click)="activeTab.set('timeline')"
              class="px-4 py-2 text-sm font-semibold rounded-t-lg transition flex items-center gap-2"
              [ngClass]="{
                'bg-primary text-white': activeTab() === 'timeline',
                'text-ink-muted hover:text-ink': activeTab() !== 'timeline'
              }"
            >
              <rm-icon name="clock" [size]="16" /> Timeline
            </button>
          </div>

          <!-- TAB 1: DETAILS -->
          @if (activeTab() === 'details') {
            <div class="bg-surface rounded-2xl border border-border p-6 shadow-sm space-y-8">
              <!-- Top Grid: Application Code, Date, Status -->
              <div class="grid grid-cols-1 md:grid-cols-2 gap-6 pb-6 border-b border-border-light">
                <div>
                  <span class="block text-xs text-ink-muted">Application Code</span>
                  <span class="text-sm font-bold text-ink mt-0.5 block">{{ appData().applicationCode }}</span>
                </div>
                <div>
                  <span class="block text-xs text-ink-muted">Application Date</span>
                  <span class="text-sm font-semibold text-ink mt-0.5 block">{{ appData().appliedDate }}</span>
                </div>
                <div class="md:col-span-2">
                  <span class="block text-xs text-ink-muted mb-1">Status</span>
                  <span class="px-3 py-1 rounded-full text-xs font-semibold bg-primary-tint text-primary inline-block">
                    {{ appData().status }}
                  </span>
                </div>
              </div>

              <!-- Candidate Information -->
              <div class="space-y-4 pb-6 border-b border-border-light">
                <h3 class="text-xs font-bold text-ink-muted uppercase tracking-wider">Candidate Information</h3>
                <div class="grid grid-cols-1 md:grid-cols-2 gap-4 text-sm">
                  <div>
                    <span class="block text-xs text-ink-muted">Full Name</span>
                    <span class="font-bold text-ink mt-0.5 block">{{ appData().fullName }}</span>
                  </div>
                  <div>
                    <span class="block text-xs text-ink-muted">Email</span>
                    <span class="font-medium text-ink mt-0.5 block">{{ appData().email }}</span>
                  </div>
                  <div>
                    <span class="block text-xs text-ink-muted">Mobile Number</span>
                    <span class="font-medium text-ink mt-0.5 block">{{ appData().mobileNumber }}</span>
                  </div>
                  <div>
                    <span class="block text-xs text-ink-muted">Location</span>
                    <span class="font-medium text-ink mt-0.5 block">{{ appData().location }}</span>
                  </div>
                </div>
              </div>

              <!-- Job Information -->
              <div class="space-y-4 pb-6 border-b border-border-light">
                <h3 class="text-xs font-bold text-ink-muted uppercase tracking-wider">Job Information</h3>
                <div class="grid grid-cols-1 md:grid-cols-2 gap-4 text-sm">
                  <div>
                    <span class="block text-xs text-ink-muted">Job Title</span>
                    <span class="font-bold text-ink mt-0.5 block uppercase">{{ appData().jobTitle }}</span>
                  </div>
                  <div>
                    <span class="block text-xs text-ink-muted">Department</span>
                    <span class="font-medium text-ink mt-0.5 block">{{ appData().department }}</span>
                  </div>
                  <div>
                    <span class="block text-xs text-ink-muted">Requisition Code</span>
                    <span class="font-mono text-xs font-semibold text-primary mt-0.5 block">{{ appData().requisitionCode }}</span>
                  </div>
                  <div>
                    <span class="block text-xs text-ink-muted">Experience Required</span>
                    <span class="font-medium text-ink mt-0.5 block">{{ appData().experienceRequired }}</span>
                  </div>
                </div>
              </div>

              <!-- Professional Information -->
              <div class="space-y-4 pb-6 border-b border-border-light">
                <h3 class="text-xs font-bold text-ink-muted uppercase tracking-wider">Professional Information</h3>
                <div class="grid grid-cols-1 md:grid-cols-2 gap-4 text-sm">
                  <div>
                    <span class="block text-xs text-ink-muted">Current Designation</span>
                    <span class="font-medium text-ink mt-0.5 block capitalize">{{ appData().currentDesignation }}</span>
                  </div>
                  <div>
                    <span class="block text-xs text-ink-muted">Current Company</span>
                    <span class="font-medium text-ink mt-0.5 block capitalize">{{ appData().currentCompany }}</span>
                  </div>
                  <div>
                    <span class="block text-xs text-ink-muted">Experience</span>
                    <span class="font-medium text-ink mt-0.5 block">{{ appData().experienceYears }}</span>
                  </div>
                  <div>
                    <span class="block text-xs text-ink-muted">Expected Salary</span>
                    <span class="font-medium text-ink mt-0.5 block">{{ appData().expectedSalary }}</span>
                  </div>
                  <div>
                    <span class="block text-xs text-ink-muted">Preferred Location</span>
                    <span class="font-medium text-ink mt-0.5 block capitalize">{{ appData().preferredLocation }}</span>
                  </div>
                  <div>
                    <span class="block text-xs text-ink-muted">Notice Period</span>
                    <span class="font-medium text-ink mt-0.5 block">{{ appData().noticePeriod }}</span>
                  </div>
                </div>
              </div>

              <!-- Skills -->
              <div class="space-y-3 pb-6 border-b border-border-light">
                <h3 class="text-xs font-bold text-ink-muted uppercase tracking-wider">Skills</h3>
                <div class="flex flex-wrap gap-2">
                  @for (skill of appData().skills; track skill) {
                    <span class="px-3 py-1 text-xs rounded-full bg-primary-tint/70 text-primary font-medium">
                      {{ skill }}
                    </span>
                  }
                </div>
              </div>

              <!-- Additional Information -->
              <div class="space-y-4 pb-6 border-b border-border-light">
                <h3 class="text-xs font-bold text-ink-muted uppercase tracking-wider">Additional Information</h3>
                <div class="grid grid-cols-1 md:grid-cols-2 gap-4 text-sm">
                  <div>
                    <span class="block text-xs text-ink-muted">Education</span>
                    <span class="font-medium text-ink mt-0.5 block">{{ appData().education }}</span>
                  </div>
                  <div>
                    <span class="block text-xs text-ink-muted">Date of Birth</span>
                    <span class="font-medium text-ink mt-0.5 block">{{ appData().dateOfBirth }}</span>
                  </div>
                  <div class="md:col-span-2">
                    <span class="block text-xs text-ink-muted">Current Address</span>
                    <span class="font-medium text-ink mt-0.5 block">{{ appData().currentAddress }}</span>
                  </div>
                </div>
              </div>

              <!-- Experience Summary Box -->
              <div class="space-y-2">
                <h3 class="text-xs font-bold text-ink-muted uppercase tracking-wider">Experience Summary</h3>
                <div class="p-4 rounded-xl bg-surface-alt border border-border text-sm text-ink leading-relaxed">
                  {{ appData().experienceSummary }}
                </div>
              </div>

              <!-- Cover Letter Box -->
              <div class="space-y-2">
                <h3 class="text-xs font-bold text-ink-muted uppercase tracking-wider">Cover Letter</h3>
                <div class="p-4 rounded-xl bg-surface-alt border border-border text-sm text-ink leading-relaxed">
                  {{ appData().coverLetter }}
                </div>
              </div>
            </div>
          }

          <!-- TAB 2: RESUME VIEWER -->
          @if (activeTab() === 'resume') {
            <div class="bg-surface rounded-2xl border border-border p-6 shadow-sm">
              <div class="flex items-center justify-between pb-4 mb-4 border-b border-border">
                <h3 class="text-sm font-semibold text-ink">Candidate Resume Document</h3>
                <a
                  [href]="appData().resumeUrl"
                  target="_blank"
                  download
                  class="rm-btn-primary text-xs px-4 py-2 flex items-center gap-1.5"
                >
                  <rm-icon name="file-text" [size]="14" /> Download Resume
                </a>
              </div>

              <div class="w-full h-[700px] rounded-xl overflow-hidden border border-border bg-slate-900">
                <iframe
                  [src]="safeResumeUrl()"
                  class="w-full h-full border-0"
                  title="Candidate Resume"
                ></iframe>
              </div>
            </div>
          }

          <!-- TAB 3: TIMELINE -->
          @if (activeTab() === 'timeline') {
            <div class="bg-surface rounded-2xl border border-border p-6 shadow-sm">
              <h3 class="text-sm font-semibold text-ink mb-6">Application Timeline</h3>
              
              <div class="relative pl-6 space-y-6 before:absolute before:left-2 before:top-2 before:bottom-2 before:w-0.5 before:bg-border">
                <div class="relative">
                  <div class="absolute -left-6 top-0.5 w-4 h-4 rounded-full bg-primary ring-4 ring-primary-tint"></div>
                  <div>
                    <h4 class="text-sm font-semibold text-ink">Application Submitted</h4>
                    <p class="text-xs text-ink-muted mt-0.5">{{ appData().appliedDate }}</p>
                  </div>
                </div>

                <div class="relative">
                  <div class="absolute -left-6 top-0.5 w-4 h-4 rounded-full bg-amber-500 ring-4 ring-amber-100"></div>
                  <div>
                    <h4 class="text-sm font-semibold text-ink">AI Resume Extraction Completed</h4>
                    <p class="text-xs text-ink-muted mt-0.5">Parsed via Multinet AI Engine</p>
                  </div>
                </div>
              </div>
            </div>
          }
        </div>

        <!-- Right Column: Summary Card (4 Cols) -->
        <div class="lg:col-span-4">
          <div class="bg-surface rounded-2xl border border-border p-6 shadow-sm sticky top-6 space-y-6">
            <div class="flex items-center gap-2 pb-4 border-b border-border">
              <rm-icon name="info" [size]="18" class="text-ink-muted" />
              <h3 class="text-sm font-bold text-ink">Summary</h3>
            </div>

            <div class="space-y-4 text-xs">
              <div>
                <span class="block text-ink-muted mb-1">Current Status</span>
                <span class="px-3 py-1 rounded-full font-semibold bg-primary-tint text-primary inline-block">
                  {{ appData().status }}
                </span>
              </div>

              <div>
                <span class="block text-ink-muted mb-1">Applied On</span>
                <span class="text-sm font-semibold text-ink">{{ appData().appliedDate }}</span>
              </div>

              <div class="pt-4 border-t border-border-light">
                <span class="block text-ink-muted mb-2">Resume</span>
                <a
                  [href]="appData().resumeUrl"
                  target="_blank"
                  download
                  class="w-full rm-btn-secondary text-xs py-2 flex items-center justify-center gap-1.5"
                >
                  <rm-icon name="file-text" [size]="14" /> Download Resume
                </a>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
})
export class ApplicationDetailsComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(RecruitmentService);
  private readonly aiService = inject(RecruitmentAiService);
  private readonly sanitizer = inject(DomSanitizer);

  protected activeTab = signal<'details' | 'resume' | 'timeline'>('details');

  protected readonly loading = signal(true);
  protected readonly notFound = signal(false);

  protected readonly screening = signal(false);
  protected readonly screenResult = signal<import('../../../core/api/recruitment-ai.models').ScreenResumeResponse | null>(null);
  protected readonly screenError = signal<string | null>(null);


  /**
   * Starts EMPTY and is filled from the API.
   *
   * The previous version shipped a complete "Dominic Alvarez" record — a real
   * enough looking candidate, with a resume URL pointing at the staging
   * server — which rendered before any request was made. A details screen
   * that shows a person who does not exist is worse than one that shows
   * nothing, because nothing is obviously nothing.
   */
  protected appData = signal<ApplicationDetailModel>({
    applicationId: 0,
    applicationCode: '',
    appliedDate: '',
    status: '',
    fullName: '',
    email: '',
    mobileNumber: '',
    location: '',
    jobTitle: '',
    department: '',
    requisitionCode: '',
    experienceRequired: '',
    currentDesignation: '',
    currentCompany: '',
    experienceYears: '',
    expectedSalary: '',
    preferredLocation: '',
    noticePeriod: '',
    skills: [],
    education: '',
    dateOfBirth: '',
    currentAddress: '',
    experienceSummary: '',
    coverLetter: '',
    resumeUrl: '',
  });

  protected safeResumeUrl(): SafeResourceUrl {
    return this.sanitizer.bypassSecurityTrustResourceUrl(this.appData().resumeUrl);
  }

  constructor() {
    const id = Number(this.route.snapshot.queryParamMap.get('id'));

    if (!id) {
      this.loading.set(false);
      this.notFound.set(true);
      return;
    }

    this.api.getApplication(id).subscribe((application) => {
      this.loading.set(false);

      if (!application) {
        this.notFound.set(true);
        return;
      }

      const appSkills = application.skills
        ? application.skills.split(',').map((s: string) => s.trim()).filter(Boolean)
        : [];

      const expStr = application.experienceYears != null
        ? `${application.experienceYears}+ years`
        : (application.totalExperience != null ? `${application.totalExperience}+ years` : '3+ years');

      this.appData.set({
        applicationId: application.applicationID,
        applicationCode: application.applicationCode ?? '',
        appliedDate: application.applicationDate ?? '',
        status: application.statusName ?? 'Applied',

        fullName: application.fullName ?? '',
        email: application.email ?? '',
        mobileNumber: application.mobileNumber ?? '',
        location: application.currentAddress || application.preferredLocation || application.requisitionLocation || '',

        jobTitle: application.requisitionJobTitle ?? '',
        department: application.departmentName ?? '',
        requisitionCode: application.requisitionCode ?? '',
        experienceRequired: expStr,

        currentDesignation: application.currentDesignation || application.currentJobTitle || 'Software Engineer',
        currentCompany: application.currentCompany || 'N/A',
        experienceYears: expStr,
        expectedSalary: application.expectedSalary ? `$${application.expectedSalary}` : 'Negotiable',
        preferredLocation: application.preferredLocation || application.currentAddress || 'Karachi, Pakistan',
        noticePeriod: application.noticePeriod ? `${application.noticePeriod} days` : 'Immediate',

        skills: appSkills,
        education: application.education || 'Bachelor Degree',
        dateOfBirth: application.dateOfBirth ?? '',
        currentAddress: application.currentAddress ?? '',
        experienceSummary: application.experienceSummary || application.coverLetter || 'No experience summary provided.',
        coverLetter: application.coverLetter || 'No cover letter submitted.',
        resumeUrl: application.resumePath ?? '',
      });
    });
  }



  protected updateStatus(newStatus: string): void {
    this.appData.update((curr) => ({ ...curr, status: newStatus }));

    const data = this.appData();
    if (!data.applicationId) return;

    if (newStatus === 'Shortlisted') {
      this.api.shortlistCandidate(data.applicationId, environment.companyId).subscribe();
    } else if (newStatus === 'Rejected') {
      this.api.rejectApplication(data.applicationId, environment.companyId).subscribe();
    } else {
      const statusId = newStatus === 'Shortlisted' ? 2 : newStatus === 'Rejected' ? 7 : 1;
      this.api.updateApplication({
        applicationID: data.applicationId,
        currentStatusID: statusId,
        remarks: `Recruiter Decision: ${newStatus}`,
      }).subscribe();
    }
  }



  protected screenWithAi(): void {
    const data = this.appData();
    if (!data.applicationId) return;

    this.screening.set(true);
    this.screenError.set(null);

    const req: import('../../../core/api/recruitment-ai.models').ScreenResumeRequest = {
      companyID: environment.companyId,
      applicationID: data.applicationId,
      resumePath: data.resumeUrl || 'sample_resume.pdf',
      jobRequirements: {
        jobTitle: data.jobTitle || 'Software Engineer',
        requiredSkills: data.skills.length > 0 ? data.skills : ['C#', '.NET', 'SQL Server'],
        experience: data.experienceRequired || '3+ years',
        education: data.education || 'Bachelor Degree',
      },
    };

    this.aiService.screenResume(req).subscribe({
      next: (res) => {
        this.screening.set(false);
        if (res.isSuccess && res.data) {
          this.screenResult.set(res.data);
          if (res.data.shortlisted) {
            this.appData.update((curr) => ({ ...curr, status: 'Shortlisted' }));
          }
        } else {
          this.screenError.set(res.message || 'AI Screening failed to complete.');
        }
      },
      error: (err) => {
        this.screening.set(false);
        this.screenError.set(err.message || 'Network error executing AI Screening.');
      },
    });
  }

  protected getScoreRatingLabel(score: number | null): string {
    if (score == null) return 'Unscreened';
    if (score >= 85) return 'Strong Match';
    if (score >= 70) return 'Good Match';
    if (score >= 50) return 'Moderate Match';
    return 'Weak Match';
  }

  protected getScoreBadgeClass(score: number | null): string {
    if (score == null) return 'bg-slate-800 text-slate-300 border-slate-700';
    if (score >= 85) return 'bg-emerald-500/20 text-emerald-300 border-emerald-500/40';
    if (score >= 70) return 'bg-sky-500/20 text-sky-300 border-sky-500/40';
    if (score >= 50) return 'bg-amber-500/20 text-amber-300 border-amber-500/40';
    return 'bg-rose-500/20 text-rose-300 border-rose-500/40';
  }

  protected getScoreGaugeClass(score: number | null): string {
    if (score == null) return 'bg-slate-800 text-slate-300 border border-slate-700';
    if (score >= 85) return 'bg-emerald-500 text-white shadow-emerald-500/40 shadow-lg';
    if (score >= 70) return 'bg-sky-500 text-white shadow-sky-500/40 shadow-lg';
    if (score >= 50) return 'bg-amber-500 text-white shadow-amber-500/40 shadow-lg';
    return 'bg-rose-500 text-white shadow-rose-500/40 shadow-lg';
  }
}


