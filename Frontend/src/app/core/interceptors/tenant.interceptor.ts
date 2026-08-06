import { HttpInterceptorFn } from '@angular/common/http';
import { environment } from '../../../environments/environment';

/**
 * Enterprise Multi-Tenant Interceptor.
 * Attaches X-Company-ID header to every internal Rainmaker API call to ensure strict
 * company isolation across all ERP modules.
 */
export const tenantInterceptor: HttpInterceptorFn = (req, next) => {
  const isOurApi = req.url.startsWith(environment.apiBaseUrl);

  if (!isOurApi || req.headers.has('X-Company-ID')) {
    return next(req);
  }

  return next(
    req.clone({
      setHeaders: {
        'X-Company-ID': environment.companyId.toString(),
      },
    }),
  );
};
