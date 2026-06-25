import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';

interface QuoteResponse {
  text: string;
  index: number;
  total: number;
  label?: string;
}

@Component({
  selector: 'app-quotes',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="quotes-container">
      <div class="quotes-card">
        <div class="header">
          <div>
            <h1>Inspiration</h1>
            <p class="subtitle">Developer quotes to brighten your sprint</p>
          </div>
          <button (click)="goBack()" class="btn-back">Back to Dashboard</button>
        </div>

        <div class="quote-display" *ngIf="currentQuote; else emptyTpl">
          <span class="quote-icon">"</span>
          <blockquote>{{ currentQuote.text }}</blockquote>
          <p class="quote-meta" *ngIf="currentQuote.label">
            {{ currentQuote.label | titlecase }} · {{ currentQuote.index + 1 }} of {{ currentQuote.total }}
          </p>
          <p class="quote-meta" *ngIf="!currentQuote.label">
            Random pick · {{ currentQuote.index + 1 }} of {{ currentQuote.total }}
          </p>
        </div>

        <ng-template #emptyTpl>
          <div class="placeholder">
            <p>Press a button below to fetch a quote from the API.</p>
          </div>
        </ng-template>

        <div class="actions">
          <button (click)="fetchRandom()" [disabled]="loading" class="btn-primary">
            {{ loading && activeAction === 'random' ? 'Loading...' : 'Random Quote' }}
          </button>
          <button (click)="fetchDaily()" [disabled]="loading" class="btn-secondary">
            {{ loading && activeAction === 'daily' ? 'Loading...' : 'Quote of the Day' }}
          </button>
        </div>

        <div *ngIf="errorMessage" class="error-banner">
          {{ errorMessage }}
        </div>
      </div>
    </div>
  `,
  styles: [`
    .quotes-container {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: 100vh;
      background: linear-gradient(135deg, #0f172a 0%, #1e1b4b 100%);
      font-family: 'Inter', sans-serif;
      padding: 1.5rem;
    }
    .quotes-card {
      width: 100%;
      max-width: 640px;
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
      background: linear-gradient(90deg, #f472b6, #818cf8);
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
    }
    .subtitle {
      margin: 0.5rem 0 0;
      color: #94a3b8;
      font-size: 0.95rem;
    }
    .btn-back {
      padding: 0.5rem 1.25rem;
      background: rgba(255, 255, 255, 0.05);
      border: 1px solid rgba(255, 255, 255, 0.1);
      border-radius: 8px;
      color: #cbd5e1;
      font-size: 0.9rem;
      cursor: pointer;
      transition: all 0.3s ease;
    }
    .btn-back:hover {
      background: rgba(255, 255, 255, 0.1);
    }
    .quote-display {
      position: relative;
      background: rgba(15, 23, 42, 0.5);
      border: 1px solid rgba(255, 255, 255, 0.05);
      border-radius: 16px;
      padding: 2rem 2rem 1.5rem;
      margin-bottom: 2rem;
      min-height: 120px;
    }
    .quote-icon {
      position: absolute;
      top: -0.5rem;
      left: 1rem;
      font-size: 4rem;
      line-height: 1;
      color: rgba(244, 114, 182, 0.25);
      font-family: Georgia, serif;
    }
    blockquote {
      margin: 0;
      font-size: 1.15rem;
      line-height: 1.7;
      color: #e2e8f0;
      font-style: italic;
      padding-left: 1rem;
    }
    .quote-meta {
      margin: 1.25rem 0 0;
      font-size: 0.8rem;
      color: #64748b;
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }
    .placeholder {
      text-align: center;
      padding: 2rem;
      color: #64748b;
      margin-bottom: 2rem;
    }
    .actions {
      display: flex;
      gap: 1rem;
    }
    .btn-primary, .btn-secondary {
      flex: 1;
      padding: 0.75rem 1.25rem;
      border: none;
      border-radius: 10px;
      font-weight: 600;
      font-size: 0.95rem;
      cursor: pointer;
      transition: all 0.3s ease;
    }
    .btn-primary {
      background: linear-gradient(90deg, #f472b6, #a78bfa);
      color: white;
    }
    .btn-secondary {
      background: rgba(255, 255, 255, 0.08);
      border: 1px solid rgba(255, 255, 255, 0.12);
      color: #e2e8f0;
    }
    .btn-primary:hover:not(:disabled), .btn-secondary:hover:not(:disabled) {
      transform: translateY(-1px);
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.2);
    }
    .btn-primary:disabled, .btn-secondary:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }
    .error-banner {
      margin-top: 1.5rem;
      padding: 0.75rem;
      background: rgba(239, 68, 68, 0.15);
      border: 1px solid rgba(239, 68, 68, 0.3);
      color: #fca5a5;
      border-radius: 10px;
      text-align: center;
    }
    @keyframes fadeIn {
      from { opacity: 0; transform: translateY(20px); }
      to { opacity: 1; transform: translateY(0); }
    }
  `]
})
export class QuotesComponent implements OnInit {
  currentQuote: QuoteResponse | null = null;
  loading = false;
  errorMessage = '';
  activeAction: 'random' | 'daily' | null = null;

  constructor(
    private http: HttpClient,
    private router: Router
  ) {}

  ngOnInit() {
    this.fetchDaily();
  }

  fetchRandom() {
    this.loadQuote('/api/quotes/random', 'random');
  }

  fetchDaily() {
    this.loadQuote('/api/quotes/daily', 'daily');
  }

  private loadQuote(url: string, action: 'random' | 'daily') {
    this.loading = true;
    this.activeAction = action;
    this.errorMessage = '';

    this.http.get<QuoteResponse>(url).subscribe({
      next: (res) => {
        this.currentQuote = res;
        this.loading = false;
        this.activeAction = null;
      },
      error: (err) => {
        this.loading = false;
        this.activeAction = null;
        this.errorMessage = err.error?.message || 'Failed to load quote.';
      }
    });
  }

  goBack() {
    this.router.navigate(['/dashboard']);
  }
}
