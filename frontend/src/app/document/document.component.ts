import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';

@Component({
  selector: 'app-document',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="doc-container">
      <div class="doc-card">
        <h2>Document Portal</h2>
        
        <div class="form-group">
          <!-- Fix: Accessibility label added with matching for and id on input -->
          <label for="downloadFileName">Document filename:</label>
          <input 
            id="downloadFileName"
            type="text" 
            [(ngModel)]="downloadFileName" 
            placeholder="Document filename (e.g. report.pdf)"
            class="form-control"
          />
          <button (click)="onDownload()" class="btn">Download</button>
        </div>

        <div class="form-group">
          <!-- Fix: bypassSecurityTrustHtml removed. Default Angular sanitization active. -->
          <label>Status Preview:</label>
          <div [innerHTML]="trustedPreview"></div>
          <input 
            type="text" 
            [(ngModel)]="statusMarkup" 
            (ngModelChange)="updatePreview()" 
            placeholder="HTML Status Markup" 
            class="form-control"
          />
        </div>
      </div>
    </div>
  `,
  styles: [`
    .doc-container {
      padding: 2rem;
      background: #1e293b;
      color: #f8fafc;
      min-height: 100vh;
    }
    .doc-card {
      max-width: 600px;
      margin: 0 auto;
      background: #0f172a;
      padding: 2rem;
      border-radius: 12px;
      border: 1px solid #334155;
    }
    .form-group {
      margin-bottom: 1.5rem;
    }
    .form-control {
      width: 100%;
      padding: 0.5rem;
      margin-bottom: 0.5rem;
      background: #1e293b;
      border: 1px solid #475569;
      color: #f8fafc;
    }
    .btn {
      padding: 0.5rem 1rem;
      background: #3b82f6;
      color: white;
      border: none;
      cursor: pointer;
      border-radius: 4px;
    }
    .meta {
      font-size: 0.8rem;
      color: #64748b;
    }
  `]
})
export class DocumentComponent implements OnInit {
  downloadFileName = '';
  statusMarkup = '';
  trustedPreview = '';

  constructor(
    private http: HttpClient,
    private router: Router
  ) {}

  ngOnInit() {
    this.statusMarkup = '<strong>Portal Active</strong>';
    this.updatePreview();
  }

  updatePreview() {
    // Fix: No bypassSecurityTrustHtml bypass used
    this.trustedPreview = this.statusMarkup;
  }

  onDownload() {
    // Fix: console.log secret leak removed
    // Fix: encodeURIComponent added to sanitize query string
    this.http.get(`/api/documents/download?fileName=${encodeURIComponent(this.downloadFileName)}`, { responseType: 'blob' }).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = this.downloadFileName;
        a.click();
      },
      error: (err) => {
        console.error('Download error occurred:', err);
      }
    });
  }
}

