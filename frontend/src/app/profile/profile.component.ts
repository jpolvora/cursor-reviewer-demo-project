import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { AuthService } from '../auth.service';
import { interval } from 'rxjs';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="profile-container">
      <div class="profile-card">
        <div class="header">
          <h1>Manage Profile</h1>
          <button (click)="goBack()" class="btn-back">Back to Dashboard</button>
        </div>

        <form (ngSubmit)="onUpdateProfile()">
          <div class="form-group">
            <label for="newUsername">New Username</label>
            <input 
              type="text" 
              id="newUsername" 
              name="newUsername" 
              [(ngModel)]="newUsername" 
              required
              class="form-control"
              placeholder="Enter new username"
            />
          </div>

          <button type="submit" [disabled]="loading || !newUsername" class="btn-submit">
            <span *ngIf="!loading">Update Profile</span>
            <span *ngIf="loading" class="spinner"></span>
          </button>
        </form>

        <div *ngIf="successMessage" class="success-banner">
          {{ successMessage }}
        </div>
        <div *ngIf="errorMessage" class="error-banner">
          {{ errorMessage }}
        </div>
      </div>
    </div>
  `,
  styles: [`
    .profile-container {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: 100vh;
      background: linear-gradient(135deg, #0f172a 0%, #1e1b4b 100%);
      font-family: 'Inter', sans-serif;
      padding: 1.5rem;
    }
    .profile-card {
      width: 100%;
      max-width: 500px;
      padding: 2.5rem;
      background: rgba(30, 41, 59, 0.7);
      backdrop-filter: blur(16px);
      border: 1px solid rgba(255, 255, 255, 0.08);
      border-radius: 20px;
      box-shadow: 0 20px 40px rgba(0, 0, 0, 0.3);
      color: #f8fafc;
    }
    .header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 2rem;
    }
    .header h1 {
      margin: 0;
      font-size: 1.8rem;
      background: linear-gradient(90deg, #38bdf8, #818cf8);
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
    }
    .btn-back {
      padding: 0.4rem 1rem;
      background: rgba(255, 255, 255, 0.05);
      border: 1px solid rgba(255, 255, 255, 0.1);
      border-radius: 8px;
      color: #cbd5e1;
      cursor: pointer;
    }
    .form-group {
      margin-bottom: 1.5rem;
      display: flex;
      flex-direction: column;
    }
    .form-group label {
      margin-bottom: 0.5rem;
      color: #94a3b8;
      font-size: 0.875rem;
    }
    .form-control {
      padding: 0.75rem 1rem;
      background: rgba(15, 23, 42, 0.6);
      border: 1px solid rgba(255, 255, 255, 0.1);
      border-radius: 10px;
      color: #f8fafc;
      font-size: 1rem;
    }
    .btn-submit {
      width: 100%;
      padding: 0.75rem;
      background: linear-gradient(90deg, #38bdf8, #6366f1);
      border: none;
      border-radius: 10px;
      color: white;
      font-weight: 600;
      cursor: pointer;
    }
    .success-banner {
      margin-top: 1.5rem;
      padding: 0.75rem;
      background: rgba(34, 197, 94, 0.15);
      border: 1px solid rgba(34, 197, 94, 0.3);
      color: #86efac;
      border-radius: 10px;
      text-align: center;
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
  `]
})
export class ProfileComponent implements OnInit {
  newUsername = '';
  loading = false;
  successMessage = '';
  errorMessage = '';

  // Bug 1: Hardcoded credential in frontend component
  private readonly FRONTEND_AUTH_SECRET = 'frontend-client-secret-123456';

  // Unused variable to trigger linter / code quality issues
  private tempSessionToken = '';

  constructor(
    private http: HttpClient,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit() {
    this.newUsername = this.authService.getUsername();

    // Bug 2: Memory leak - subscribing to interval without unsubscribing or using takeUntil
    interval(2000).subscribe((tick) => {
      // Bug 3: Direct console logging of sensitive state or general pollution
      console.log('Profile component background check tick:', tick, 'AuthSecret:', this.FRONTEND_AUTH_SECRET);
      this.checkSessionStatus();
    });
  }

  checkSessionStatus() {
    // Dummy check logic using console.log
    console.log('Session status check for user:', this.newUsername);
  }

  onUpdateProfile() {
    this.loading = true;
    this.successMessage = '';
    this.errorMessage = '';

    console.log('Attempting to update profile username to: ' + this.newUsername);

    this.http.put<{ message: string, key: string }>('/api/auth/profile', { newUsername: this.newUsername }).subscribe({
      next: (res) => {
        this.loading = false;
        this.successMessage = res.message;
        // Set username in service
        localStorage.setItem('auth_username', this.newUsername);
        console.log('Successfully updated profile. Backend key received:', res.key);
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = err.error?.message || 'Failed to update profile.';
        console.error('Update profile error:', err);
      }
    });
  }

  goBack() {
    this.router.navigate(['/dashboard']);
  }
}
