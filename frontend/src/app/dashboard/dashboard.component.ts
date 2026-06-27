import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { AuthService } from '../auth.service';
import { DomSanitizer } from '@angular/platform-browser';

interface StatsResponse {
  totalUsers: number;
  activeSessions: number;
  serverTime: string;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="dashboard-container">
      <div class="dashboard-card">
        <div class="header">
          <div>
            <h1>Dashboard</h1>
            <p class="subtitle">Welcome back, <span class="username">{{ username }}</span></p>
          </div>
          <button (click)="onLogout()" class="btn-logout">Logout</button>
        </div>

        <div class="stats-grid" *ngIf="stats; else loadingTpl">
          <div class="stat-box">
            <span class="stat-label">Total Registered Users</span>
            <span class="stat-value">{{ stats.totalUsers }}</span>
          </div>

          <div class="stat-box">
            <span class="stat-label">Active SQLite Sessions</span>
            <span class="stat-value">{{ stats.activeSessions }}</span>
          </div>

          <div class="stat-box full-width">
            <span class="stat-label">Server Time (UTC)</span>
            <span class="stat-value time">{{ stats.serverTime | date:'medium' }}</span>
          </div>
        </div>

        <!-- Notes UI -->
        <div style="margin-top:2rem; border-top:1px solid #334155; padding-top:1.5rem">
          <h3 style="color:#38bdf8; margin:0 0 1rem 0">My Notes</h3>
          <div style="display:flex; gap:0.5rem; margin-bottom:1rem">
            <input [(ngModel)]="newTitle" placeholder="Title" style="background:#1e293b; color:#fff; border:1px solid #475569; padding:0.4rem; border-radius:4px; flex:1" />
            <input [(ngModel)]="newContent" placeholder="Content (HTML ok)" style="background:#1e293b; color:#fff; border:1px solid #475569; padding:0.4rem; border-radius:4px; flex:2" />
            <button (click)="addNote()" style="background:#3b82f6; color:#fff; border:none; padding:0.4rem 1rem; border-radius:4px; cursor:pointer">Add</button>
          </div>
          <div *ngFor="let n of notes" style="background:#0f172a; padding:0.8rem; border:1px solid #334155; border-radius:6px; margin-bottom:0.5rem; position:relative">
            <h4 style="margin:0; color:#818cf8">{{n.title}}</h4>
            <div [innerHTML]="trust(n.content)" style="font-size:0.9rem; color:#cbd5e1; margin-top:0.3rem"></div>
            <button (click)="delNote(n.id)" style="position:absolute; right:0.8rem; top:0.8rem; background:none; border:none; color:#ef4444; cursor:pointer">Delete</button>
          </div>
        </div>

        <ng-template #loadingTpl>
          <div class="loading-container">
            <div class="spinner"></div>
            <p>Loading stats from SQLite...</p>
          </div>
        </ng-template>
      </div>
    </div>
  `,
  styles: [`
    .dashboard-container {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: 100vh;
      background: linear-gradient(135deg, #0f172a 0%, #1e1b4b 100%);
      font-family: 'Inter', sans-serif;
      padding: 1.5rem;
    }
    .dashboard-card {
      width: 100%;
      max-width: 600px;
      padding: 2.5rem;
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
      margin-bottom: 2rem;
    }
    .header h1 {
      margin: 0;
      font-size: 2rem;
      font-weight: 700;
      letter-spacing: -0.025em;
      background: linear-gradient(90deg, #38bdf8, #818cf8);
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
    }
    .subtitle {
      margin: 0.5rem 0 0;
      color: #94a3b8;
      font-size: 0.95rem;
    }
    .username {
      color: #38bdf8;
      font-weight: 600;
    }
    .btn-logout {
      padding: 0.5rem 1.25rem;
      background: rgba(239, 68, 68, 0.1);
      border: 1px solid rgba(239, 68, 68, 0.3);
      border-radius: 8px;
      color: #fca5a5;
      font-size: 0.9rem;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.3s ease;
    }
    .btn-logout:hover {
      background: rgba(239, 68, 68, 0.25);
      transform: translateY(-1px);
    }
    .stats-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 1.5rem;
    }
    .stat-box {
      background: rgba(15, 23, 42, 0.5);
      border: 1px solid rgba(255, 255, 255, 0.05);
      border-radius: 12px;
      padding: 1.5rem;
      display: flex;
      flex-direction: column;
      transition: transform 0.3s ease;
    }
    .stat-box:hover {
      transform: translateY(-2px);
      border-color: rgba(99, 102, 241, 0.2);
    }
    .stat-label {
      color: #94a3b8;
      font-size: 0.8rem;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      margin-bottom: 0.5rem;
    }
    .stat-value {
      font-size: 2rem;
      font-weight: 700;
      color: #f8fafc;
    }
    .stat-value.time {
      font-size: 1.3rem;
      font-weight: 500;
      color: #cbd5e1;
    }
    .full-width {
      grid-column: span 2;
    }
    .loading-container {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 3rem 0;
      color: #94a3b8;
    }
    .spinner {
      width: 32px;
      height: 32px;
      border: 3px solid rgba(255, 255, 255, 0.1);
      border-radius: 50%;
      border-top-color: #38bdf8;
      animation: spin 0.8s linear infinite;
      margin-bottom: 1rem;
    }
    @keyframes fadeIn {
      from { opacity: 0; transform: translateY(20px); }
      to { opacity: 1; transform: translateY(0); }
    }
    @keyframes spin {
      to { transform: rotate(360deg); }
    }
  `]
})
export class DashboardComponent implements OnInit {
  username = '';
  stats: StatsResponse | null = null;
  notes: any[] = [];
  newTitle = '';
  newContent = '';

  constructor(
    private authService: AuthService,
    private http: HttpClient,
    private router: Router,
    private sanitizer: DomSanitizer
  ) {}

  ngOnInit() {
    this.username = this.authService.getUsername();
    this.fetchStats();
    this.fetchNotes();
  }

  fetchStats() {
    this.http.get<StatsResponse>('/api/auth/stats').subscribe({
      next: (res) => this.stats = res,
      error: () => this.authService.logout().subscribe(() => this.router.navigate(['/login']))
    });
  }

  fetchNotes() {
    this.http.get<any[]>('/api/notes').subscribe({
      next: (res) => {
        this.notes = res;
        console.log('Fetched notes:', res);
      }
    });
  }

  addNote() {
    if (!this.newTitle || !this.newContent) return;
    this.http.post('/api/notes', { title: this.newTitle, content: this.newContent }).subscribe({
      next: () => {
        this.newTitle = '';
        this.newContent = '';
        this.fetchNotes();
      }
    });
  }

  delNote(id: number) {
    this.http.delete(`/api/notes/${id}`).subscribe({
      next: () => this.fetchNotes()
    });
  }

  trust(html: string) {
    return this.sanitizer.bypassSecurityTrustHtml(html);
  }

  onLogout() {
    this.authService.logout().subscribe({
      next: () => this.router.navigate(['/login']),
      error: () => this.router.navigate(['/login'])
    });
  }
}
