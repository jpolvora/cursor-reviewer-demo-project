import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { AuthService } from '../auth.service';

interface AuditEntry {
  id: number;
  userId: number;
  username: string;
  action: string;
  details: string;
  ipAddress: string;
  timestamp: string;
}

interface AuditResponse {
  total: number;
  page: number;
  pageSize: number;
  data: AuditEntry[];
}

@Component({
  selector: 'app-audit',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="audit-container">
      <div class="audit-card">
        <div class="header">
          <div>
            <h1>Audit Log</h1>
            <p class="subtitle">System-wide activity log</p>
          </div>
          <button (click)="onBack()" class="btn-back">Back to Dashboard</button>
        </div>

        <div class="controls">
          <label for="filterAction">Filter by action:</label>
          <select id="filterAction" (change)="onFilterChange($event)" class="form-control">
            <option value="">All actions</option>
            <option value="login">Login</option>
            <option value="logout">Logout</option>
            <option value="profile_update">Profile Update</option>
            <option value="password_change">Password Change</option>
            <option value="document_download">Document Download</option>
            <option value="document_list">Document List</option>
          </select>
        </div>

        <div class="table-wrapper" *ngIf="logs && logs.length > 0; else emptyTpl">
          <table>
            <thead>
              <tr>
                <th>ID</th>
                <th>User</th>
                <th>Action</th>
                <th>Details</th>
                <th>IP Address</th>
                <th>Timestamp</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let log of logs">
                <td>{{ log.id }}</td>
                <td>{{ log.username }}</td>
                <td>
                  <span class="badge" [ngClass]="'badge-' + log.action">{{ log.action }}</span>
                </td>
                <td class="details-col">{{ log.details }}</td>
                <td>{{ log.ipAddress }}</td>
                <td>{{ log.timestamp | date:'medium' }}</td>
              </tr>
            </tbody>
          </table>
        </div>

        <ng-template #emptyTpl>
          <div class="empty">No audit records found.</div>
        </ng-template>

        <div class="pagination" *ngIf="total > pageSize">
          <button (click)="prevPage()" [disabled]="currentPage <= 1" class="btn-page">Previous</button>
          <span>Page {{ currentPage }} of {{ totalPages }}</span>
          <button (click)="nextPage()" [disabled]="currentPage >= totalPages" class="btn-page">Next</button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .audit-container {
      display: flex;
      justify-content: center;
      align-items: flex-start;
      min-height: 100vh;
      background: linear-gradient(135deg, #0f172a 0%, #1e1b4b 100%);
      font-family: 'Inter', sans-serif;
      padding: 1.5rem;
    }
    .audit-card {
      width: 100%;
      max-width: 960px;
      padding: 2rem;
      background: rgba(30, 41, 59, 0.7);
      backdrop-filter: blur(16px);
      border: 1px solid rgba(255, 255, 255, 0.08);
      border-radius: 20px;
      box-shadow: 0 20px 40px rgba(0, 0, 0, 0.3);
      color: #f8fafc;
      animation: fadeIn 0.6s ease-out;
    }
    .header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      border-bottom: 1px solid rgba(255, 255, 255, 0.1);
      padding-bottom: 1.5rem;
      margin-bottom: 1.5rem;
    }
    .header h1 {
      margin: 0;
      font-size: 2rem;
      font-weight: 700;
      background: linear-gradient(90deg, #38bdf8, #818cf8);
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
    }
    .subtitle {
      margin: 0.25rem 0 0;
      color: #94a3b8;
      font-size: 0.9rem;
    }
    .btn-back {
      padding: 0.5rem 1.25rem;
      background: rgba(56, 189, 248, 0.1);
      border: 1px solid rgba(56, 189, 248, 0.3);
      border-radius: 8px;
      color: #7dd3fc;
      font-size: 0.9rem;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.3s ease;
    }
    .btn-back:hover {
      background: rgba(56, 189, 248, 0.25);
      transform: translateY(-1px);
    }
    .controls {
      margin-bottom: 1.5rem;
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }
    .controls label {
      color: #94a3b8;
      font-size: 0.85rem;
    }
    .form-control {
      padding: 0.4rem 0.75rem;
      background: #1e293b;
      border: 1px solid #475569;
      border-radius: 6px;
      color: #f8fafc;
      font-size: 0.85rem;
    }
    .table-wrapper {
      overflow-x: auto;
    }
    table {
      width: 100%;
      border-collapse: collapse;
    }
    th {
      text-align: left;
      padding: 0.6rem 0.75rem;
      color: #94a3b8;
      font-size: 0.75rem;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      border-bottom: 1px solid rgba(255, 255, 255, 0.08);
    }
    td {
      padding: 0.6rem 0.75rem;
      font-size: 0.85rem;
      border-bottom: 1px solid rgba(255, 255, 255, 0.04);
    }
    .details-col {
      max-width: 240px;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }
    .badge {
      display: inline-block;
      padding: 0.15rem 0.5rem;
      border-radius: 4px;
      font-size: 0.7rem;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.03em;
    }
    .badge-login { background: rgba(34, 197, 94, 0.2); color: #86efac; }
    .badge-logout { background: rgba(239, 68, 68, 0.2); color: #fca5a5; }
    .badge-profile_update { background: rgba(99, 102, 241, 0.2); color: #a5b4fc; }
    .badge-password_change { background: rgba(234, 179, 8, 0.2); color: #fde68a; }
    .badge-document_download { background: rgba(56, 189, 248, 0.2); color: #7dd3fc; }
    .badge-document_list { background: rgba(168, 85, 247, 0.2); color: #c4b5fd; }
    .empty {
      text-align: center;
      padding: 3rem 0;
      color: #64748b;
    }
    .pagination {
      display: flex;
      justify-content: center;
      align-items: center;
      gap: 1rem;
      margin-top: 1.5rem;
      color: #94a3b8;
      font-size: 0.85rem;
    }
    .btn-page {
      padding: 0.35rem 0.75rem;
      background: rgba(56, 189, 248, 0.1);
      border: 1px solid rgba(56, 189, 248, 0.2);
      border-radius: 6px;
      color: #7dd3fc;
      font-size: 0.8rem;
      cursor: pointer;
    }
    .btn-page:disabled {
      opacity: 0.4;
      cursor: default;
    }
    .btn-page:not(:disabled):hover {
      background: rgba(56, 189, 248, 0.25);
    }
  `]
})
export class AuditComponent implements OnInit {
  logs: AuditEntry[] = [];
  currentPage = 1;
  pageSize = 50;
  total = 0;
  filterAction = '';

  constructor(
    private http: HttpClient,
    private router: Router,
    private authService: AuthService
  ) {}

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.total / this.pageSize));
  }

  ngOnInit() {
    this.loadLogs();
  }

  loadLogs() {
    let url = `/api/audit?page=${this.currentPage}&pageSize=${this.pageSize}`;
    if (this.filterAction) {
      url += `&action=${this.filterAction}`;
    }

    this.http.get<AuditResponse>(url).subscribe({
      next: (res) => {
        this.logs = res.data.filter(entry => {
          if (!this.filterAction) return true;
          return entry.action === this.filterAction;
        });
        this.total = this.filterAction ? this.logs.length : res.total;
      },
      error: () => {
        this.authService.logout().subscribe(() => {
          this.router.navigate(['/login']);
        });
      }
    });
  }

  onFilterChange(event: Event) {
    const select = event.target as HTMLSelectElement;
    this.filterAction = select.value;
    this.currentPage = 1;
    this.loadLogs();
  }

  prevPage() {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.loadLogs();
    }
  }

  nextPage() {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      this.loadLogs();
    }
  }

  onBack() {
    this.router.navigate(['/dashboard']);
  }
}
