import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, of, throwError } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ApiResponse } from './recruitment-ai.models';
import {
  Applicant,
  ApplicantCreateRequest,
  ApplicantListResult,
  JobApplication,
  JobApplicationCreateRequest,
  JobApplicationListQuery,
  JobApplicationListResult,
  JobRequisition,
  JobRequisitionListQuery,
  JobRequisitionListResult,
  PublishResult,
  RecruitmentStatus,
  UploadResumeResult,
} from './recruitment.models';

/**
 * The non-AI recruitment endpoints: requisitions, applicants, applications.
 *
 * Read calls degrade to an empty result rather than throwing — a grid that
 * renders "no rows" with a visible message beats one that blanks the page.
 * Write calls propagate, because a failed save must never look like a save.
 */
@Injectable({ providedIn: 'root' })
export class RecruitmentService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/recruitment/api/Recruitment`;

  /* ── Requisitions ─────────────────────────────────────────────────────── */

  listRequisitions(query: JobRequisitionListQuery): Observable<JobRequisitionListResult> {
    const empty: JobRequisitionListResult = {
      requisitions: [],
      totalCount: 0,
      pageNumber: 1,
      pageSize: 50,
      totalPages: 0,
    };

    return this.http
      .get<ApiResponse<JobRequisitionListResult>>(`${this.base}/Requisitions`, {
        params: this.params(query),
      })
      .pipe(
        map((r) => r.data ?? empty),
        catchError(() => of(empty)),
      );
  }

  getRequisition(requisitionID: number): Observable<JobRequisition | null> {
    return this.http
      .get<ApiResponse<JobRequisition>>(`${this.base}/Requisitions/${requisitionID}`)
      .pipe(
        map((r) => r.data),
        catchError(() => of(null)),
      );
  }

  /**
   * The public careers feed. Anonymous — no token is attached, and the
   * backend only ever returns published, active, unexpired requisitions.
   */
  listPublicRequisitions(
    companyID: number,
    searchText?: string | null,
    location?: string | null,
  ): Observable<JobRequisition[]> {
    return this.http
      .get<ApiResponse<JobRequisition[]>>(`${this.base}/Requisitions/Public`, {
        params: this.params({ companyID, searchText, location }),
      })
      .pipe(
        map((r) => r.data ?? []),
        catchError(() => of([])),
      );
  }

  /**
   * Publishing is the moment a draft becomes a public advert. Always a
   * deliberate human action — the AI produces drafts and never reaches here.
   */
  publishRequisition(
    requisitionID: number,
    companyID: number,
    publishedBy: string,
  ): Observable<PublishResult> {
    return this.http
      .post<ApiResponse<PublishResult>>(
        `${this.base}/Requisitions/${requisitionID}/Publish`,
        { companyID, publishedBy },
      )
      .pipe(
        map((r) => {
          if (!r.isSuccess || !r.data) {
            throw new Error(r.message || 'The requisition could not be published.');
          }
          return r.data;
        }),
        catchError((e) => throwError(() => this.readable(e))),
      );
  }

  /* ── Applicants ───────────────────────────────────────────────────────── */

  createApplicant(request: ApplicantCreateRequest): Observable<Applicant> {
    return this.http
      .post<ApiResponse<Applicant>>(`${this.base}/Applicants`, request)
      .pipe(
        map((r) => {
          if (!r.isSuccess || !r.data) {
            throw new Error(r.message || 'The candidate record could not be created.');
          }
          return r.data;
        }),
        catchError((e) => throwError(() => this.readable(e))),
      );
  }

  listApplicants(companyID: number, searchTerm?: string | null): Observable<Applicant[]> {
    return this.http
      .get<ApiResponse<ApplicantListResult>>(`${this.base}/Applicants`, {
        params: this.params({ companyID, searchTerm, pageSize: 200 }),
      })
      .pipe(
        map((r) => r.data?.applicants ?? []),
        catchError(() => of([])),
      );
  }

  /* ── Applications ─────────────────────────────────────────────────────── */

  createApplication(request: JobApplicationCreateRequest): Observable<JobApplication> {
    return this.http
      .post<ApiResponse<JobApplication>>(`${this.base}/Applications`, request)
      .pipe(
        map((r) => {
          if (!r.isSuccess || !r.data) {
            throw new Error(r.message || 'The application could not be submitted.');
          }
          return r.data;
        }),
        catchError((e) => throwError(() => this.readable(e))),
      );
  }

  listApplications(query: JobApplicationListQuery): Observable<JobApplicationListResult> {
    const empty: JobApplicationListResult = {
      applications: [],
      totalCount: 0,
      pageNumber: 1,
      pageSize: 50,
      totalPages: 0,
    };

    return this.http
      .get<ApiResponse<JobApplicationListResult>>(`${this.base}/Applications/List`, {
        params: this.params(query),
      })
      .pipe(
        map((r) => r.data ?? empty),
        catchError(() => of(empty)),
      );
  }

  getApplication(applicationID: number): Observable<JobApplication | null> {
    return this.http
      .get<ApiResponse<JobApplication>>(`${this.base}/Applications/${applicationID}`)
      .pipe(
        map((r) => r.data),
        catchError(() => of(null)),
      );
  }

  updateApplication(request: { applicationID: number; currentStatusID?: number; remarks?: string }): Observable<boolean> {
    return this.http
      .put<ApiResponse<boolean>>(`${this.base}/Applications/${request.applicationID}`, request)
      .pipe(
        map((r) => r.isSuccess),
        catchError(() => of(false)),
      );
  }

  shortlistCandidate(applicationID: number, companyID: number = 133): Observable<boolean> {
    return this.http
      .post<ApiResponse<any>>(`${this.base}/Applications/${applicationID}/Shortlist`, {
        companyID,
        shortlistedBy: 'Recruiter',
        remarks: 'Shortlisted by HR recruiter sign-off',
      })
      .pipe(
        map((r) => r.isSuccess),
        catchError(() => of(false)),
      );
  }

  rejectApplication(applicationID: number, companyID: number = 133): Observable<boolean> {
    return this.http
      .post<ApiResponse<any>>(`${this.base}/Applications/${applicationID}/Reject`, {
        companyID,
        rejectionReason: 'Not matching current requisition criteria',
        rejectedBy: 'Recruiter',
      })
      .pipe(
        map((r) => r.isSuccess),
        catchError(() => of(false)),
      );
  }

  deleteRequisition(requisitionID: number): Observable<boolean> {

    return this.http
      .delete<ApiResponse<boolean>>(`${this.base}/Requisitions/${requisitionID}`)
      .pipe(
        map((r) => r.isSuccess),
        catchError(() => of(false)),
      );
  }

  deleteApplication(applicationID: number): Observable<boolean> {
    return this.http
      .delete<ApiResponse<boolean>>(`${this.base}/Applications/${applicationID}`)
      .pipe(
        map((r) => r.isSuccess),
        catchError(() => of(false)),
      );
  }


  /* ── Lookups ──────────────────────────────────────────────────────────── */

  listStatuses(statusTypeCode?: string): Observable<RecruitmentStatus[]> {
    return this.http
      .get<ApiResponse<RecruitmentStatus[]>>(`${this.base}/Statuses`, {
        params: this.params({ statusTypeCode, isActive: true }),
      })
      .pipe(
        map((r) => r.data ?? []),
        catchError(() => of([])),
      );
  }

  /* ── Resume upload ────────────────────────────────────────────────────── */

  /**
   * Stores the CV and returns its path. Anonymous, because candidates apply
   * from the public careers page without an account.
   *
   * The path this returns is what gets handed to the AI parser — the file is
   * uploaded once and referenced afterwards, rather than posted again as
   * bytes for every parse.
   */
  uploadResume(file: File, companyID: number): Observable<UploadResumeResult> {
    const form = new FormData();
    form.append('file', file, file.name);
    form.append('companyID', String(companyID));

    return this.http
      .post<ApiResponse<UploadResumeResult>>(`${this.base}/Resume/Upload`, form)
      .pipe(
        map((r) => {
          if (!r.isSuccess || !r.data) {
            throw new Error(r.message || 'The file could not be uploaded.');
          }
          return r.data;
        }),
        catchError((e) => throwError(() => this.readable(e))),
      );
  }

  /* ── Plumbing ─────────────────────────────────────────────────────────── */

  /** Drops null/undefined so absent filters are omitted, not sent as "null". */
  private params(source: object): HttpParams {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(source as Record<string, unknown>)) {
      if (value !== null && value !== undefined && value !== '') {
        params = params.set(key, String(value));
      }
    }
    return params;
  }

  private readable(e: unknown): Error {
    if (e instanceof HttpErrorResponse) {
      if (e.status === 0) {
        return new Error(
          `Could not reach the Rainmaker backend at ${environment.apiBaseUrl}.`,
        );
      }
      const body = e.error as ApiResponse<unknown> | undefined;
      return new Error(body?.message || `The request failed (HTTP ${e.status}).`);
    }
    return e instanceof Error ? e : new Error(String(e));
  }
}
