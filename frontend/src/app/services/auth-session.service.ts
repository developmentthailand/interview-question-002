import { Injectable, inject, signal } from '@angular/core';
import { catchError, Observable, switchMap, tap, throwError } from 'rxjs';
import { AuthApiService, CurrentUserResponse } from './auth-api.service';

@Injectable({ providedIn: 'root' })
export class AuthSessionService {
  private readonly api = inject(AuthApiService);
  private readonly accessTokenState = signal<string | null>(null);
  private readonly currentUserState = signal<CurrentUserResponse | null>(null);

  readonly accessToken = this.accessTokenState.asReadonly();
  readonly currentUser = this.currentUserState.asReadonly();

  signIn(username: string, password: string): Observable<CurrentUserResponse> {
    return this.api.login({ username, password }).pipe(
      tap((session) => this.accessTokenState.set(session.accessToken)),
      switchMap(() =>
        this.api.currentUser().pipe(tap((user) => this.currentUserState.set(user))),
      ),
      catchError((error: unknown) => {
        this.signOut();
        return throwError(() => error);
      }),
    );
  }

  signOut(): void {
    this.accessTokenState.set(null);
    this.currentUserState.set(null);
  }
}
