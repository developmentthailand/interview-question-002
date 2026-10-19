import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthSessionService } from './auth-session.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const token = inject(AuthSessionService).accessToken();
  if (!token || request.url !== '/api/auth/me') {
    return next(request);
  }

  return next(request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
};
