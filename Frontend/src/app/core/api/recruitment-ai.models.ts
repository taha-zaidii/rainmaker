/**
 * Wire contract of the recruitment AI endpoints.
 *
 * These mirror Digi.Shared/DTOs/hrm.module/RecruitmentAIDtos.cs exactly. When
 * that file changes, this one changes with it — nothing here is invented.
 */

/** Every endpoint answers in this envelope. `data` is null on most failures. */
export interface ApiResponse<T> {
  statusCode: number;
  message: string;
  data: T | null;
  errors: string[];
  isSuccess: boolean;
  timestamp: string;
}

/* ── Settings ───────────────────────────────────────────────────────────── */

export type AiProvider =
  | 'openai'
  | 'anthropic'
  | 'google'
  | 'custom'
  | 'multinetai';

export interface FeatureSettings {
  autoScreening: boolean;
  autoMatching: boolean;
  generateQuestions: boolean;
  emailNotifications: boolean;
  autoParse: boolean;
}

export interface SaveApiKeySettingsRequest {
  companyId: number;
  provider: AiProvider;
  /** Blank keeps the stored key. Encrypted server-side; never echoed back. */
  apiKey: string;
  apiEndpoint: string;
  model: string;
  maxTokens: number;
  temperature: number;
  settings: FeatureSettings;
}

export interface ApiKeyStatus {
  hasApiKey: boolean;
  provider: string | null;
  isValid: boolean;
}

export interface ApiKeySettings {
  apiKey: string | null;
  provider: string;
  apiEndpoint: string | null;
  model: string | null;
  maxTokens: number;
  temperature: number;
  autoShortlistThreshold: number;
  settings: FeatureSettings;
  createdOn: string | null;
  updatedOn: string | null;
}

/**
 * Why this is a string and not a boolean: "your key is wrong" and "we could
 * not reach the service" look identical to a recruiter but need opposite
 * actions. Collapsing them into one flag is what made the old settings page
 * report "API Key Invalid" for faults that had nothing to do with the key.
 */
export type TestApiKeyStatus =
  | 'valid'
  | 'invalid_key'
  | 'unreachable'
  | 'misconfigured'
  | 'unsupported_provider';

export interface TestApiKeyRequest {
  companyId: number;
  provider: AiProvider;
  /** Blank tests the stored key — which is what the button usually means. */
  apiKey: string;
  apiEndpoint: string;
}

export interface TestApiKeyResult {
  isValid: boolean;
  provider: string;
  /** Service identity, e.g. "hrms-ai-service". Not a model name. */
  model: string | null;
  testResponse: string | null;
  error: string | null;
  status: TestApiKeyStatus | null;
  serviceVersion: string | null;
  schemaVersion: string | null;
  /** Drive the feature toggles from this rather than hard-coding them. */
  capabilities: string[];
  /** Usable-but-suspicious config, e.g. an endpoint we had to correct. */
  configurationWarning: string | null;
}

/* ── Job description generation ─────────────────────────────────────────── */

export interface GenerateJobDescriptionRequest {
  companyId: number;
  jobTitle: string;
  department?: string | null;
  designation?: string | null;
  experience?: string | null;
  skills?: string | null;
  additionalInfo?: string | null;
  /** Send the dropdown's real values so the answer always binds. */
  jobCategoryOptions?: string[] | null;
}

export interface AiJobDraftRange {
  minimum: number | null;
  maximum: number | null;
}

export interface AiJobDraftBasicInfo {
  /** Verbatim echoes of what was submitted — safe to bind straight back. */
  jobTitle: string | null;
  department: string | null;
  designation: string | null;
  jobSummary: string | null;
  jobCategory: string | null;
  vacancies: number | null;
  /** Null by design — HR decides. Render empty and editable, never inferred. */
  employmentType: string | null;
  /** Null by design — HR decides. */
  grade: string | null;
}

export interface AiJobDraftRequirements {
  experienceYears: AiJobDraftRange | null;
  /**
   * ALWAYS null, and the UI must never bind an input to it. Age is a
   * protected attribute; an AI proposing an age band in a job advert is
   * discriminatory and indefensible under the EU AI Act's high-risk hiring
   * rules. Present in the type only so a non-null value is visible as the
   * contract violation it would be.
   */
  ageLimits: AiJobDraftRange | null;
  keyResponsibilities: string[];
  requirements: string[];
  qualifications: string[];
  skills: string[];
}

export interface AiJobDraftCompensation {
  location: string | null;
  /** Null by design. */
  benefits: string | null;
  /** Null by design. */
  budgetType: string | null;
  /** Null by design. */
  budgetLineId: number | null;
}

export interface AiJobDraftPublishing {
  /** Null by design. */
  justification: string | null;
  /** Always false. A human publishes; the AI never does. */
  isPublicJob: boolean;
  /** Always "Draft". */
  status: string | null;
  /** Null by design. */
  closingDate: string | null;
}

/** One property per wizard step — the shape IS the screen. */
export interface AiJobDraft {
  basicInfo: AiJobDraftBasicInfo;
  requirements: AiJobDraftRequirements;
  compensation: AiJobDraftCompensation;
  publishing: AiJobDraftPublishing;
}

export interface GenerateJobDescriptionResult {
  /** Readable rendering of the whole draft, for callers that want one blob. */
  jobDescription: string;
  generatedOn: string;
  tokensUsed: number;
  model: string | null;

  /** Present for providers that return structure. Null for plain-text ones. */
  draft: AiJobDraft | null;

  /** Always true for AI output. Drives the review banner. */
  reviewRequired: boolean;
  executionTimeMs: number | null;
  /** True when the service answered from its deterministic cache. */
  cacheHit: boolean | null;
  /** "parsed_from_request" when the AI used the range the user typed. */
  experienceSource: string | null;
  /** "selected_from_options" when the category snapped to a real value. */
  jobCategorySource: string | null;
  workMode: string | null;
  /** Friendly names of the fields the AI deliberately left for a human. */
  fieldsForHumanToComplete: string[];
}

/* ── Dashboard ──────────────────────────────────────────────────────────── */

export interface DashboardStats {
  totalRequisitions: number;
  activeRequisitions: number;
  totalApplications: number;
  interviewsScheduled: number;
  hiredCount: number;
  pendingEvaluations: number;
  totalJobsAnalyzed: number;
  resumesScreened: number;
  candidatesMatched: number;
  /** Hours. */
  timeSaved: number;
}

export interface RecentActivity {
  id: number;
  /** e.g. "job_description", "resume_parsing", "screening". */
  activityType: string;
  title: string;
  description: string;
  relatedId: number | null;
  createdOn: string;
}

export interface DashboardData {
  stats: DashboardStats;
  recentActivity: RecentActivity[];
}

/* ── Capability slugs reported by /auth/verify ──────────────────────────── */

export const AI_CAPABILITY_LABEL: Record<string, string> = {
  'parser.extract': 'Resume Parsing',
  'recruitment.jobreq.generate': 'JD Generation',
  'recruitment.screening.screen': 'Screening',
  'recruitment.interview.questions': 'Interview Questions',
  'matching.rank': 'Ranking',
  'scoring.score': 'Scoring',
};


/* ── Resume parsing ─────────────────────────────────────────────────────── */

export interface ResumeExperience {
  company: string | null;
  role: string | null;
  jobTitle: string | null;
  duration: string | null;
  location: string | null;
  description: string | null;
}

export interface ResumeEducation {
  institution: string | null;
  degree: string | null;
  duration: string | null;
  gpa: string | null;
}

export interface ResumeProject {
  name: string | null;
  technologies: string | null;
  description: string | null;
}

/**
 * Output of the AI resume parser.
 *
 * Accuracy is certified around 95% at field level — excellent, not perfect.
 * Every value here is a SUGGESTION a human confirms before it becomes a
 * candidate record, which is why the apply form shows these fields filled but
 * editable and marked as AI-derived rather than silently accepting them.
 */
export interface ParsedResume {
  fullName: string | null;
  candidateName: string | null;
  email: string | null;
  phone: string | null;
  location: string | null;
  summary: string | null;
  skills: string[];
  experience: ResumeExperience[];
  education: ResumeEducation[];
  projects: ResumeProject[];
  certifications: string[];
  languages: string | null;
  totalYearsExperience: number | null;
  parsedOn: string | null;

  /* The service returns more than the C# DTO documents. These are optional
     because which of them appear depends on the extraction route the parser
     took, so a screen must tolerate any of them being absent. */
  phoneNumber?: string | null;
  totalExperienceYears?: number | null;

  /** Raw ProfileSchema JSON, stored verbatim in the ParsedData column. */
  parsedDataJson?: string | null;

  /** Per-field record of HOW each value was obtained. Anything not produced
   *  and verified by the model itself is worth flagging to a reviewer. */
  fieldProvenance?: Record<string, string> | null;

  /** Anything added after this client was written — never a hard failure. */
  [key: string]: unknown;
}

/* ── Resume screening ───────────────────────────────────────────────────── */

export interface ScreeningReason {
  kind: 'match' | 'gap' | string;
  detail: string;
  evidence: string;
}

export interface JobRequirements {
  jobTitle: string;
  requiredSkills?: string[];
  experience?: string | null;
  education?: string | null;
}

export interface ScreenResumeRequest {
  companyID: number;
  applicationID?: number | null;
  applicantID?: number | null;
  resumeParsingID?: number | null;
  resumeId?: number | null;
  resumePath: string;
  resumeText?: string | null;
  jobRequirements: JobRequirements;
}

export interface ScreenResumeResponse {
  matchScore: number;
  recommendation: string;
  strengths: string[];
  weaknesses: string[];
  screeningNotes: string;
  screenedOn: string;
  shortlisted?: boolean;
  thresholdUsed?: number;
  matchedSkills?: string[];
  missingSkills?: string[];
  reasons?: ScreeningReason[];
}

