import { HttpInterceptorFn } from '@angular/common/http';

import { environment } from '../../../environments/environment';

/**
 * Attaches the local development bearer token to Rainmaker API calls.
 *
 * The recruitment module is `[Authorize]` plus `[ModuleAuthorize("RECRUITMENT_")]`,
 * so every request needs a token carrying the right claims. Until the real
 * login flow exists locally, this stands in.
 *
 * Deliberately scoped to `environment.apiBaseUrl` — a blanket interceptor
 * would leak the token to any third-party host the app ever calls.
 */
export const devAuthInterceptor: HttpInterceptorFn = (req, next) => {
  const isOurApi = req.url.startsWith(environment.apiBaseUrl);
  const haveToken = !!environment.devAuthToken;

  if (!isOurApi || !haveToken) {
    return next(req);
  }

  return next(
    req.clone({
      setHeaders: { Authorization: `Bearer ${environment.devAuthToken}` },
    }),
  );
};
