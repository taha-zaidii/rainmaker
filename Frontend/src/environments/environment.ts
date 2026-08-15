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
   * Local-only dev bearer token, signed with the local `Jwt:SecretKey` from
   * appsettings.Development.json (never with a real/production key).
   *
   * There is no login endpoint in this tree yet, so this token is hand-signed
   * HS256, not issued by the API. To mint your own (30-day expiry), run:
   *
   *   python3 -c "
   *   import base64,hmac,hashlib,json,time
   *   def b64(d): return base64.urlsafe_b64encode(d).rstrip(b'=').decode()
   *   secret = '<the Jwt:SecretKey value from your appsettings.Development.json>'
   *   now = int(time.time())
   *   payload = {'sub':'1','nameid':'1','unique_name':'superadmin','UserName':'superadmin',
   *     'CompanyID':'133','EmployeeID':'1','EmployeeCode':'System',
   *     'Permission':['ADMIN_RECRUITMENT','RECRUITMENT_VIEW','RECRUITMENT_CREATE',
   *       'RECRUITMENT_EDIT','RECRUITMENT_DELETE','RECRUITMENT_APPROVE',
   *       'RECRUITMENT_AI_SETTINGS','RECRUITMENT_AI_GENERATE'],
   *     'Modules':'[\"HRM\",\"RECRUITMENT\"]','iss':'DigiSoftERP','aud':'DigiSoftERPUsers',
   *     'iat':now,'nbf':now,'exp':now+30*24*3600}
   *   h = b64(json.dumps({'alg':'HS256','typ':'JWT'},separators=(',',':')).encode())
   *   p = b64(json.dumps(payload,separators=(',',':')).encode())
   *   sig = b64(hmac.new(secret.encode(), f'{h}.{p}'.encode(), hashlib.sha256).digest())
   *   print(f'{h}.{p}.{sig}')"
   *
   * Must match Program.cs's TokenValidationParameters: issuer "DigiSoftERP",
   * audience "DigiSoftERPUsers". CompanyID: 133 | Valid: 30 days from mint time.
   */
  devAuthToken:
    'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwibmFtZWlkIjoiMSIsInVuaXF1ZV9uYW1lIjoic3VwZXJhZG1pbiIsIlVzZXJOYW1lIjoic3VwZXJhZG1pbiIsIkNvbXBhbnlJRCI6IjEzMyIsIkVtcGxveWVlSUQiOiIxIiwiRW1wbG95ZWVDb2RlIjoiU3lzdGVtIiwiUGVybWlzc2lvbiI6WyJBRE1JTl9SRUNSVUlUTUVOVCIsIlJFQ1JVSVRNRU5UX1ZJRVciLCJSRUNSVUlUTUVOVF9DUkVBVEUiLCJSRUNSVUlUTUVOVF9FRElUIiwiUkVDUlVJVE1FTlRfREVMRVRFIiwiUkVDUlVJVE1FTlRfQVBQUk9WRSIsIlJFQ1JVSVRNRU5UX0FJX1NFVFRJTkdTIiwiUkVDUlVJVE1FTlRfQUlfR0VORVJBVEUiXSwiTW9kdWxlcyI6IltcIkhSTVwiLFwiUkVDUlVJVE1FTlRcIl0iLCJpc3MiOiJEaWdpU29mdEVSUCIsImF1ZCI6IkRpZ2lTb2Z0RVJQVXNlcnMiLCJpYXQiOjE3ODYzMjgxNDksIm5iZiI6MTc4NjMyODE0OSwiZXhwIjoxNzg4OTIwMTQ5fQ._WgOUVO-h5rRL_sHguBepFLtGerz4bbvLAqbSY8Szkw',

  /**
   * The AI service takes ~13 s warm and up to ~35 s cold. The browser must be
   * more patient than the slowest honest generation, or a working call looks
   * like a failure.
   */
  aiRequestTimeoutMs: 180_000,
};
