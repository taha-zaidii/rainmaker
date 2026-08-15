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
  templateUrl: './application-details.component.html',
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
    if (score == null) return 'bg-surface-sunken text-ink-muted border border-line';
    if (score >= 85) return 'bg-success-tint text-success border border-success/30';
    if (score >= 70) return 'bg-info-tint text-info border border-info/30';
    if (score >= 50) return 'bg-warning-tint text-warning border border-warning/30';
    return 'bg-danger-tint text-danger border border-danger/30';
  }

  protected getScoreGaugeClass(score: number | null): string {
    if (score == null) return 'bg-surface-sunken text-ink-muted border border-line';
    if (score >= 85) return 'bg-success text-white shadow-md';
    if (score >= 70) return 'bg-info text-white shadow-md';
    if (score >= 50) return 'bg-warning text-white shadow-md';
    return 'bg-danger text-white shadow-md';
  }
}


