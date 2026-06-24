import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

export interface LoginResponse {
  token: string;
  username: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly tokenKey = 'auth_token';
  private readonly userKey = 'auth_username';

  constructor(private http: HttpClient) {}

  login(username: string, password: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>('/api/auth/login', { username, password }).pipe(
      tap(res => {
        localStorage.setItem(this.tokenKey, res.token);
        localStorage.setItem(this.userKey, res.username);
      })
    );
  }

  logout(): Observable<any> {
    return this.http.post('/api/auth/logout', {}).pipe(
      tap({
        finalize: () => {
          localStorage.removeItem(this.tokenKey);
          localStorage.removeItem(this.userKey);
        }
      })
    );
  }

  isLoggedIn(): boolean {
    return !!localStorage.getItem(this.tokenKey);
  }

  getUsername(): string {
    return localStorage.getItem(this.userKey) || '';
  }

  getToken(): string {
    return localStorage.getItem(this.tokenKey) || '';
  }
}
