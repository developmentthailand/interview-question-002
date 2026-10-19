import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

export interface RegisterRequest {
  username: string;
  password: string;
  confirmPassword: string;
}

export interface RegisteredUserResponse {
  id: number;
  username: string;
}

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  tokenType: 'Bearer';
  expiresAtUtc: string;
}

export interface CurrentUserResponse {
  id: number;
  username: string;
}

@Injectable({ providedIn: 'root' })
export class AuthApiService {
  private readonly http = inject(HttpClient);

  register(request: RegisterRequest): Observable<RegisteredUserResponse> {
    return this.http.post<RegisteredUserResponse>('/api/auth/register', request);
  }

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>('/api/auth/login', request);
  }

  currentUser(): Observable<CurrentUserResponse> {
    return this.http.get<CurrentUserResponse>('/api/auth/me');
  }
}
