/**
 * Wire contract of the non-AI recruitment endpoints (RecruitmentController).
 *
 * Mirrors Digi.Shared/DTOs/hrm.module/RecruitmentCRUDDtos.cs. Property names
 * matter more than usual here: the backend's stored procedures alias columns
 * to these exact names and Dapper binds by name, so a rename on either side
 * silently produces nulls rather than an error.
 */

export interface PagedResult<T> {
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

/* ── Requisitions ───────────────────────────────────────────────────────── */

export interface JobRequisition {
  requisitionID: number;
  requisitionCode: string | null;
  companyID: number;
  jobTitle: string;
  jobSummary: string | null;

  departmentID: number | null;
  departmentName: string | null;
  designationID: number | null;
  designationName: string | null;
  employmentTypeID: number | null;
  employmentTypeName: string | null;
  gradeID: number | null;

  vacancies: number;
  minExperience: number | null;
  maxExperience: number | null;
  minSalary: number | null;
  maxSalary: number | null;
  location: string | null;

  /** Stored as newline- or comma-separated text, not arrays. */
  keyResponsibilities: string | null;
  requirements: string | null;
  qualifications: string | null;
  skills: string | null;
  benefits: string | null;
  justification: string | null;

  /** The gate for the public careers feed. A human sets this, never the AI. */
  isPublished: boolean;
  publishedDate: string | null;
  closingDate: string | null;

  statusID: number | null;
  statusCode: string | null;
  statusName: string | null;
  isActive: boolean;
  createdBy: string | null;
  /** Backend serializes this field as createdOn (not createdDate) for requisitions — see JobRequisitionResponseDto. */
  createdOn: string | null;

  /** Backend serializes this field as totalApplications (not applicationCount) for requisitions. */
  totalApplications?: number | null;
}

export interface JobRequisitionListResult extends PagedResult<JobRequisition> {
  requisitions: JobRequisition[];
}

export interface JobRequisitionListQuery {
  companyID: number;
  pageNumber?: number;
  pageSize?: number;
  searchTerm?: string | null;
  statusID?: number | null;
  isActive?: boolean | null;
  departmentID?: number | null;
}

export interface PublishResult {
  requisitionID: number;
  isPublished: boolean;
  publishedDate: string | null;
  publishedBy: string | null;
  statusID: number | null;
  statusCode: string | null;
  statusName: string | null;
}

/* ── Applicants (the candidate record) ──────────────────────────────────── */

export interface ApplicantCreateRequest {
  companyID: number;
  firstName: string;
  middleName?: string | null;
  lastName?: string | null;
  email?: string | null;
  mobileNumber?: string | null;
  phoneNumber?: string | null;
  currentAddress?: string | null;

  /** Everything below is typically filled by the AI resume parser. */
  totalExperience?: number | null;
  experienceYears?: number | null;
  experienceSummary?: string | null;
  education?: string | null;
  skills?: string | null;
  currentJobTitle?: string | null;
  currentDesignation?: string | null;
  currentCompany?: string | null;
  preferredLocation?: string | null;
  expectedSalary?: number | null;
  noticePeriod?: number | null;

  resumePath?: string | null;
  coverLetter?: string | null;
}

export interface Applicant {
  applicantID: number;
  applicantCode: string | null;
  companyID: number;
  firstName: string;
  lastName: string | null;
  fullName: string | null;
  email: string | null;
  mobileNumber: string | null;
  totalExperience: number | null;
  currentJobTitle: string | null;
  currentCompany: string | null;
  skills: string | null;
  education: string | null;
  experienceSummary: string | null;
  resumePath: string | null;
  isActive: boolean;
  createdDate: string | null;
}

export interface ApplicantListResult extends PagedResult<Applicant> {
  applicants: Applicant[];
}

/* ── Applications ───────────────────────────────────────────────────────── */

export interface JobApplicationCreateRequest {
  companyID: number;
  requisitionID: number;
  applicantID: number;
  applicationSourceID?: number | null;
  currentStatusID?: number | null;
  resumePath?: string | null;
  coverLetter?: string | null;
  remarks?: string | null;
}

export interface JobApplication {
  applicationID: number;
  applicationCode: string | null;
  companyID: number;
  requisitionID: number;
  applicantID: number;
  applicationDate: string | null;
  currentStatusID: number | null;
  statusID: number | null;
  statusCode: string | null;
  statusName: string | null;

  resumePath: string | null;
  coverLetter: string | null;
  remarks: string | null;
  screeningScore: number | null;
  resumeParsingID: number | null;

  /** Candidate, denormalised by the list procedure. */
  fullName: string | null;
  email: string | null;
  mobileNumber: string | null;
  applicantCode: string | null;
  currentJobTitle: string | null;
  currentDesignation?: string | null;
  currentCompany?: string | null;
  currentAddress?: string | null;
  skills?: string | null;
  education?: string | null;
  experienceSummary?: string | null;
  expectedSalary?: string | null;
  preferredLocation?: string | null;
  noticePeriod?: string | null;
  dateOfBirth?: string | null;
  experienceYears?: number | null;
  totalExperience?: number | null;


  /**
   * Note the names: the DTO exposes `requisitionJobTitle`, NOT `jobTitle`.
   * Binding the obvious name leaves the column blank with no error.
   */
  requisitionCode: string | null;
  requisitionJobTitle: string | null;
  requisitionLocation: string | null;
  departmentID: number | null;
  departmentName: string | null;

  isActive: boolean;
  createdDate: string | null;
}


export interface JobApplicationListResult extends PagedResult<JobApplication> {
  applications: JobApplication[];
}

export interface JobApplicationListQuery {
  companyID: number;
  pageNumber?: number;
  pageSize?: number;
  searchTerm?: string | null;
  requisitionID?: number | null;
  currentStatusID?: number | null;
}

/* ── Lookups ────────────────────────────────────────────────────────────── */

export interface RecruitmentStatus {
  statusID: number;
  statusTypeID: number;
  statusCode: string;
  statusName: string;
  displayOrder: number | null;
  isActive: boolean;
  statusTypeCode: string | null;
  statusTypeName: string | null;
}

/* ── Resume upload ──────────────────────────────────────────────────────── */

export interface UploadResumeResult {
  /**
   * Storage-relative path, e.g.
   * "superadmin/recruitment/resumes/documents/cv_20260805.pdf".
   *
   * THIS is what gets handed to the parser, not `url`. The absolute URL is
   * built for the local host (https://localhost:7777/...), which the AI
   * service running on Multinet's GPU box obviously cannot fetch — passing it
   * makes the parse fail with a confusing "could not read the document".
   * The relative path lets the backend open the file itself and upload the
   * bytes, which works from any environment.
   */
  relativePath: string;

  /** Absolute URL for browser display (the PDF preview), not for the parser. */
  url?: string | null;

  fileName: string;
  fileSize: number;
  fileType?: string | null;
}

/* ── Helpers ────────────────────────────────────────────────────────────── */

/**
 * Requisition text fields arrive as one blob — newline separated when the AI
 * wrote them, comma separated when a human typed them. Both render as a list.
 */
export function toLines(value: string | null | undefined): string[] {
  if (!value) {
    return [];
  }

  const byLine = value
    .split(/\r?\n/)
    .map((l) => l.replace(/^[-•*]\s*/, '').trim())
    .filter(Boolean);

  if (byLine.length > 1) {
    return byLine;
  }

  return value
    .split(',')
    .map((s) => s.trim())
    .filter(Boolean);
}
