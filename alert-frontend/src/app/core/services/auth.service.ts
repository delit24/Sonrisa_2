import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, tap, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LoginResponse } from '../models/user.model';

interface CurrentUser {
  id: number;
  email: string;
  fullName: string;
  role: 'admin' | 'user';
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly TOKEN_KEY = 'auth_token';
  private currentUserSubject = new BehaviorSubject<CurrentUser | null>(null);

  currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient, private router: Router) {
    // Restore user from token on page reload
    const token = localStorage.getItem(this.TOKEN_KEY);
    if (token) {
      const decoded = this.decodeToken(token);
      if (decoded && !this.isTokenExpired(decoded)) {
        this.currentUserSubject.next(decoded.user);
      } else {
        this.logout();
      }
    }
  }

  login(email: string, password: string): Observable<void> {
    return this.http.post<LoginResponse>(`${environment.apiBaseUrl}/api/auth/login`, { email, password })
      .pipe(
        tap(response => {
          localStorage.setItem(this.TOKEN_KEY, response.token);
          this.currentUserSubject.next(response.user);
        }),
        map(response => {
          // Role-based redirect
          if (response.user.role === 'admin') {
            this.router.navigate(['/admin']);
          } else {
            this.router.navigate(['/dashboard']);
          }
        })
      );
  }

  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    this.currentUserSubject.next(null);
    this.router.navigate(['/login']);
  }

  isLoggedIn(): boolean {
    return this.currentUserSubject.value !== null;
  }

  currentUser(): CurrentUser | null {
    return this.currentUserSubject.value;
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  private decodeToken(token: string): any {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return {
        exp: payload.exp,
        user: {
          id: parseInt(payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier']),
          email: payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'],
          fullName: payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'],
          role: payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
        }
      };
    } catch {
      return null;
    }
  }

  private isTokenExpired(decoded: any): boolean {
    return decoded.exp * 1000 < Date.now();
  }
}
