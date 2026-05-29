import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AlertPreference } from '../models/preference.model';

@Injectable({ providedIn: 'root' })
export class PreferenceService {
  private readonly baseUrl = `${environment.apiBaseUrl}/api/preferences`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<AlertPreference[]> {
    return this.http.get<AlertPreference[]>(this.baseUrl);
  }

  create(preference: { category: string; keyword?: string }): Observable<AlertPreference> {
    return this.http.post<AlertPreference>(this.baseUrl, preference);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  toggle(id: number): Observable<AlertPreference> {
    return this.http.patch<AlertPreference>(`${this.baseUrl}/${id}/toggle`, {});
  }
}
