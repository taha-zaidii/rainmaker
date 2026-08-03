import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';

import { IconComponent, IconName } from '../../../shared/icon.component';
import { RecruitmentAiService } from '../../../core/api/recruitment-ai.service';
import {
  AI_CAPABILITY_LABEL,
  AiProvider,
  DashboardData,
  TestApiKeyResult,
} from '../../../core/api/recruitment-ai.models';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'rm-dashboard',
  standalone: true,
  imports: [IconComponent, RouterLink, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './dashboard.component.html',
})
export class DashboardComponent {
  private readonly api = inject(RecruitmentAiService);

  protected readonly loadingDashboard = signal(true);
  protected readonly dashboard = signal<DashboardData | null>(null);

  protected readonly checking = signal(true);
  protected readonly service = signal<TestApiKeyResult | null>(null);

  /**
   * Built from the real counts. Returns an empty list when the query failed
   * rather than falling back to sample numbers — a dashboard figure gets
   * believed and acted on, so an invented one is worse than a blank.
   */
  protected readonly stats = computed(() => {
    const s = this.dashboard()?.stats;
    if (!s) {
      return [];
    }

    return [
      {
        icon: 'file-text' as IconName,
        label: 'Open Requisitions',
        value: s.activeRequisitions,
      },
      {
        icon: 'user-plus' as IconName,
        label: 'Applications',
        value: s.totalApplications,
      },
      {
        icon: 'calendar' as IconName,
        label: 'Interviews Scheduled',
        value: s.interviewsScheduled,
      },
      { icon: 'check-circle' as IconName, label: 'Hired', value: s.hiredCount },
    ];
  });

  /** AI-specific counters, kept apart from the hiring funnel figures. */
  protected readonly aiStats = computed(() => {
    const s = this.dashboard()?.stats;
    if (!s) {
      return [];
    }

    return [
      { label: 'Job descriptions generated', value: s.totalJobsAnalyzed },
      { label: 'Resumes screened', value: s.resumesScreened },
      { label: 'Candidates matched', value: s.candidatesMatched },
    ];
  });

  protected readonly activity = computed(() => this.dashboard()?.recentActivity ?? []);

  constructor() {
    this.api.getDashboard().subscribe((data) => {
      this.dashboard.set(data);
      this.loadingDashboard.set(false);
    });

    // A live probe, not a decoration: /auth/verify costs no GPU time and
    // answers in milliseconds, so the dashboard can honestly say whether the
    // AI is reachable right now instead of assuming it is.
    this.api.getApiKeySettings().subscribe((settings) => {
      const provider = (settings?.provider?.toLowerCase() as AiProvider) ?? 'multinetai';
      this.api
        .testApiKey({
          companyId: environment.companyId,
          provider,
          apiKey: '',
          apiEndpoint: settings?.apiEndpoint ?? '',
        })
        .subscribe((result) => {
          this.service.set(result);
          this.checking.set(false);
        });
    });
  }

  protected capabilityLabel(slug: string): string {
    return AI_CAPABILITY_LABEL[slug] ?? slug;
  }

  /** Activity rows produced by the AI get the orange treatment. */
  protected isAiActivity(type: string): boolean {
    return ['job_description', 'resume_parsing', 'screening', 'matching', 'scoring'].includes(
      type,
    );
  }
}
