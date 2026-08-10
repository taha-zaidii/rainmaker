import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { IconComponent } from '../../../shared/icon.component';
import { RmTableComponent } from '../../../shared/components/ui/table/table.component';
import { RecruitmentService } from '../../../core/api/recruitment.service';
import { JobRequisition } from '../../../core/api/recruitment.models';
import { SessionService } from '../../../core/auth/session.service';
import { environment } from '../../../../environments/environment';

/**
 * Job Requisitions Management.
 *
 * Every row is live from SP_Ruc_JobRequisition_GetAll — nothing here is
 * seeded or sampled. Publishing is the one state change on this screen and it
 * is deliberately explicit: it takes a draft the AI may have written and makes
 * it a public advert, which is a decision the AI never gets to make.
 */
@Component({
  selector: 'rm-job-requisitions',
  standalone: true,
  imports: [IconComponent, FormsModule, RouterLink, DatePipe, RmTableComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './job-requisitions.component.html',
})
export class JobRequisitionsComponent {
  private readonly api = inject(RecruitmentService);
  private readonly session = inject(SessionService);

  protected readonly loading = signal(true);
  protected readonly requisitions = signal<JobRequisition[]>([]);
  protected readonly search = signal('');
  protected readonly filter = signal<'all' | 'published' | 'draft'>('all');

  /** Which row is publishing, so only its own button shows the busy state. */
  protected readonly publishing = signal<number | null>(null);
  protected readonly deletingId = signal<number | null>(null);
  protected readonly message = signal<{ ok: boolean; text: string } | null>(null);

  protected readonly filtered = computed(() => {
    const term = this.search().trim().toLowerCase();
    const mode = this.filter();

    return this.requisitions().filter((r) => {
      if (mode === 'published' && !r.isPublished) return false;
      if (mode === 'draft' && r.isPublished) return false;
      if (!term) return true;

      return [r.jobTitle, r.requisitionCode, r.departmentName, r.location]
        .filter(Boolean)
        .some((v) => v!.toLowerCase().includes(term));
    });
  });

  protected readonly publishedCount = computed(
    () => this.requisitions().filter((r) => r.isPublished).length,
  );

  protected readonly careersUrl = computed(
    () => `/careers?companyId=${environment.companyId}`,
  );

  constructor() {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.api
      .listRequisitions({ companyID: environment.companyId, pageSize: 200 })
      .subscribe((result) => {
        this.requisitions.set(result.requisitions);
        this.loading.set(false);
      });
  }

  protected refresh(): void {
    this.message.set(null);
    this.load();
  }

  /**
   * Publish. On success the row is patched in place rather than the whole
   * list refetched — the grid keeps its scroll position, which matters when
   * someone is working down a long list.
   */
  protected publish(requisition: JobRequisition): void {
    if (requisition.isPublished) {
      return;
    }

    this.publishing.set(requisition.requisitionID);
    this.message.set(null);

    this.api
      .publishRequisition(
        requisition.requisitionID,
        environment.companyId,
        this.session.userName(),
      )
      .subscribe({
        next: (result) => {
          this.publishing.set(null);
          this.requisitions.update((list) =>
            list.map((r) =>
              r.requisitionID === requisition.requisitionID
                ? {
                    ...r,
                    isPublished: result.isPublished,
                    publishedDate: result.publishedDate,
                    statusName: result.statusName ?? 'Published',
                    statusCode: result.statusCode ?? 'PUBLISHED',
                  }
                : r,
            ),
          );
          this.message.set({
            ok: true,
            text: `${requisition.jobTitle} is now live on the careers page.`,
          });
        },
        error: (e) => {
          this.publishing.set(null);
          this.message.set({ ok: false, text: e.message || 'Publishing failed.' });
        },
      });
  }

  protected deleteRequisition(requisition: JobRequisition): void {
    if (!confirm(`Are you sure you want to delete "${requisition.jobTitle}"?`)) {
      return;
    }

    this.deletingId.set(requisition.requisitionID);
    this.message.set(null);

    this.api.deleteRequisition(requisition.requisitionID).subscribe({
      next: (ok) => {
        this.deletingId.set(null);
        if (ok) {
          this.requisitions.update((list) =>
            list.filter((r) => r.requisitionID !== requisition.requisitionID),
          );
          this.message.set({
            ok: true,
            text: `Requisition "${requisition.jobTitle}" was deleted successfully.`,
          });
        } else {
          this.message.set({
            ok: false,
            text: `Failed to delete requisition "${requisition.jobTitle}". It may have active applications.`,
          });
        }
      },
      error: (e) => {
        this.deletingId.set(null);
        this.message.set({
          ok: false,
          text: e?.message || `Error deleting requisition "${requisition.jobTitle}".`,
        });
      },
    });
  }

  protected statusTone(r: JobRequisition): string {
    if (r.isPublished) return 'rm-chip-success';
    if ((r.statusCode ?? '').toUpperCase() === 'PENDING') return 'rm-chip-warning';
    return 'rm-chip-neutral';
  }
}
