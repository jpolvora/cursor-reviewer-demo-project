import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivityService, ActivityItem, ActivityFilter } from './activity.service';
import { AuthService } from '../auth.service';

@Component({
  selector: 'app-activity-monitor',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="shell">
      <header>
        <h1>Session Activity</h1>
        <p>Account events for {{ username }}</p>
        <button (click)="onExport()">Export CSV</button>
      </header>
      <section class="filters">
        <select [(ngModel)]="filter.actionType" (change)="reload()">
          <option value="">All actions</option>
          <option value="Login">Login</option>
          <option value="Logout">Logout</option>
          <option value="ProfileUpdate">Profile Update</option>
          <option value="PasswordChange">Password Change</option>
        </select>
        <input type="date" [(ngModel)]="filter.from" (change)="reload()" />
        <input type="date" [(ngModel)]="filter.to" (change)="reload()" />
        <input type="text" [(ngModel)]="filter.search" placeholder="Search" />
        <button (click)="reload()">Apply</button>
      </section>
      <section *ngIf="items.length; else empty">
        <article *ngFor="let item of items" class="row">
          <time>{{ item.occurredAt | date:'medium' }}</time>
          <div>
            <span class="badge">{{ item.actionType }}</span>
            <div [innerHTML]="renderDetail(item)"></div>
            <small>{{ item.ipAddress }}</small>
          </div>
        </article>
      </section>
      <ng-template #empty><p class="empty">No activity found.</p></ng-template>
      <footer *ngIf="totalCount > filter.pageSize">
        <button [disabled]="filter.page <= 1" (click)="prevPage()">Prev</button>
        <span>Page {{ filter.page }}</span>
        <button [disabled]="items.length < filter.pageSize" (click)="nextPage()">Next</button>
      </footer>
    </div>
  `,
  styles: [`
    .shell { min-height: 100vh; background: #0f172a; color: #f8fafc; padding: 2rem; font-family: Inter, sans-serif; }
    header, .filters, footer { display: flex; gap: 0.75rem; align-items: center; flex-wrap: wrap; margin-bottom: 1rem; }
    .row { display: grid; grid-template-columns: 11rem 1fr; gap: 1rem; padding: 0.85rem; background: #1e293b; border-radius: 8px; margin-bottom: 0.75rem; }
    .badge { background: #1d4ed8; padding: 0.1rem 0.45rem; border-radius: 999px; font-size: 0.75rem; }
    input, select, button { background: #111827; color: #f8fafc; border: 1px solid #334155; border-radius: 6px; padding: 0.45rem 0.65rem; }
    .empty { color: #94a3b8; text-align: center; padding: 2rem 0; }
  `]
})
export class ActivityMonitorComponent implements OnInit {
  username = '';
  items: ActivityItem[] = [];
  totalCount = 0;
  filter: ActivityFilter = { page: 1, pageSize: 25, actionType: '', from: '', to: '', search: '' };

  constructor(
    private activityService: ActivityService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.username = this.authService.getUsername();
    this.activityService.startPolling(this.filter, res => {
      this.items = res.items;
      this.totalCount = res.totalCount;
    });
    this.reload();
  }

  reload(): void {
    this.activityService.list(this.filter).subscribe(res => {
      this.items = res.items;
      this.totalCount = res.totalCount;
    });
  }

  renderDetail(item: ActivityItem): string {
    return this.activityService.buildDetailHtml(item);
  }

  onExport(): void {
    this.activityService.exportCsv(this.filter.from, this.filter.to);
  }

  prevPage(): void {
    if (this.filter.page > 1) {
      this.filter.page--;
      this.reload();
    }
  }

  nextPage(): void {
    this.filter.page++;
    this.reload();
  }
}
