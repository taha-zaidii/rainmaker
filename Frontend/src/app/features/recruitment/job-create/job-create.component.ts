import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { IconComponent } from '../../../shared/icon.component';
import { RecruitmentAiService } from '../../../core/api/recruitment-ai.service';
import {
  AiJobDraft,
  GenerateJobDescriptionResult,
} from '../../../core/api/recruitment-ai.models';
import { environment } from '../../../../environments/environment';

type Stage = 'mode' | 'ai-form' | 'generating' | 'wizard';

/**
 * The Job Category dropdown's real values.
 *
 * These are sent to the AI so it snaps its answer to something the dropdown
 * can actually bind. Without them the service returns free text and the
 * select silently fails to match. Replace with the live lookup once the
 * category table is available.
 */
const JOB_CATEGORY_OPTIONS = [
  'UI/UX',
  'Laravel Developer',
  'Ai Developer',
  'Dot Net Developer',
  'Python Developer',
  'Odoo Consultant',
  'Network Engineer',
];

const DEPARTMENTS = [
  'Information Technology',
  'Human Resource',
  'Finance',
  'Quality Assurance',
  'Procurement',
];

const DESIGNATIONS = [
  'System Administrator',
  'Software Engineer',
  'Senior Software Engineer',
  'Team Lead',
  'Manager',
];

const EMPLOYMENT_TYPES = ['Permanent', 'Contract', 'Internship', 'Part-time'];
const GRADES = ['Assistant', 'Officer', 'Sr. Officer', 'Manager', 'Sr. Manager'];

@Component({
  selector: 'rm-job-create',
  standalone: true,
  imports: [FormsModule, IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './job-create.component.html',
})
export class JobCreateComponent {
  private readonly api = inject(RecruitmentAiService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly jobCategoryOptions = JOB_CATEGORY_OPTIONS;
  protected readonly departments = DEPARTMENTS;
  protected readonly designations = DESIGNATIONS;
  protected readonly employmentTypes = EMPLOYMENT_TYPES;
  protected readonly grades = GRADES;

  protected stage = signal<Stage>('mode');
  protected step = signal(1);

  /** Typed here rather than inline in the template so the keys are checked
   *  against the draft shape instead of falling back to `any`. */
  protected readonly listGroups: {
    key: 'keyResponsibilities' | 'requirements' | 'qualifications';
    label: string;
  }[] = [
    { key: 'keyResponsibilities', label: 'Key Responsibilities' },
    { key: 'requirements', label: 'Requirements' },
    { key: 'qualifications', label: 'Qualifications' },
  ];

  /* ── AI generator inputs ──────────────────────────────────────────────── */
  protected jobTitle = signal('');
  protected department = signal('');
  protected designation = signal('');
  protected experience = signal('');
  protected jobCategory = signal('');
  protected keySkills = signal('');

  /* ── Result ───────────────────────────────────────────────────────────── */
  protected draft = signal<AiJobDraft | null>(null);
  protected meta = signal<GenerateJobDescriptionResult | null>(null);
  protected error = signal<string | null>(null);
  protected isSaving = signal(false);
  protected bannerDismissed = signal(false);
  protected elapsed = signal(0);

  private timer?: ReturnType<typeof setInterval>;

  /** jobTitle is the service's only hard requirement. */
  protected readonly canGenerate = computed(() => this.jobTitle().trim().length > 1);

  protected readonly elapsedLabel = computed(() => {
    const s = this.elapsed();
    return `${Math.floor(s / 60)}:${String(s % 60).padStart(2, '0')}`;
  });

  /**
   * Which of the three generating phases to highlight. Purely presentational —
   * the service does not stream progress — but a 30-second wait with no
   * movement reads as a hung page, and the phases are honest about what is
   * happening.
   */
  protected readonly phase = computed(() => {
    const s = this.elapsed();
    return s < 4 ? 0 : s < 12 ? 1 : 2;
  });

  constructor() {
    this.destroyRef.onDestroy(() => this.stopTimer());
  }

  protected startAi(): void {
    this.stage.set('ai-form');
  }

  protected startManual(): void {
    this.draft.set(null);
    this.meta.set(null);
    this.stage.set('wizard');
    this.step.set(1);
  }

  protected generate(): void {
    if (!this.canGenerate()) {
      return;
    }

    this.error.set(null);
    this.stage.set('generating');
    this.elapsed.set(0);
    this.timer = setInterval(() => this.elapsed.update((v) => v + 1), 1000);

    this.api
      .generateJobDescription({
        companyId: environment.companyId,
        jobTitle: this.jobTitle().trim(),
        department: this.department() || null,
        designation: this.designation() || null,
        experience: this.experience() || null,
        skills: this.keySkills() || null,
        // Always send the real options so the answer binds to the dropdown.
        jobCategoryOptions: this.jobCategoryOptions,
      })
      .subscribe({
        next: (result) => {
          this.stopTimer();
          this.meta.set(result);
          this.draft.set(result.draft);
          this.bannerDismissed.set(false);
          this.stage.set('wizard');
          this.step.set(1);
        },
        error: (e: Error) => {
          this.stopTimer();
          this.error.set(e.message);
          this.stage.set('ai-form');
        },
      });
  }

  protected saveRequisition(): void {
    const d = this.draft();
    if (!d) return;

    this.isSaving.set(true);
    this.error.set(null);

    const payload = {
      companyId: environment.companyId,
      jobTitle: d.basicInfo.jobTitle,
      jobDescription: d.basicInfo.jobSummary || d.basicInfo.jobTitle,
      department: d.basicInfo.department,
      designation: d.basicInfo.designation,
      employmentType: d.basicInfo.employmentType,
      grade: d.basicInfo.grade,
      vacancies: d.basicInfo.vacancies,
      experience: d.requirements.experienceYears
        ? `${d.requirements.experienceYears.minimum ?? ''}-${d.requirements.experienceYears.maximum ?? ''}`
        : null,
      skills: d.requirements.skills?.join(', '),
      qualifications: d.requirements.qualifications?.join('\n'),
      keyResponsibilities: d.requirements.keyResponsibilities?.join('\n'),
      requirements: d.requirements.requirements?.join('\n'),
      benefits: d.compensation.benefits,
      justification: d.publishing.justification,
      closingDate: d.publishing.closingDate,
      isPublished: true,
      status: 'Published'
    };

    this.api.saveJobDescription(payload).subscribe({
      next: (res) => {
        this.isSaving.set(false);
        if (res.isSuccess) {
          this.router.navigate(['/recruitment/applications']);
        } else {
          this.error.set(res.message || 'Failed to save job requisition');
        }
      },
      error: (err) => {
        this.isSaving.set(false);
        this.error.set(err.message || 'Error saving job requisition');
      }
    });
  }

  protected regenerate(): void {
    this.stage.set('ai-form');
  }

  protected goToStep(n: number): void {
    if (n >= 1 && n <= 4) {
      this.step.set(n);
    }
  }

  /* ── Draft mutation ───────────────────────────────────────────────────── */
  // The whole point of the draft is that a person edits it, so every write
  // goes through here rather than binding straight to the response object.

  protected patchBasic<K extends keyof AiJobDraft['basicInfo']>(
    key: K,
    value: AiJobDraft['basicInfo'][K],
  ): void {
    this.draft.update((d) =>
      d ? { ...d, basicInfo: { ...d.basicInfo, [key]: value } } : d,
    );
  }

  protected patchList(
    key: 'keyResponsibilities' | 'requirements' | 'qualifications' | 'skills',
    index: number,
    value: string,
  ): void {
    this.draft.update((d) => {
      if (!d) return d;
      const list = [...d.requirements[key]];
      list[index] = value;
      return { ...d, requirements: { ...d.requirements, [key]: list } };
    });
  }

  protected addListItem(
    key: 'keyResponsibilities' | 'requirements' | 'qualifications' | 'skills',
  ): void {
    this.draft.update((d) =>
      d
        ? { ...d, requirements: { ...d.requirements, [key]: [...d.requirements[key], ''] } }
        : d,
    );
  }

  protected removeListItem(
    key: 'keyResponsibilities' | 'requirements' | 'qualifications' | 'skills',
    index: number,
  ): void {
    this.draft.update((d) => {
      if (!d) return d;
      const list = d.requirements[key].filter((_, i) => i !== index);
      return { ...d, requirements: { ...d.requirements, [key]: list } };
    });
  }

  protected patchExperienceYears(which: 'minimum' | 'maximum', value: number | null): void {
    this.draft.update((d) => {
      if (!d) return d;
      const current = d.requirements.experienceYears ?? { minimum: null, maximum: null };
      return {
        ...d,
        requirements: {
          ...d.requirements,
          experienceYears: { ...current, [which]: value },
        },
      };
    });
  }

  protected patchCompensation<K extends keyof AiJobDraft['compensation']>(
    key: K,
    value: AiJobDraft['compensation'][K],
  ): void {
    this.draft.update((d) =>
      d ? { ...d, compensation: { ...d.compensation, [key]: value } } : d,
    );
  }

  protected patchPublishing<K extends keyof AiJobDraft['publishing']>(
    key: K,
    value: AiJobDraft['publishing'][K],
  ): void {
    this.draft.update((d) =>
      d ? { ...d, publishing: { ...d.publishing, [key]: value } } : d,
    );
  }

  private stopTimer(): void {
    if (this.timer) {
      clearInterval(this.timer);
      this.timer = undefined;
    }
  }
}
