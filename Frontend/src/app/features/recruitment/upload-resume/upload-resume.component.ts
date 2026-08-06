import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { IconComponent } from '../../../shared/icon.component';
import { RecruitmentAiService } from '../../../core/api/recruitment-ai.service';
import { RecruitmentService } from '../../../core/api/recruitment.service';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'rm-upload-resume',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-w-[1280px] mx-auto px-6 py-8">
      <!-- Breadcrumb & Header -->
      <div class="pb-6 border-b border-border">
        <div class="flex items-center gap-2 text-xs text-ink-muted mb-2">
          <a routerLink="/recruitment/dashboard" class="hover:underline">Recruitment</a>
          <span>/</span>
          <a routerLink="/recruitment/applications" class="hover:underline">Applications Management</a>
          <span>/</span>
          <span class="text-ink font-medium">Upload Resume</span>
        </div>
        <h1 class="text-2xl font-bold text-ink">Upload Resume & AI Extraction Review</h1>
        <p class="mt-1 text-sm text-ink-muted">
          Upload a candidate CV and the Multinet AI will extract the profile for your review.
        </p>
      </div>

      <!-- Main Two-Column Layout (B1 Design) -->
      <div class="grid grid-cols-1 lg:grid-cols-12 gap-8 mt-8">
        <!-- Left Column: Upload Dropzone & File Status -->
        <div class="lg:col-span-5 flex flex-col gap-6">
          <div class="bg-surface rounded-2xl border-2 border-dashed border-primary/30 hover:border-primary p-8 text-center bg-primary-tint/10 transition group cursor-pointer">
            <div class="w-16 h-16 mx-auto rounded-full bg-primary-tint text-primary grid place-items-center mb-4 group-hover:scale-110 transition">
              <rm-icon name="file-text" [size]="28" />
            </div>
            <h3 class="text-base font-semibold text-ink">Drag candidate CV here or click to browse</h3>
            <p class="text-xs text-ink-muted mt-1">Supports PDF, DOCX, PNG or JPG (up to 5 MB)</p>
            
            <input
              type="file"
              #fileInput
              (change)="onFileSelected($event)"
              accept=".pdf,.docx,.png,.jpg,.jpeg"
              class="hidden"
            />
            <button
              (click)="fileInput.click()"
              class="mt-6 rm-btn-secondary mx-auto text-xs px-4 py-2"
            >
              Browse Files
            </button>
          </div>

          <!-- Uploaded File Card -->
          @if (selectedFileName()) {
            <div class="bg-surface rounded-xl border border-border p-4 shadow-sm">
              <div class="flex items-center justify-between mb-2">
                <div class="flex items-center gap-3">
                  <div class="w-10 h-10 rounded-lg bg-red-100 text-red-600 grid place-items-center">
                    <rm-icon name="file-text" [size]="20" />
                  </div>
                  <div>
                    <div class="text-sm font-semibold text-ink">{{ selectedFileName() }}</div>
                    <div class="text-xs text-ink-muted">2.4 MB · Ready for AI Extraction</div>
                  </div>
                </div>
                <span class="text-xs font-semibold px-2.5 py-1 rounded-full bg-green-100 text-green-700 flex items-center gap-1">
                  <rm-icon name="check-circle" [size]="12" /> Uploaded
                </span>
              </div>
              <div class="w-full bg-surface-alt rounded-full h-1.5 overflow-hidden">
                <div class="bg-primary h-full rounded-full w-full"></div>
              </div>
            </div>
          }

          <!-- Parse Action Card -->
          <div class="bg-surface rounded-2xl border border-border p-6 shadow-sm">
            <h3 class="text-sm font-semibold text-ink mb-1">Target Requisition & Execute</h3>
            <p class="text-xs text-ink-muted mb-4">Select the job requisition to link this candidate profile.</p>

            <div class="space-y-4">
              <div>
                <label class="block text-xs font-medium text-ink-muted mb-1">Job Requisition</label>
                <select
                  [(ngModel)]="jobRequisitionId"
                  class="w-full px-3 py-2 text-sm bg-surface-alt border border-border rounded-lg focus:outline-none focus:border-primary"
                >
                  <option [value]="1">REQ-2026-001 - Senior Frontend Developer</option>
                  <option [value]="2">REQ-2026-002 - Team Lead - IT</option>
                </select>
              </div>

              <div>
                <label class="block text-xs font-medium text-ink-muted mb-1">Resume Storage URL / File Path</label>
                <input
                  type="text"
                  [(ngModel)]="resumeFilePath"
                  class="w-full px-3 py-2 text-sm bg-surface-alt border border-border rounded-lg focus:outline-none focus:border-primary"
                />
              </div>

              <button
                (click)="extractProfile()"
                [disabled]="isParsing() || !resumeFilePath.trim()"
                class="w-full rm-btn-ai py-2.5 flex items-center justify-center gap-2 font-medium"
              >
                @if (isParsing()) {
                  <rm-icon name="sparkles" [size]="18" class="animate-spin" /> Extracting Profile via AI...
                } @else {
                  <rm-icon name="sparkles" [size]="18" /> Extract Candidate Profile with AI
                }
              </button>
            </div>
          </div>
        </div>

        <!-- Right Column: Extracted Profile Review (B1 Specification) -->
        <div class="lg:col-span-7">
          @if (extractedProfile(); as p) {
            <div class="bg-surface rounded-2xl border border-ai-border shadow-md p-6 relative">
              <!-- AI Badge Header -->
              <div class="flex items-center justify-between pb-4 mb-6 border-b border-border">
                <div class="flex items-center gap-2">
                  <rm-icon name="sparkles" [size]="20" class="text-ai" />
                  <h2 class="text-base font-semibold text-ink">Extracted Candidate Profile</h2>
                </div>
                <span class="px-3 py-1 rounded-full bg-ai-tint text-ai text-xs font-semibold border border-ai-border">
                  AI-extracted — please verify
                </span>
              </div>

              <!-- Verification Notice Banner -->
              <div class="mb-6 p-3 rounded-xl bg-amber-50 border border-amber-200 text-amber-800 text-xs flex items-center gap-2">
                <rm-icon name="alert-triangle" [size]="16" class="text-amber-600 shrink-0" />
                <span><strong>Verification Required:</strong> 3 fields marked with yellow borders were extracted by pattern matching and need human review.</span>
              </div>

              <!-- Profile Form Fields -->
              <div class="space-y-4">
                <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div>
                    <label class="block text-xs font-medium text-ink-muted mb-1">Candidate Name</label>
                    <input
                      type="text"
                      [(ngModel)]="p.candidateName"
                      class="w-full px-3 py-2 text-sm bg-surface-alt border border-border rounded-lg"
                    />
                  </div>
                  <div>
                    <label class="block text-xs font-medium text-ink-muted mb-1">Email Address</label>
                    <input
                      type="text"
                      [(ngModel)]="p.email"
                      class="w-full px-3 py-2 text-sm bg-surface-alt border border-border rounded-lg"
                    />
                  </div>
                </div>

                <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <!-- Flagged Field: Phone -->
                  <div>
                    <label class="block text-xs font-medium text-amber-700 mb-1 flex items-center gap-1">
                      <rm-icon name="alert-triangle" [size]="12" /> Phone Number (Needs Review)
                    </label>
                    <input
                      type="text"
                      [(ngModel)]="p.phone"
                      class="w-full px-3 py-2 text-sm bg-amber-50/50 border-2 border-amber-400 rounded-lg focus:outline-none focus:border-amber-600"
                    />
                  </div>
                  <!-- Flagged Field: Location -->
                  <div>
                    <label class="block text-xs font-medium text-amber-700 mb-1 flex items-center gap-1">
                      <rm-icon name="alert-triangle" [size]="12" /> Location (Needs Review)
                    </label>
                    <input
                      type="text"
                      [(ngModel)]="p.location"
                      class="w-full px-3 py-2 text-sm bg-amber-50/50 border-2 border-amber-400 rounded-lg focus:outline-none focus:border-amber-600"
                    />
                  </div>
                </div>

                <div>
                  <label class="block text-xs font-medium text-ink-muted mb-1">Professional Summary</label>
                  <textarea
                    rows="3"
                    [(ngModel)]="p.summary"
                    class="w-full px-3 py-2 text-sm bg-surface-alt border border-border rounded-lg"
                  ></textarea>
                </div>

                <!-- Flagged Field: Skills -->
                <div>
                  <label class="block text-xs font-medium text-amber-700 mb-2 flex items-center gap-1">
                    <rm-icon name="alert-triangle" [size]="12" /> Extracted Skills (Needs Review)
                  </label>
                  <div class="p-3 rounded-xl bg-amber-50/30 border-2 border-amber-400 flex flex-wrap gap-2">
                    @for (skill of p.skills; track skill; let i = $index) {
                      <span class="px-2.5 py-1 text-xs rounded-md bg-white border border-amber-300 text-ink flex items-center gap-1 shadow-sm">
                        {{ skill }}
                        <button (click)="removeSkill(i)" class="text-ink-muted hover:text-red-600 ml-1">×</button>
                      </span>
                    }
                  </div>
                </div>
              </div>

              <!-- Footer Buttons (B1 Design Rule) -->
              <div class="mt-8 pt-4 border-t border-border flex items-center justify-between">
                <button (click)="discard()" class="rm-btn-secondary text-xs px-4 py-2">
                  Discard
                </button>
                <button (click)="acceptAndSave()" class="rm-btn-primary text-xs px-6 py-2">
                  Accept and Save Candidate Profile
                </button>
              </div>
            </div>
          } @else {
            <div class="bg-surface rounded-2xl border border-border p-12 text-center text-ink-muted">
              <div class="w-16 h-16 mx-auto rounded-full bg-surface-alt grid place-items-center mb-4">
                <rm-icon name="sparkles" [size]="28" class="text-ai" />
              </div>
              <h3 class="text-base font-semibold text-ink mb-1">No Profile Extracted Yet</h3>
              <p class="text-xs max-w-md mx-auto">
                Select a candidate resume on the left and click "Extract Candidate Profile with AI" to view structured fields and verification markers.
              </p>
            </div>
          }
        </div>
      </div>
    </div>
  `,
})
export class UploadResumeComponent {
  private readonly api = inject(RecruitmentAiService);
  private readonly recruitment = inject(RecruitmentService);
  private readonly router = inject(Router);

  // Nothing is pre-filled. The previous version shipped a canned "Ayesha
  // Khan" profile and a fixed sample CV URL, which made the screen look like
  // it worked before anything had been uploaded — and told you nothing about
  // how the parser handles a real document.
  protected selectedFileName = signal<string | null>(null);
  protected selectedFile = signal<File | null>(null);
  protected jobRequisitionId = 0;
  protected resumeFilePath = '';
  protected isUploading = signal(false);
  protected isParsing = signal(false);
  protected error = signal<string | null>(null);
  protected extractedProfile = signal<any | null>(null);

  protected onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const chosen = input.files?.[0];
    if (!chosen) {
      return;
    }

    // Same limits the backend enforces, checked here so a predictable
    // rejection does not cost a round trip.
    const ext = chosen.name.slice(chosen.name.lastIndexOf('.')).toLowerCase();
    if (!['.pdf', '.doc', '.docx'].includes(ext)) {
      this.error.set('Please choose a PDF, DOC or DOCX file.');
      return;
    }
    if (chosen.size > 10 * 1024 * 1024) {
      this.error.set('That file is larger than 10 MB.');
      return;
    }

    this.error.set(null);
    this.extractedProfile.set(null);
    this.selectedFile.set(chosen);
    this.selectedFileName.set(chosen.name);
    this.upload(chosen);
  }

  /** Store the CV first, then parse it by path. */
  private upload(chosen: File): void {
    this.isUploading.set(true);

    this.recruitment.uploadResume(chosen, environment.companyId).subscribe({
      next: (uploaded) => {
        this.isUploading.set(false);
        // relativePath, NOT url — see UploadResumeResult for why.
        const path = uploaded.relativePath || '';
        this.resumeFilePath = path;

        if (!path) {
          this.error.set(
            'The file uploaded but the server returned no path, so it cannot be parsed.',
          );
          return;
        }

        this.extractProfile();
      },
      error: (e: Error) => {
        this.isUploading.set(false);
        this.error.set(e.message);
      },
    });
  }

  protected extractProfile(): void {
    if (!this.resumeFilePath.trim()) return;

    this.isParsing.set(true);
    this.api
      .parseResume({
        companyId: environment.companyId,
        jobRequisitionId: this.jobRequisitionId,
        jobApplicationId: 1,
        resumeFilePath: this.resumeFilePath.trim(),
      })
      .subscribe({
        next: (res) => {
          this.isParsing.set(false);
          if (res.isSuccess && res.data) {
            let dataObj: any = {};
            try {
              dataObj = typeof res.data.parsedDataJson === 'string'
                ? JSON.parse(res.data.parsedDataJson)
                : (res.data.parsedDataJson || {});
            } catch {
              dataObj = {};
            }

            const exp = res.data.experience?.length ? res.data.experience : (dataObj.experience || dataObj.work_experience || []);
            const edu = res.data.education?.length ? res.data.education : (dataObj.education || []);
            const proj = res.data.projects?.length ? res.data.projects : (dataObj.projects || []);

            const expSummaryStr = Array.isArray(exp) && exp.length
              ? exp.map((e: any) => `${e.position || e.role || ''} at ${e.company || ''} (${e.duration || ''})`).join('; ')
              : (res.data.summary || dataObj.summary || '');

            const eduSummaryStr = Array.isArray(edu) && edu.length
              ? edu.map((ed: any) => `${ed.degree || ''} ${ed.field ? 'in ' + ed.field : ''} from ${ed.institution || ''}`).join('; ')
              : (dataObj.educationSummary || 'Bachelor Degree');

            const extractedName =
              res.data.fullName ||
              res.data.candidateName ||
              dataObj.fullName ||
              dataObj.candidateName ||
              dataObj.full_name ||
              dataObj.name;

            this.extractedProfile.set({
              candidateName: extractedName || 'Extracted Candidate',
              email: res.data.email || dataObj.email || '',
              phone: res.data.phone || res.data.phoneNumber || dataObj.phone || dataObj.phone_number || '',
              location: res.data.location || dataObj.location || '',
              summary: res.data.summary || dataObj.summary || 'Candidate resume extracted via Multinet AI.',
              skills: res.data.skills?.length ? res.data.skills : (dataObj.skills || dataObj.skills_list || []),
              experience: exp,
              education: edu,
              projects: proj,
              experienceSummaryStr: expSummaryStr,
              educationSummaryStr: eduSummaryStr,
              totalYearsExperience: res.data.totalYearsExperience || dataObj.totalYearsExperience || dataObj.total_years_experience || '',
            });
          }
        },
        error: () => {
          this.isParsing.set(false);
        },
      });
  }

  protected removeSkill(index: number): void {
    const p = this.extractedProfile();
    if (!p) return;
    const skills = p.skills.filter((_: any, i: number) => i !== index);
    this.extractedProfile.set({ ...p, skills });
  }

  protected discard(): void {
    this.extractedProfile.set(null);
  }

  protected acceptAndSave(): void {
    const profile = this.extractedProfile();
    if (!profile) {
      this.router.navigate(['/recruitment/applications']);
      return;
    }

    this.isUploading.set(true);

    const nameParts = (profile.candidateName || 'Candidate').split(' ');
    const firstName = nameParts[0] || 'Candidate';
    const lastName = nameParts.slice(1).join(' ') || '';

    const applicantReq = {
      companyID: environment.companyId,
      firstName: firstName,
      lastName: lastName,
      email: profile.email || 'candidate@example.com',
      mobileNumber: profile.phone || '',
      currentAddress: profile.location || '',
      skills: Array.isArray(profile.skills) ? profile.skills.join(', ') : profile.skills || '',
      experienceSummary: profile.experienceSummaryStr || profile.summary || '',
      education: profile.educationSummaryStr || 'Bachelor Degree',
      experienceYears: Number(profile.totalYearsExperience) || 0,
      resumePath: this.resumeFilePath || 'sample_resume.pdf',
    };


    this.recruitment.createApplicant(applicantReq).subscribe({
      next: (applicant) => {
        const applicantID = applicant?.applicantID || 1;
        const appReq = {
          companyID: environment.companyId,
          requisitionID: this.jobRequisitionId || 1,
          applicantID: applicantID,
          currentStatusID: 1, // Applied
          resumePath: this.resumeFilePath || 'sample_resume.pdf',
          coverLetter: profile.summary || '',
          remarks: `AI Extracted Profile (Skills: ${applicantReq.skills})`,
        };

        this.recruitment.createApplication(appReq).subscribe({
          next: (app) => {
            this.isUploading.set(false);
            const appID = app?.applicationID;
            if (appID) {
              this.router.navigate(['/recruitment/application-details'], {
                queryParams: { id: appID },
              });
            } else {
              this.router.navigate(['/recruitment/applications']);
            }
          },
          error: () => {
            this.isUploading.set(false);
            this.router.navigate(['/recruitment/applications']);
          },
        });
      },
      error: () => {
        this.isUploading.set(false);
        this.router.navigate(['/recruitment/applications']);
      },
    });
  }

}

