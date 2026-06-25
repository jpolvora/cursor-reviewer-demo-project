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
          <h3>Available Documents</h3>
          <ul class="doc-list" *ngIf="documents && documents.length > 0; else noDocs">
            <li *ngFor="let doc of documents" class="doc-item">
              <div>
                <span class="doc-name">{{ doc.name }}</span>
                <span class="doc-size"> ({{ (doc.size / 1024).toFixed(1) }} KB)</span>
              </div>
              <button (click)="selectDocument(doc.name)" class="btn btn-sm">Select</button>
            </li>
          </ul>
          <ng-template #noDocs>
            <p class="no-docs">No documents available.</p>
          </ng-template>
        </div>
        
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
    .btn-sm {
      padding: 0.25rem 0.5rem;
      font-size: 0.8rem;
    }
    .meta {
      font-size: 0.8rem;
      color: #64748b;
    }
    .doc-list {
      list-style: none;
      padding: 0;
      margin: 1rem 0;
    }
    .doc-item {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 0.5rem;
      background: #1e293b;
      border: 1px solid #334155;
      margin-bottom: 0.5rem;
      border-radius: 4px;
    }
    .doc-name {
      font-size: 0.9rem;
      font-weight: 500;
    }
    .doc-size {
      font-size: 0.8rem;
      color: #94a3b8;
    }
    .no-docs {
      color: #64748b;
      font-style: italic;
    }
  `]
})
export class DocumentComponent implements OnInit {
  downloadFileName = '';
  statusMarkup = '';
  trustedPreview = '';
  documents: any[] = [];

  constructor(
    private http: HttpClient,
    private router: Router
  ) {}

  ngOnInit() {
    this.statusMarkup = '<strong>Portal Active</strong>';
    this.updatePreview();
    this.loadDocuments();
  }

  updatePreview() {
    // Fix: No bypassSecurityTrustHtml bypass used
    this.trustedPreview = this.statusMarkup;
  }

  loadDocuments() {
    this.http.get<any[]>('/api/documents/list').subscribe({
      next: (data) => {
        this.documents = data;
      },
      error: (err) => {
        console.error('Error loading documents:', err);
      }
    });
  }

  selectDocument(name: string) {
    this.downloadFileName = name;
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

