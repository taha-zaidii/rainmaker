import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';

import { IconComponent } from '../../shared/icon.component';
import { RecruitmentService } from '../../core/api/recruitment.service';
import { JobRequisition } from '../../core/api/recruitment.models';
import { environment } from '../../../environments/environment';

/**
 * The public careers page.
 *
 * Deliberately OUTSIDE the portal shell — no sidebar, no top bar, no auth.
 * This is the one surface anonymous internet traffic reaches, and it should
 * read as a company careers site rather than a fragment of an internal ERP.
 *
 * The company is taken from the query string exactly as the live portal does
 * (`?companyId=MTMz`, base64 of the id) so existing links keep working. A
 * plain numeric id is accepted too, because a base64 query parameter is
 * obfuscation rather than security and hand-written links are inevitable.
 */
@Component({
  selector: 'rm-careers',
  standalone: true,
  imports: [IconComponent, RouterLink, FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './careers.component.html',
})
export class CareersComponent {
  private readonly api = inject(RecruitmentService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly loading = signal(true);
  protected readonly jobs = signal<JobRequisition[]>([]);
  protected readonly search = signal('');
  protected readonly view = signal<'grid' | 'list'>('grid');

  protected readonly companyId = this.resolveCompanyId();

  /** Client-side filter: the feed is small and this keeps typing instant. */
  protected readonly filtered = computed(() => {
    const term = this.search().trim().toLowerCase();
    if (!term) {
      return this.jobs();
    }

    return this.jobs().filter((j) =>
      [j.jobTitle, j.departmentName, j.location, j.skills, j.jobSummary]
        .filter(Boolean)
        .some((v) => v!.toLowerCase().includes(term)),
    );
  });

  constructor() {
    this.api.listPublicRequisitions(this.companyId).subscribe((jobs) => {
      this.jobs.set(jobs);
      this.loading.set(false);
    });
  }

  protected apply(job: JobRequisition): void {
    this.router.navigate(['/careers/job', job.requisitionID], {
      queryParams: { companyId: this.route.snapshot.queryParamMap.get('companyId') },
      state: { apply: true },
    });
  }

  /** "3 days ago" reads better than a date on a job board. */
  protected postedAgo(job: JobRequisition): string {
    const raw = job.publishedDate ?? job.createdOn;
    if (!raw) {
      return '';
    }

    const days = Math.floor((Date.now() - new Date(raw).getTime()) / 86_400_000);
    if (days <= 0) return 'Posted today';
    if (days === 1) return 'Posted yesterday';
    if (days < 30) return `Posted ${days} days ago`;
    const months = Math.floor(days / 30);
    return `Posted ${months} month${months > 1 ? 's' : ''} ago`;
  }

  protected skillsPreview(job: JobRequisition): string[] {
    if (!job.skills) return [];
    return job.skills
      .split(/[\n,;]/)
      .map((s) => s.replace(/^[-•*]\s*/, '').trim())
      .filter(Boolean)
      .slice(0, 5);
  }

  protected closingText(job: JobRequisition): string | null {
    if (!job.closingDate) return null;
    const d = new Date(job.closingDate);
    if (isNaN(d.getTime())) return null;
    return `Closes ${d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}`;
  }

  protected salaryText(job: JobRequisition): string | null {
    const { minSalary: min, maxSalary: max } = job;
    if (min == null && max == null) return null;
    if (min != null && max != null) return `PKR ${min.toLocaleString()} - ${max.toLocaleString()}`;
    if (min != null) return `From PKR ${min.toLocaleString()}`;
    return `Up to PKR ${max!.toLocaleString()}`;
  }

  protected departmentLabel(job: JobRequisition): string {
    return job.departmentName || job.designationName || 'Engineering & Product';
  }

  protected experienceLabel(job: JobRequisition): string | null {
    const { minExperience: min, maxExperience: max } = job;
    if (min == null && max == null) return null;
    if (min != null && max != null) return `${min}-${max} yrs exp`;
    if (min != null) return `${min}+ yrs exp`;
    return `Up to ${max} yrs exp`;
  }


  /**
   * Accepts `?companyId=MTMz` (base64, as the live portal emits) or a plain
   * number. Falls back to the configured tenant so the page is never blank
   * just because someone dropped the query string.
   */
  private resolveCompanyId(): number {
    const raw = this.route.snapshot.queryParamMap.get('companyId');
    if (!raw) {
      return environment.companyId;
    }

    const asNumber = Number(raw);
    if (Number.isFinite(asNumber) && asNumber > 0) {
      return asNumber;
    }

    try {
      const decoded = Number(atob(raw));
      if (Number.isFinite(decoded) && decoded > 0) {
        return decoded;
      }
    } catch {
      // Not base64 either — fall through.
    }

    return environment.companyId;
  }
}
