import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';

import { IconComponent } from '../../shared/icon.component';
import { RecruitmentService } from '../../core/api/recruitment.service';
import { RecruitmentAiService } from '../../core/api/recruitment-ai.service';
import { JobRequisition, toLines } from '../../core/api/recruitment.models';
import { ParsedResume } from '../../core/api/recruitment-ai.models';
import { environment } from '../../../environments/environment';

type Stage = 'detail' | 'apply' | 'submitting' | 'done';

/** Which form fields the AI filled, so the UI can mark them honestly. */
type AiField =
  | 'fullName'
  | 'email'
  | 'phone'
  | 'location'
  | 'skills'
  | 'experience'
  | 'currentJobTitle'
  | 'currentCompany'
  | 'experienceSummary'
  | 'education'
  | 'projects';

@Component({
  selector: 'rm-job-detail',
  standalone: true,
  imports: [IconComponent, RouterLink, FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './job-detail.component.html',
})
export class JobDetailComponent {
  private readonly api = inject(RecruitmentService);
  private readonly ai = inject(RecruitmentAiService);
  private readonly route = inject(ActivatedRoute);

  protected readonly loading = signal(true);
  protected readonly job = signal<JobRequisition | null>(null);
  protected readonly stage = signal<Stage>('detail');
  protected readonly error = signal<string | null>(null);

  protected readonly companyId = this.resolveCompanyId();

  /* ── Applicant form ───────────────────────────────────────────────────── */
  protected fullName = signal('');
  protected email = signal('');
  protected phone = signal('');
  protected location = signal('');
  protected currentJobTitle = signal('');
  protected currentCompany = signal('');
  protected totalExperience = signal<number | null>(null);
  protected skills = signal('');
  protected experienceSummary = signal('');
  protected education = signal('');
  protected projects = signal('');
  protected coverLetter = signal('');

  /* ── Resume + parsing ─────────────────────────────────────────────────── */
  protected readonly file = signal<File | null>(null);
  protected readonly resumePath = signal<string | null>(null);
  protected readonly uploading = signal(false);
  protected readonly parsing = signal(false);
  protected readonly parsed = signal<ParsedResume | null>(null);
  protected readonly parseError = signal<string | null>(null);

  /** Fields the parser populated. Drives the orange AI markers. */
  protected readonly aiFilled = signal<Set<AiField>>(new Set());

  protected readonly applicationCode = signal<string | null>(null);

  protected readonly responsibilities = computed(() =>
    toLines(this.job()?.keyResponsibilities),
  );
  protected readonly requirements = computed(() => toLines(this.job()?.requirements));
  protected readonly qualifications = computed(() => toLines(this.job()?.qualifications));
  protected readonly skillList = computed(() => toLines(this.job()?.skills));

  protected readonly canSubmit = computed(
    () => this.fullName().trim().length > 1 && this.email().trim().includes('@'),
  );

  protected readonly departmentLabel = computed(() => {
    const j = this.job();
    if (!j) return 'Engineering & Product';
    return j.departmentName || j.designationName || 'Engineering & Product';
  });

  protected readonly salaryLabel = computed(() => {
    const j = this.job();
    if (!j) return null;
    const { minSalary: min, maxSalary: max } = j;
    if (min == null && max == null) return null;
    if (min != null && max != null) return `PKR ${min.toLocaleString()} - ${max.toLocaleString()} / mo`;
    if (min != null) return `From PKR ${min.toLocaleString()} / mo`;
    return `Up to PKR ${max!.toLocaleString()} / mo`;
  });

  protected readonly publishedDateLabel = computed(() => {
    const j = this.job();
    if (!j || !j.publishedDate) return null;
    const d = new Date(j.publishedDate);
    if (isNaN(d.getTime())) return null;
    return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  });

  protected readonly closingDateLabel = computed(() => {
    const j = this.job();
    if (!j || !j.closingDate) return null;
    const d = new Date(j.closingDate);
    if (isNaN(d.getTime())) return null;
    return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  });

  protected readonly experienceLabel = computed(() => {
    const j = this.job();
    if (!j) return null;
    const { minExperience: min, maxExperience: max } = j;
    if (min == null && max == null) return null;
    if (min != null && max != null) return `${min}–${max} years`;
    if (min != null) return `${min}+ years`;
    return `Up to ${max} years`;
  });


  constructor() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.api.getRequisition(id).subscribe((job) => {
      this.job.set(job);
      this.loading.set(false);
    });

    // Arriving from the "Apply" button on the listing skips the detail read.
    if (history.state?.apply) {
      this.stage.set('apply');
    }
  }

  protected startApply(): void {
    this.stage.set('apply');
    this.error.set(null);
  }

  /* ── Resume: upload, then parse ───────────────────────────────────────── */

  protected onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const chosen = input.files?.[0];
    if (!chosen) {
      return;
    }

    const allowed = ['.pdf', '.doc', '.docx'];
    const ext = chosen.name.slice(chosen.name.lastIndexOf('.')).toLowerCase();
    if (!allowed.includes(ext)) {
      this.parseError.set('Please upload a PDF, DOC or DOCX file.');
      return;
    }
    if (chosen.size > 10 * 1024 * 1024) {
      this.parseError.set('That file is larger than 10 MB.');
      return;
    }

    this.parseError.set(null);
    this.file.set(chosen);
    this.uploadAndParse(chosen);
  }

  private uploadAndParse(chosen: File): void {
    this.uploading.set(true);
    this.parsed.set(null);
    this.clearAiFilled();

    this.api.uploadResume(chosen, this.companyId).subscribe({
      next: (uploaded) => {
        this.uploading.set(false);
        const path = uploaded.relativePath || null;
        this.resumePath.set(path);

        if (!path) {
          this.parseError.set(
            'The file was uploaded but the server did not return a path, so it ' +
              'could not be parsed. Fill the form in manually.',
          );
          return;
        }

        this.parse(path);
      },
      error: (e: Error) => {
        this.uploading.set(false);
        this.parseError.set(e.message);
      },
    });
  }

  private parse(path: string): void {
    this.parsing.set(true);

    this.ai.parseResume({ companyId: this.companyId, resumeFilePath: path }).subscribe({
      next: (response) => {
        this.parsing.set(false);

        if (!response.isSuccess || !response.data) {
          this.parseError.set(
            response.message ||
              'The CV could not be read automatically. Please fill in the form below.',
          );
          return;
        }

        this.applyParsed(response.data);
      },
      error: () => {
        this.parsing.set(false);
        this.parseError.set(
          'The CV could not be read automatically. Please fill in the form below.',
        );
      },
    });
  }

  private applyParsed(data: ParsedResume): void {
    this.parsed.set(data);
    const filled = new Set<AiField>();
    const d = data as Record<string, any>;

    const name = (data.fullName || data.candidateName || d['full_name'] || d['candidate_name'] || d['name'] || '').trim();
    if (name) {
      this.fullName.set(name);
      filled.add('fullName');
    }

    const email = (data.email || d['email_address'] || d['emailAddress'] || '').trim();
    if (email) {
      this.email.set(email);
      filled.add('email');
    }

    const phone = (data.phone || data.phoneNumber || d['phone_number'] || d['mobileNumber'] || d['mobile'] || '').trim();
    if (phone) {
      this.phone.set(phone);
      filled.add('phone');
    }

    const loc = (data.location || d['location'] || d['address'] || d['currentAddress'] || d['city'] || d['country'] || d['preferredLocation'] || '').trim();
    if (loc) {
      this.location.set(loc);
      filled.add('location');
    }

    const rawSkills = data.skills ?? d['skills'] ?? d['skills_list'] ?? d['technical_skills'] ?? d['skillsList'] ?? d['key_skills'] ?? d['keySkills'];
    if (rawSkills) {
      const formattedSkills = Array.isArray(rawSkills) ? rawSkills.join(', ') : String(rawSkills);
      if (formattedSkills.trim()) {
        this.skills.set(formattedSkills.trim());
        filled.add('skills');
      }
    }

    const exp = data.totalYearsExperience ?? data.totalExperienceYears ?? d['total_years_experience'] ?? d['experience_years'] ?? d['experienceYears'] ?? d['totalExperience'];
    if (exp != null && !isNaN(Number(exp))) {
      this.totalExperience.set(Number(exp));
      filled.add('experience');
    }

    const expList = data.experience ?? d['workExperience'] ?? d['work_experience'] ?? d['experiences'] ?? d['experienceList'] ?? [];
    if (Array.isArray(expList) && expList.length) {
      const latest = expList[0] as Record<string, any>;
      const role = (latest['position'] || latest['role'] || latest['jobTitle'] || latest['title'] || latest['job_title'] || '').trim();
      if (role) {
        this.currentJobTitle.set(role);
        filled.add('currentJobTitle');
      }
      const comp = (latest['company'] || latest['organization'] || latest['company_name'] || '').trim();
      if (comp) {
        this.currentCompany.set(comp);
        filled.add('currentCompany');
      }

      const formattedExp = expList.map((e: any) => {
        if (typeof e === 'string') return e;
        const r = e.position || e.role || e.jobTitle || e.title || '';
        const c = e.company || e.organization || '';
        const dur = e.duration || e.year || '';
        const desc = e.description || '';
        let line = [r, c].filter(Boolean).join(' at ');
        if (dur) line += ` (${dur})`;
        if (desc) line += `\n${desc}`;
        return line;
      }).filter(Boolean).join('\n\n');

      if (formattedExp.trim()) {
        const fullSummary = (data.summary || d['summary'] ? `${data.summary || d['summary']}\n\n` : '') + formattedExp.trim();
        this.experienceSummary.set(fullSummary);
        filled.add('experienceSummary');
      }
    } else if (data.summary || d['summary']) {
      this.experienceSummary.set((data.summary || d['summary']).trim());
      filled.add('experienceSummary');
    }


    const eduList = data.education ?? d['education'] ?? d['qualifications'] ?? [];
    if (Array.isArray(eduList) && eduList.length) {
      const formattedEdu = eduList.map((e: any) => {
        const degree = e.degree || e.qualification || '';
        const inst = e.institution || e.university || e.school || '';
        const year = e.year || e.duration || '';
        let line = [degree, inst].filter(Boolean).join(' — ');
        if (year) line += ` (${year})`;
        return line;
      }).filter(Boolean).join('\n');

      if (formattedEdu.trim()) {
        this.education.set(formattedEdu.trim());
        filled.add('education');
      }
    }

    const projList = data.projects ?? d['projects'] ?? d['project_list'] ?? [];
    if (Array.isArray(projList) && projList.length) {
      const formattedProj = projList.map((p: any) => {
        const name = p.name || p.title || '';
        const tech = p.technologies || p.tech_stack || '';
        const desc = p.description || '';
        let line = name;
        if (tech) line += ` [Tech: ${tech}]`;
        if (desc) line += `\n${desc}`;
        return line;
      }).filter(Boolean).join('\n\n');

      if (formattedProj.trim()) {
        this.projects.set(formattedProj.trim());
        filled.add('projects');
      }
    }

    this.aiFilled.set(filled);
  }



  /**
   * Drop what the previous CV's parse contributed, and nothing else.
   *
   * Swapping CVs used to leave the first candidate's details behind:
   * applyParsed deliberately never overwrites a non-empty field, so the second
   * parse was silently discarded and the form still read "Syed Taha Zaidi"
   * while a different person's CV was attached. Submitting that would file an
   * application under the wrong name, email and phone.
   *
   * aiFilled already records which values came from the model rather than from
   * the person, so only those are cleared — anything typed by hand survives,
   * which is the same rule applyParsed applies in the other direction.
   */
  private clearAiFilled(): void {
    const filled = this.aiFilled();

    if (filled.has('fullName')) this.fullName.set('');
    if (filled.has('email')) this.email.set('');
    if (filled.has('phone')) this.phone.set('');
    if (filled.has('location')) this.location.set('');
    if (filled.has('skills')) this.skills.set('');
    if (filled.has('experience')) this.totalExperience.set(null);
    if (filled.has('currentJobTitle')) this.currentJobTitle.set('');
    if (filled.has('currentCompany')) this.currentCompany.set('');
    if (filled.has('experienceSummary')) this.experienceSummary.set('');
    if (filled.has('education')) this.education.set('');
    if (filled.has('projects')) this.projects.set('');

    this.aiFilled.set(new Set());
  }

  protected isAi(field: AiField): boolean {
    return this.aiFilled().has(field);
  }

  /** Editing a field makes it the person's, so the AI marker comes off. */
  protected clearAiMark(field: AiField): void {
    if (!this.aiFilled().has(field)) {
      return;
    }
    this.aiFilled.update((s) => {
      const next = new Set(s);
      next.delete(field);
      return next;
    });
  }

  /* ── Submit ───────────────────────────────────────────────────────────── */

  protected submit(): void {
    const job = this.job();
    if (!job || !this.canSubmit()) {
      return;
    }

    this.stage.set('submitting');
    this.error.set(null);

    const [firstName, ...rest] = this.fullName().trim().split(/\s+/);

    const expSummary = this.experienceSummary().trim() || null;
    const eduSummary = this.education().trim() || null;
    const projSummary = this.projects().trim() ? `Projects:\n${this.projects().trim()}` : '';
    const fullExpSummary = [expSummary, projSummary].filter(Boolean).join('\n\n') || null;

    this.api
      .createApplicant({
        companyID: this.companyId,
        firstName,
        lastName: rest.join(' ') || null,
        email: this.email().trim(),
        mobileNumber: this.phone().trim() || null,
        currentAddress: this.location().trim() || null,
        preferredLocation: this.location().trim() || null,
        currentJobTitle: this.currentJobTitle().trim() || null,
        currentDesignation: this.currentJobTitle().trim() || null,
        currentCompany: this.currentCompany().trim() || null,
        totalExperience: this.totalExperience(),
        experienceYears: this.totalExperience(),
        skills: this.skills().trim() || null,
        experienceSummary: fullExpSummary,
        education: eduSummary,
        resumePath: this.resumePath(),
        coverLetter: this.coverLetter().trim() || null,
      })




      .subscribe({
        next: (applicant) => {
          this.api
            .createApplication({
              companyID: this.companyId,
              requisitionID: job.requisitionID,
              applicantID: applicant.applicantID,
              resumePath: this.resumePath(),
              coverLetter: this.coverLetter().trim() || null,
              // 1 = Careers Page, per SP_Recruitment_GetApplicationSources.
              applicationSourceID: 1,
            })
            .subscribe({
              next: (application) => {
                this.applicationCode.set(application.applicationCode);
                this.stage.set('done');
              },
              error: (e: Error) => {
                this.error.set(e.message);
                this.stage.set('apply');
              },
            });
        },
        error: (e: Error) => {
          this.error.set(e.message);
          this.stage.set('apply');
        },
      });
  }

  private resolveCompanyId(): number {
    const raw = this.route.snapshot.queryParamMap.get('companyId');
    if (!raw) return environment.companyId;

    const n = Number(raw);
    if (Number.isFinite(n) && n > 0) return n;

    try {
      const decoded = Number(atob(raw));
      if (Number.isFinite(decoded) && decoded > 0) return decoded;
    } catch {
      /* not base64 */
    }
    return environment.companyId;
  }
}
