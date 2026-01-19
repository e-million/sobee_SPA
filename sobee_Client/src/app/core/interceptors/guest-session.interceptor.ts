import { HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { tap } from 'rxjs/operators';

/**
 * Interceptor that attaches guest session headers and captures updated sessions.
 * @param req - Outgoing HTTP request.
 * @param next - Next handler in the interceptor chain.
 * @returns Observable of the HTTP event stream.
 */
export const guestSessionInterceptor: HttpInterceptorFn = (req, next) => {
  // Get guest session from localStorage
  const sessionId = localStorage.getItem('guestSessionId');
  const sessionSecret = localStorage.getItem('guestSessionSecret');

  // Clone the request and add guest session headers if they exist
  if (sessionId && sessionSecret) {
    req = req.clone({
      setHeaders: {
        'X-Session-Id': sessionId,
        'X-Session-Secret': sessionSecret
      }
    });
  }

  // Process the response and capture guest session headers if present
  return next(req).pipe(
    tap(event => {
      if (event instanceof HttpResponse) {
        const newSessionId = event.headers.get('X-Session-Id');
        const newSessionSecret = event.headers.get('X-Session-Secret');

        if (newSessionId && newSessionSecret) {
          localStorage.setItem('guestSessionId', newSessionId);
          localStorage.setItem('guestSessionSecret', newSessionSecret);
        }
      }
    })
  );
};
