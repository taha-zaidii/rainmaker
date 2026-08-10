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
  templateUrl: './upload-resume.component.html',
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

