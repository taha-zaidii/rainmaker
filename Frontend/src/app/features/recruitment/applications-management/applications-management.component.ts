import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { IconComponent } from '../../../shared/icon.component';
import { RmTableComponent } from '../../../shared/components/ui/table/table.component';
import { RmDrawerComponent } from '../../../shared/components/ui/drawer/drawer.component';
import { RecruitmentService } from '../../../core/api/recruitment.service';
import { RecruitmentAiService } from '../../../core/api/recruitment-ai.service';
import { JobApplication, RecruitmentStatus } from '../../../core/api/recruitment.models';
import { ParsedResume } from '../../../core/api/recruitment-ai.models';
import { environment } from '../../../../environments/environment';

/**
 * Applications Management.
 *
 * The list is live from SP_Ruc_JobApplication_GetAll. The AI action on each
 * row parses THAT candidate's stored CV — there is no sample document and no
 * canned profile, because a parser demoed against a fixed file tells you
 * nothing about how it handles the CVs people actually send.
 */
@Component({
  selector: 'rm-applications-management',
  standalone: true,
  imports: [IconComponent, FormsModule, RouterLink, DatePipe, RmTableComponent, RmDrawerComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './applications-management.component.html',
})
export class ApplicationsManagementComponent {
  private readonly api = inject(RecruitmentService);
  private readonly ai = inject(RecruitmentAiService);

  protected readonly loading = signal(true);
  protected readonly applications = signal<JobApplication[]>([]);
  protected readonly statuses = signal<RecruitmentStatus[]>([]);
  protected readonly search = signal('');
  protected readonly statusFilter = signal<number | null>(null);

  /* ── AI parse drawer ──────────────────────────────────────────────────── */
  protected readonly parsingId = signal<number | null>(null);
  protected readonly deletingId = signal<number | null>(null);
  protected readonly message = signal<{ ok: boolean; text: string } | null>(null);
  protected readonly parsed = signal<ParsedResume | null>(null);
  protected readonly parsedFor = signal<JobApplication | null>(null);
  protected readonly parseError = signal<string | null>(null);

  protected readonly filtered = computed(() => {
    const term = this.search().trim().toLowerCase();
    const status = this.statusFilter();

    return this.applications().filter((a) => {
      if (status !== null && a.currentStatusID !== status) return false;
      if (!term) return true;

      return [a.fullName, a.email, a.requisitionJobTitle, a.applicationCode]
        .filter(Boolean)
        .some((v) => v!.toLowerCase().includes(term));
    });
  });

  /** Counters derived from the same rows the grid shows, never a second query. */
  protected readonly counts = computed(() => {
    const all = this.applications();
    const by = (code: string) =>
      all.filter((a) => (a.statusCode ?? '').toUpperCase() === code).length;

    return {
      total: all.length,
      shortlisted: by('SHORTLISTED'),
      interview: by('INTERVIEW'),
      hired: by('HIRED'),
    };
  });

  constructor() {
    this.load();
    this.api.listStatuses('APPLICATION').subscribe((s) => this.statuses.set(s));
  }

  private load(): void {
    this.loading.set(true);
    this.api
      .listApplications({ companyID: environment.companyId, pageSize: 200 })
      .subscribe((result) => {
        this.applications.set(result.applications);
        this.loading.set(false);
      });
  }

  protected refresh(): void {
    this.parsed.set(null);
    this.parsedFor.set(null);
    this.load();
  }

  /**
   * Runs the AI parser over this application's stored CV.
   *
   * Requires a resume path — an application submitted without a file has
   * nothing to parse, and the button says so rather than failing on click.
   */
  protected parse(application: JobApplication): void {
    if (!application.resumePath) {
      return;
    }

    this.parsingId.set(application.applicationID);
    this.parsed.set(null);
    this.parsedFor.set(application);
    this.parseError.set(null);

    this.ai
      .parseResume({
        companyId: environment.companyId,
        jobRequisitionId: application.requisitionID,
        jobApplicationId: application.applicationID,
        resumeFilePath: application.resumePath,
      })
      .subscribe({
        next: (response) => {
          this.parsingId.set(null);

          if (!response.isSuccess || !response.data) {
            this.parseError.set(response.message || 'The CV could not be parsed.');
            return;
          }

          this.parsed.set(this.normalise(response.data));
        },
        error: (e: Error) => {
          this.parsingId.set(null);
          this.parseError.set(e.message);
        },
      });
  }

  /**
   * The service returns the profile both as typed fields and as raw
   * ProfileSchema JSON, and which one is populated depends on the extraction
   * route it took. Reading both means the drawer renders either way.
   */
  private normalise(data: ParsedResume): ParsedResume {
    let raw: Record<string, unknown> = {};
    try {
      if (typeof data.parsedDataJson === 'string') {
        raw = JSON.parse(data.parsedDataJson) as Record<string, unknown>;
      } else if (data.parsedDataJson) {
        raw = data.parsedDataJson as unknown as Record<string, unknown>;
      }
    } catch {
      // Malformed JSON from the service is not worth failing the drawer over
      // — the typed fields alone still render a usable profile.
      raw = {};
    }

    const pick = <T>(...values: unknown[]): T | null =>
      (values.find((v) => v !== null && v !== undefined && v !== '') as T) ?? null;

    return {
      ...data,
      fullName: pick<string>(data.fullName, data.candidateName, raw['fullName'], raw['full_name'], raw['name']),
      email: pick<string>(data.email, raw['email']),
      phone: pick<string>(data.phone, data.phoneNumber, raw['phone']),
      location: pick<string>(data.location, raw['location']),
      summary: pick<string>(data.summary, raw['summary']),
      skills: data.skills?.length ? data.skills : ((raw['skills'] as string[]) ?? []),
      experience: data.experience?.length
        ? data.experience
        : ((raw['experience'] ?? raw['work_experience']) as ParsedResume['experience']) ?? [],
      education: data.education?.length
        ? data.education
        : ((raw['education'] as ParsedResume['education']) ?? []),
      totalYearsExperience: pick<number>(
        data.totalYearsExperience,
        data.totalExperienceYears,
        raw['total_experience_years'],
      ),
    };
  }

  protected closeDrawer(): void {
    this.parsed.set(null);
    this.parsedFor.set(null);
    this.parseError.set(null);
  }

  protected deleteApplication(application: JobApplication): void {
    if (
      !confirm(
        `Are you sure you want to delete application "${
          application.applicationCode || application.fullName
        }"?`,
      )
    ) {
      return;
    }

    this.deletingId.set(application.applicationID);
    this.message.set(null);

    this.api.deleteApplication(application.applicationID).subscribe({
      next: (ok) => {
        this.deletingId.set(null);
        if (ok) {
          this.applications.update((list) =>
            list.filter((a) => a.applicationID !== application.applicationID),
          );
          if (this.parsedFor()?.applicationID === application.applicationID) {
            this.closeDrawer();
          }
          this.message.set({
            ok: true,
            text: `Application "${
              application.applicationCode || application.fullName
            }" deleted successfully.`,
          });
        } else {
          this.message.set({
            ok: false,
            text: `Failed to delete application "${
              application.applicationCode || application.fullName
            }".`,
          });
        }
      },
      error: (e) => {
        this.deletingId.set(null);
        this.message.set({
          ok: false,
          text:
            e?.message ||
            `Error deleting application "${application.fullName}".`,
        });
      },
    });
  }

  protected shortlist(application: JobApplication): void {
    this.api.shortlistCandidate(application.applicationID, environment.companyId).subscribe({
      next: (ok) => {
        if (ok) {
          this.message.set({
            ok: true,
            text: `Candidate "${application.fullName || application.applicationCode}" shortlisted successfully.`,
          });
          this.load();
        }
      },
      error: (e) => {
        this.message.set({ ok: false, text: e.message || 'Failed to shortlist candidate.' });
      },
    });
  }

  protected reject(application: JobApplication): void {
    this.api.rejectApplication(application.applicationID, environment.companyId).subscribe({
      next: (ok) => {
        if (ok) {
          this.message.set({
            ok: true,
            text: `Application for "${application.fullName || application.applicationCode}" marked as rejected.`,
          });
          this.load();
        }
      },
      error: (e) => {
        this.message.set({ ok: false, text: e.message || 'Failed to reject application.' });
      },
    });
  }


  protected statusTone(a: JobApplication): string {
    switch ((a.statusCode ?? '').toUpperCase()) {
      case 'HIRED':
        return 'rm-chip-success';
      case 'SHORTLISTED':
        return 'rm-chip-success';
      case 'INTERVIEW':
        return 'rm-chip-blue';
      case 'REJECTED':
        return 'rm-chip-danger';
      default:
        return 'rm-chip-neutral';
    }
  }

  protected getRatingLabel(score: number | null): string {
    if (score == null) return 'Unscreened';
    if (score >= 85) return 'Strong Match';
    if (score >= 70) return 'Good Match';
    if (score >= 50) return 'Moderate Match';
    return 'Weak Match';
  }

  /** Screening scores are advisory; the colour is a hint, not a verdict. */
  protected scoreTone(score: number | null): string {
    if (score == null) return 'rm-chip-neutral';
    if (score >= 85) return 'rm-chip-success';
    if (score >= 70) return 'rm-chip-blue';
    if (score >= 50) return 'rm-chip-warning';
    return 'rm-chip-danger';
  }

}
