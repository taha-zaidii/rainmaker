/**
 * Local development configuration.
 *
 * The dev token below is signed with the LOCAL dev JWT secret only. It is
 * useless against staging or production, which sign with different keys. It
 * exists so the AI features can be exercised end to end without standing up
 * the Admin/Auth service locally.
 *
 * When real login lands, delete `devAuthToken` and have the interceptor read
 * from the auth store instead — that is the only change needed.
 */
export const environment = {
  production: false,

  /** Recruitment module API. Matches the CORS allow-list in appsettings.json. */
  apiBaseUrl: 'http://localhost:5019',

  /**
   * The tenant every request is scoped to. The backend reads CompanyID from
   * the JWT for authorisation, but most endpoints also take it in the payload.
   */
  companyId: 133,

  /**
   * Local-only dev bearer token. NEVER commit a real token here.
   *
   * To generate a fresh token:
   *   cd Backend/RM/Digi.Recruitment.Module
   *   dotnet run --generate-dev-token
   * (or use the /api/auth/dev-token endpoint in local mode)
   *
   * Required claims: ADMIN_RECRUITMENT, RECRUITMENT_VIEW/CREATE/EDIT/DELETE/
   * APPROVE, RECRUITMENT_AI_SETTINGS, RECRUITMENT_AI_GENERATE
   * CompanyID: 133  |  Valid: 30 days
   */
  devAuthToken: 'REPLACE_ME_WITH_LOCAL_DEV_TOKEN',

  /**
   * The AI service takes ~13 s warm and up to ~35 s cold. The browser must be
   * more patient than the slowest honest generation, or a working call looks
   * like a failure.
   */
  aiRequestTimeoutMs: 180_000,
};
