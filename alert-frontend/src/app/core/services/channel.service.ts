import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { NotificationChannel } from '../models/channel.model';

@Injectable({ providedIn: 'root' })
export class ChannelService {
  private readonly baseUrl = `${environment.apiBaseUrl}/api/channels`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<NotificationChannel[]> {
    return this.http.get<NotificationChannel[]>(this.baseUrl);
  }

  upsertEmail(destination: string): Observable<NotificationChannel> {
    return this.http.put<NotificationChannel>(`${this.baseUrl}/email`, { destination });
  }

  upsertSlack(destination: string): Observable<NotificationChannel> {
    return this.http.put<NotificationChannel>(`${this.baseUrl}/slack`, { destination });
  }

  toggle(id: number): Observable<NotificationChannel> {
    return this.http.patch<NotificationChannel>(`${this.baseUrl}/${id}/toggle`, {});
  }
}
