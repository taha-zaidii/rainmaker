import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';

import { IconComponent } from '../../../shared/icon.component';
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
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-6 border-b border-border-light">
        <div>
          <a
            routerLink="/recruitment/applications"
            class="inline-flex items-center gap-1.5 text-xs font-semibold text-primary bg-primary-tint/60 hover:bg-primary-tint px-3 py-1.5 rounded-lg mb-3 transition"
          >
            ← Back to Applications
          </a>
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-xl bg-surface-alt border border-border grid place-items-center text-ink">
              <rm-icon name="file-text" [size]="20" />
            </div>
            <div>
              <h1 class="text-xl font-bold text-ink">Application Details</h1>
              <p class="text-xs text-ink-muted">Application Code: <span class="font-semibold text-ink">{{ appData().applicationCode }}</span></p>
            </div>
          </div>
        </div>

        <div class="flex items-center gap-3">
          <button
            (click)="updateStatus('Shortlisted')"
            class="px-4 py-2 text-xs font-semibold rounded-lg border border-green-600 text-green-700 hover:bg-green-50 flex items-center gap-1.5 transition"
          >
            <rm-icon name="user-plus" [size]="14" /> Shortlist Candidate
          </button>
          <button
            (click)="updateStatus('Rejected')"
            class="px-4 py-2 text-xs font-semibold rounded-lg border border-red-300 text-red-600 hover:bg-red-50 flex items-center gap-1.5 transition"
          >
            <rm-icon name="alert-triangle" [size]="14" /> Reject
          </button>
        </div>
      </div>

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
  private readonly sanitizer = inject(DomSanitizer);

  protected activeTab = signal<'details' | 'resume' | 'timeline'>('details');

  protected appData = signal<ApplicationDetailModel>({
    applicationId: 84,
    applicationCode: 'APP000027',
    appliedDate: 'Aug 3, 2026, 11:26:19 AM',
    status: 'Applied',

    fullName: 'Dominic Alvarez',
    email: 'd.alvarez@email.com',
    mobileNumber: '+1 (555) 392-0184',
    location: 'Austin, TX',

    jobTitle: 'Senior Cybersecurity Analyst',
    department: 'Information Technology',
    requisitionCode: 'REQ-000013',
    experienceRequired: '3-5 years',

    currentDesignation: 'Senior Security Analyst',
    currentCompany: 'CYBERSHIELD SOLUTIONS',
    experienceYears: '5 years',
    expectedSalary: 'Not Specified',
    preferredLocation: 'Austin, TX / Remote',
    noticePeriod: '1 Month',

    skills: [
      'Threat Modeling',
      'Incident Response',
      'Compliance Audit',
      'Risk Mitigation',
      'SOAR Playbooks',
      'SIEM / Sentinel',
      'Cybersecurity',
    ],
    education: 'BS in Cybersecurity & Information Assurance',
    dateOfBirth: 'N/A',
    currentAddress: 'Austin, TX, USA',
    experienceSummary:
      'Dedicated Cybersecurity professional with over 5 years of experience in protecting enterprise-level infrastructure. Proven track record in rapid incident response and vulnerability management.',
    coverLetter:
      'I am writing to express my strong interest in the Senior Cybersecurity Analyst position at Rainmaker HRMS.',
    resumeUrl:
      'https://stagginghrms.rainmaker.pk/storage/DigisoftTransformationSolutions/recruitment/resumes/documents/Dominic_Alvarez_%E2%80%94_Cybersecurity_Analyst_20260803_112532_1ba7b6e6.pdf',
  });

  protected safeResumeUrl(): SafeResourceUrl {
    return this.sanitizer.bypassSecurityTrustResourceUrl(this.appData().resumeUrl);
  }

  constructor() {
    this.route.queryParams.subscribe((params) => {
      const name = params['name'];
      const email = params['email'];
      const phone = params['phone'];
      const jobTitle = params['jobTitle'];
      const skillsStr = params['skills'];
      const resumeUrl = params['resumeUrl'];

      if (name || email || resumeUrl) {
        this.appData.update((curr) => ({
          ...curr,
          fullName: name || curr.fullName,
          email: email || curr.email,
          mobileNumber: phone || curr.mobileNumber,
          jobTitle: jobTitle || curr.jobTitle,
          skills: skillsStr ? skillsStr.split(',') : curr.skills,
          resumeUrl: resumeUrl || curr.resumeUrl,
        }));
      }
    });
  }

  protected updateStatus(newStatus: string): void {
    this.appData.update((curr) => ({ ...curr, status: newStatus }));
  }
}
