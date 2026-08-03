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

  /** Valid 30 days from 2026-08-03. Regenerate with db/../mint-dev-jwt if it expires. */
  devAuthToken:
    'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwibmFtZWlkIjoiMSIsInVuaXF1ZV9uYW1lIjoic3VwZXJhZG1pbiIsIlVzZXJOYW1lIjoic3VwZXJhZG1pbiIsIkNvbXBhbnlJRCI6IjEzMyIsImlzcyI6IkRpZ2lTb2Z0RVJQIiwiYXVkIjoiRGlnaVNvZnRFUlBVc2VycyIsImlhdCI6MTc4NTc0MTYzOSwibmJmIjoxNzg1NzQxNjM5LCJleHAiOjE3ODgzMzM2Mzl9.-YgWUpKLgyIKZCdcG9RCZcCC8Yfmzm12Q0l7P6knGXc',

  /**
   * The AI service takes ~13 s warm and up to ~35 s cold. The browser must be
   * more patient than the slowest honest generation, or a working call looks
   * like a failure.
   */
  aiRequestTimeoutMs: 180_000,
};
