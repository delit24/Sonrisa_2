import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { User } from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly baseUrl = `${environment.apiBaseUrl}/api/users`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<User[]> {
    return this.http.get<User[]>(this.baseUrl);
  }

  getById(id: number): Observable<User> {
    return this.http.get<User>(`${this.baseUrl}/${id}`);
  }

  create(user: { email: string; password: string; fullName: string; role: string }): Observable<User> {
    return this.http.post<User>(this.baseUrl, user);
  }

  update(id: number, user: { email: string; password?: string; fullName: string; role: string }): Observable<User> {
    return this.http.put<User>(`${this.baseUrl}/${id}`, user);
  }

  deactivate(id: number): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/${id}/deactivate`, {});
  }

  activate(id: number): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/${id}/activate`, {});
  }
}
