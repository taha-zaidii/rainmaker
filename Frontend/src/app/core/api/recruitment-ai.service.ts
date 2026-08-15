import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, of, throwError, timeout } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  ApiKeySettings,
  ApiKeyStatus,
  ApiResponse,
  DashboardData,
  FeatureSettings,
  GenerateJobDescriptionRequest,
  GenerateJobDescriptionResult,
  SaveApiKeySettingsRequest,
  TestApiKeyRequest,
  ParsedResume,
  TestApiKeyResult,
  ScreenResumeRequest,
  ScreenResumeResponse,
} from './recruitment-ai.models';


/**
 * The one place the recruitment AI endpoints are called.
 *
 * Two decisions worth knowing about:
 *
 * 1. Failures come back as VALUES, not thrown errors, wherever the failure is
 *    an expected business outcome. A rejected API key is not exceptional — it
 *    is the answer to "is this key valid?" — and the screens need the payload
 *    that came with it. The backend deliberately returns a populated body on
 *    those 400s for exactly this reason.
 *
 * 2. Generation calls get a 180 s timeout. The AI service runs a large local
 *    model on a single GPU: ~13 s warm, up to ~35 s cold, and it queues. A
 *    default browser-ish timeout would abort calls that were about to succeed.
 */
@Injectable({ providedIn: 'root' })
export class RecruitmentAiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/recruitment/api/RecruitmentAI`;

  /* ── Settings ─────────────────────────────────────────────────────────── */

  getApiKeyStatus(companyId = environment.companyId): Observable<ApiKeyStatus> {
    return this.http
      .get<ApiResponse<ApiKeyStatus>>(`${this.base}/CheckApiKeyStatus/${companyId}`)
      .pipe(
        map((r) => r.data ?? { hasApiKey: false, provider: null, isValid: false }),
        catchError(() => of({ hasApiKey: false, provider: null, isValid: false })),
      );
  }

  /** Returns null when no settings row exists yet — a normal first-run state. */
  getApiKeySettings(
    companyId = environment.companyId,
  ): Observable<ApiKeySettings | null> {
    return this.http
      .get<ApiResponse<ApiKeySettings>>(`${this.base}/GetApiKeySettings/${companyId}`)
      .pipe(
        map((r) => r.data),
        catchError(() => of(null)),
      );
  }

  saveApiKeySettings(
    request: SaveApiKeySettingsRequest,
  ): Observable<ApiResponse<unknown>> {
    return this.http
      .post<ApiResponse<unknown>>(`${this.base}/SaveApiKeySettings`, request)
      .pipe(catchError((e: HttpErrorResponse) => of(this.envelopeFrom(e))));
  }

  /**
   * Tests the key against the provider. Costs no GPU time for MultinetAI, so
   * it is safe to call on every save.
   *
   * Never throws on a 400: the body carries `status`, which is the entire
   * point — it says whether the key was rejected, the service was unreachable,
   * or the endpoint is simply misconfigured.
   */
  testApiKey(request: TestApiKeyRequest): Observable<TestApiKeyResult> {
    return this.http
      .post<ApiResponse<TestApiKeyResult>>(`${this.base}/TestApiKey`, request)
      .pipe(
        timeout(60_000),
        map((r) => r.data ?? this.unknownTestResult(request)),
        catchError((e: HttpErrorResponse) => {
          const body = e.error as ApiResponse<TestApiKeyResult> | undefined;
          if (body?.data) {
            return of(body.data);
          }

          // No usable body: the request never reached the backend, or it fell
          // over before it could answer. Either way we cannot claim anything
          // about the key itself.
          return of({
            ...this.unknownTestResult(request),
            status: 'unreachable' as const,
            error:
              e.status === 0
                ? 'Could not reach the Rainmaker backend. Is it running on ' +
                  `${environment.apiBaseUrl}?`
                : body?.message ?? 'The connection test could not be completed.',
          });
        }),
      );
  }

  /** Soft-deletes the stored key (IsActive = 0). Settings survive. */
  deleteApiKey(companyId = environment.companyId): Observable<ApiResponse<unknown>> {
    return this.http
      .delete<ApiResponse<unknown>>(`${this.base}/DeleteApiKey/${companyId}`)
      .pipe(catchError((e: HttpErrorResponse) => of(this.envelopeFrom(e))));
  }

  /**
   * Saves ONLY the feature toggles (auto-screening, auto-matching, auto-parse,
   * generate-questions, email-notifications, auto-shortlist threshold).
   *
   * Deliberately separate from saveApiKeySettings: this endpoint never touches
   * Provider/ApiKey/ApiEndpoint/Model, so calling it never requires the user to
   * re-enter the API key. The backend's SP_Ruc_RecruitmentAI_FeatureSettings_Save
   * will return an error if no API key row exists yet (the key must be saved first).
   */
  saveFeatureSettings(
    companyId: number,
    features: FeatureSettings,
    autoShortlistThreshold: number,
  ): Observable<ApiResponse<unknown>> {
    return this.http
      .post<ApiResponse<unknown>>(`${this.base}/SaveSettings`, {
        companyId,
        settings: {
          autoScreening: features.autoScreening,
          autoMatching: features.autoMatching,
          generateQuestions: features.generateQuestions,
          emailNotifications: features.emailNotifications,
          autoParse: features.autoParse,
          autoShortlistThreshold,
        },
      })
      .pipe(catchError((e: HttpErrorResponse) => of(this.envelopeFrom(e))));
  }

  /* ── Dashboard ────────────────────────────────────────────────────────── */

  /**
   * Real counts and the real activity feed, straight from the database.
   * Returns null rather than fabricating figures when the query fails — an
   * invented number on a dashboard is worse than an empty one, because it
   * gets believed.
   */
  getDashboard(companyId = environment.companyId): Observable<DashboardData | null> {
    return this.http
      .get<ApiResponse<DashboardData>>(`${this.base}/GetDashboardStats/${companyId}`)
      .pipe(
        map((r) => r.data),
        catchError(() => of(null)),
      );
  }

  /* ── Job description generation ───────────────────────────────────────── */

  generateJobDescription(
    request: GenerateJobDescriptionRequest,
  ): Observable<GenerateJobDescriptionResult> {
    return this.http
      .post<ApiResponse<GenerateJobDescriptionResult>>(
        `${this.base}/GenerateJobDescription`,
        request,
      )
      .pipe(
        timeout(environment.aiRequestTimeoutMs),
        map((r) => {
          if (!r.isSuccess || !r.data) {
            throw new Error(r.message || 'The AI service did not return a draft.');
          }
          return r.data;
        }),
        catchError((e: unknown) => throwError(() => this.readableError(e))),
      );
  }

  saveJobDescription(request: any): Observable<ApiResponse<any>> {
    return this.http
      .post<ApiResponse<any>>(`${this.base}/SaveJobDescription`, request)
      .pipe(catchError((e: HttpErrorResponse) => of(this.envelopeFrom(e))));
  }

  /**
   * Runs the AI parser over an already-uploaded CV.
   *
   * Takes a PATH, not bytes: the file is uploaded once and referenced
   * afterwards, so a re-parse costs no second upload. Parsing holds the
   * service's GPU lock and can take 40-90 s, hence the long timeout.
   */
  parseResume(payload: {
    companyId?: number;
    jobRequisitionId?: number;
    jobApplicationId?: number;
    resumeFilePath: string;
    /** True only when this call happened without a person explicitly asking
     *  for it right now (the careers apply form parses on upload, before
     *  the candidate submits anything) — lets the backend honour the
     *  company's "Auto Resume Parse" setting for that case specifically,
     *  without blocking a recruiter's own manual "Extract with AI" click
     *  elsewhere in the portal, which should always work regardless of
     *  that setting. Omit for every manually-triggered call. */
    isAutoProcessed?: boolean;
  }): Observable<ApiResponse<ParsedResume>> {
    const companyId = payload.companyId ?? environment.companyId;
    return this.http
      .post<ApiResponse<ParsedResume>>(`${this.base}/ParseResume`, {
        companyId: companyId,
        companyID: companyId,
        jobRequisitionId: payload.jobRequisitionId ?? 1,
        jobApplicationId: payload.jobApplicationId ?? 1,
        applicationID: payload.jobApplicationId ?? 1,
        resumePath: payload.resumeFilePath,
        resumeFilePath: payload.resumeFilePath,
        isAutoProcessed: payload.isAutoProcessed ?? false,
      })
      .pipe(
        timeout(environment.aiRequestTimeoutMs),
        catchError((e: HttpErrorResponse) => of(this.envelopeFrom(e) as ApiResponse<ParsedResume>)),
      );
  }

  /* ── Resume screening ───────────────────────────────────────────────────── */

  /**
   * Screens a resume against job requirements using AI.
   * Computes match score, shortlisted status, matched/missing skills, and evidence reasons.
   */
  screenResume(
    request: ScreenResumeRequest,
  ): Observable<ApiResponse<ScreenResumeResponse>> {
    return this.http
      .post<ApiResponse<ScreenResumeResponse>>(`${this.base}/ScreenResume`, request)
      .pipe(
        timeout(environment.aiRequestTimeoutMs),
        catchError((e: HttpErrorResponse) => of(this.envelopeFrom(e) as ApiResponse<ScreenResumeResponse>)),
      );
  }


  /* ── Plumbing ─────────────────────────────────────────────────────────── */

  private unknownTestResult(request: TestApiKeyRequest): TestApiKeyResult {
    return {
      isValid: false,
      provider: request.provider,
      model: null,
      testResponse: null,
      error: null,
      status: null,
      serviceVersion: null,
      schemaVersion: null,
      capabilities: [],
      configurationWarning: null,
    };
  }

  private envelopeFrom(e: HttpErrorResponse): ApiResponse<unknown> {
    const body = e.error as ApiResponse<unknown> | undefined;
    if (body?.message) {
      return body;
    }

    return {
      statusCode: e.status,
      message:
        e.status === 0
          ? `Could not reach the Rainmaker backend at ${environment.apiBaseUrl}.`
          : e.message,
      data: null,
      errors: [],
      isSuccess: false,
      timestamp: new Date().toISOString(),
    };
  }

  /**
   * Turns a transport failure into something a recruiter can act on. The
   * backend already sanitises its own messages, so anything it sends is safe
   * to show; what we add here is only for the cases where it never answered.
   */
  private readableError(e: unknown): Error {
    if (e instanceof HttpErrorResponse) {
      const body = e.error as ApiResponse<unknown> | undefined;

      if (e.status === 0) {
        return new Error(
          `Could not reach the Rainmaker backend at ${environment.apiBaseUrl}. ` +
            'Start it with `dotnet run` and try again.',
        );
      }

      if (body?.message) {
        return new Error(body.message);
      }

      return new Error(`The request failed (HTTP ${e.status}).`);
    }

    if (e instanceof Error && e.name === 'TimeoutError') {
      return new Error(
        'The AI service did not answer in time. It processes one request at a ' +
          'time, so it may be busy — try again in a moment.',
      );
    }

    return e instanceof Error ? e : new Error(String(e));
  }
}
