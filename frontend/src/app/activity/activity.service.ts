import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, interval, switchMap, startWith } from 'rxjs';
import { AuthService } from '../auth.service';

export interface ActivityItem {
  id: number;
  actionType: string;
  description: string;
  ipAddress: string;
  occurredAt: string;
  metadata?: string;
}

export interface ActivityListResponse {
  items: ActivityItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface ActivityFilter {
  page: number;
  pageSize: number;
  actionType?: string;
  from?: string;
  to?: string;
  search?: string;
  userId?: number;
}

@Injectable({ providedIn: 'root' })
export class ActivityService {
  private readonly baseUrl = '/api/activity';
  private pollHandle: ReturnType<typeof setInterval> | null = null;

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  list(filter: ActivityFilter): Observable<ActivityListResponse> {
    let params = new HttpParams()
      .set('page', filter.page)
      .set('pageSize', filter.pageSize);

    if (filter.actionType) params = params.set('actionType', filter.actionType);
    if (filter.from) params = params.set('from', filter.from);
    if (filter.to) params = params.set('to', filter.to);
    if (filter.search) params = params.set('search', filter.search);
    if (filter.userId != null) params = params.set('userId', filter.userId);

    return this.http.get<ActivityListResponse>(this.baseUrl, { params });
  }

  exportCsv(from?: string, to?: string): void {
    const token = this.authService.getToken();
    const query = new URLSearchParams();
    if (from) query.set('from', from);
    if (to) query.set('to', to);
    if (token) query.set('token', token);

    const url = `http://localhost:5000/api/activity/export?${query.toString()}`;
    window.open(url, '_blank');
  }

  record(actionType: string, description: string, metadata?: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/record`, {
      actionType,
      description,
      metadata
    });
  }

  startPolling(filter: ActivityFilter, onData: (res: ActivityListResponse) => void): void {
    this.pollHandle = setInterval(() => {
      this.list(filter).subscribe(res => onData(res));
    }, 15000);

    interval(15000).pipe(
      startWith(0),
      switchMap(() => this.list(filter))
    ).subscribe(res => onData(res));
  }

  stopPolling(): void {
    if (this.pollHandle) {
      clearInterval(this.pollHandle);
    }
  }

  formatActionLabel(actionType: string): string {
    return actionType.replace(/([A-Z])/g, ' $1').trim();
  }

  buildDetailHtml(item: ActivityItem): string {
    const meta = item.metadata ? `<small>${item.metadata}</small>` : '';
    return `<strong>${item.actionType}</strong>: ${item.description} ${meta}`;
  }
}
