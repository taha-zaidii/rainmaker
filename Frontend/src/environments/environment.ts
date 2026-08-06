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
   * Valid 30 days from 2026-08-05. Carries ADMIN_RECRUITMENT.
   *
   * That claim is load-bearing: without it the list endpoints silently scope
   * to "rows this user created" and every grid renders empty with a 200 and
   * no error anywhere — which cost real time to track down once already.
   */
  devAuthToken:
    'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwibmFtZWlkIjoiMSIsInVuaXF1ZV9uYW1lIjoic3VwZXJhZG1pbiIsIlVzZXJOYW1lIjoic3VwZXJhZG1pbiIsIkNvbXBhbnlJRCI6IjEzMyIsIkVtcGxveWVlSUQiOiIxIiwiRW1wbG95ZWVDb2RlIjoiU3lzdGVtIiwiUGVybWlzc2lvbiI6WyJBRE1JTl9SRUNSVUlUTUVOVCIsIlJFQ1JVSVRNRU5UX1ZJRVciLCJSRUNSVUlUTUVOVF9DUkVBVEUiLCJSRUNSVUlUTUVOVF9FRElUIiwiUkVDUlVJVE1FTlRfREVMRVRFIiwiUkVDUlVJVE1FTlRfQVBQUk9WRSIsIlJFQ1JVSVRNRU5UX0FJX1NFVFRJTkdTIiwiUkVDUlVJVE1FTlRfQUlfR0VORVJBVEUiXSwiTW9kdWxlcyI6IltcIkhSTVwiLFwiUkVDUlVJVE1FTlRcIl0iLCJpc3MiOiJEaWdpU29mdEVSUCIsImF1ZCI6IkRpZ2lTb2Z0RVJQVXNlcnMiLCJpYXQiOjE3ODU5MDIxNzcsIm5iZiI6MTc4NTkwMjE3NywiZXhwIjoxNzg4NDk0MTc3fQ.tV2eXyi0r1W6oSBUkTg96ScEwoil2DHS9fjitfkXDvg',

  /**
   * The AI service takes ~13 s warm and up to ~35 s cold. The browser must be
   * more patient than the slowest honest generation, or a working call looks
   * like a failure.
   */
  aiRequestTimeoutMs: 180_000,
};
