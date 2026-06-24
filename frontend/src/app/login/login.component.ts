import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="login-container">
      <div class="login-card">
        <div class="logo">
          <h2>Antigravity Reviewer</h2>
          <p>Sign in to your demo account</p>
        </div>
        
        <form (ngSubmit)="onSubmit()" #loginForm="ngForm">
          <div class="form-group">
            <label for="username">Username</label>
            <input 
              type="text" 
              id="username" 
              name="username" 
              [(ngModel)]="username" 
              required 
              placeholder="Enter username (admin)" 
              class="form-control"
            />
          </div>
          
          <div class="form-group">
            <label for="password">Password</label>
            <input 
              type="password" 
              id="password" 
              name="password" 
              [(ngModel)]="password" 
              required 
              placeholder="Enter password (admin123)" 
              class="form-control"
            />
          </div>
          
          <button type="submit" [disabled]="loginForm.invalid || loading" class="btn-submit">
            <span *ngIf="!loading">Sign In</span>
            <span *ngIf="loading" class="spinner"></span>
          </button>
        </form>
        
        <div *ngIf="errorMessage" class="error-banner">
          {{ errorMessage }}
        </div>
      </div>
    </div>
  `,
  styles: [`
    .login-container {
      display: flex;
      justify-content: center;
      align-items: center;
      height: 100vh;
      background: linear-gradient(135deg, #0f172a 0%, #1e1b4b 100%);
      font-family: 'Inter', sans-serif;
    }
    .login-card {
      width: 100%;
      max-width: 400px;
      padding: 2.5rem;
      background: rgba(30, 41, 59, 0.7);
      backdrop-filter: blur(16px);
      border: 1px solid rgba(255, 255, 255, 0.08);
      border-radius: 20px;
      box-shadow: 0 20px 40px rgba(0, 0, 0, 0.3);
      color: #f8fafc;
      animation: fadeIn 0.6s ease-out;
    }
    .logo {
      text-align: center;
      margin-bottom: 2rem;
    }
    .logo h2 {
      margin: 0;
      font-size: 1.8rem;
      font-weight: 700;
      letter-spacing: -0.025em;
      background: linear-gradient(90deg, #38bdf8, #818cf8);
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
    }
    .logo p {
      margin: 0.5rem 0 0;
      color: #94a3b8;
      font-size: 0.9rem;
    }
    .form-group {
      margin-bottom: 1.5rem;
      display: flex;
      flex-direction: column;
    }
    label {
      margin-bottom: 0.5rem;
      font-size: 0.85rem;
      font-weight: 500;
      color: #cbd5e1;
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }
    .form-control {
      padding: 0.75rem 1rem;
      background: rgba(15, 23, 42, 0.6);
      border: 1px solid rgba(255, 255, 255, 0.1);
      border-radius: 8px;
      color: #f8fafc;
      font-size: 0.95rem;
      transition: all 0.3s ease;
    }
    .form-control:focus {
      outline: none;
      border-color: #6366f1;
      box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.2);
    }
    .btn-submit {
      width: 100%;
      padding: 0.75rem;
      background: linear-gradient(90deg, #4f46e5, #6366f1);
      border: none;
      border-radius: 8px;
      color: #ffffff;
      font-size: 1rem;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.3s ease;
      display: flex;
      justify-content: center;
      align-items: center;
    }
    .btn-submit:hover:not(:disabled) {
      transform: translateY(-2px);
      box-shadow: 0 8px 20px rgba(99, 102, 241, 0.4);
    }
    .btn-submit:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }
    .error-banner {
      margin-top: 1.5rem;
      padding: 0.75rem 1rem;
      background: rgba(239, 68, 68, 0.15);
      border: 1px solid rgba(239, 68, 68, 0.3);
      border-radius: 8px;
      color: #fca5a5;
      font-size: 0.875rem;
      text-align: center;
      animation: shake 0.4s ease;
    }
    .spinner {
      width: 20px;
      height: 20px;
      border: 2px solid rgba(255, 255, 255, 0.3);
      border-radius: 50%;
      border-top-color: #ffffff;
      animation: spin 0.8s linear infinite;
    }
    @keyframes fadeIn {
      from { opacity: 0; transform: translateY(20px); }
      to { opacity: 1; transform: translateY(0); }
    }
    @keyframes spin {
      to { transform: rotate(360deg); }
    }
    @keyframes shake {
      0%, 100% { transform: translateX(0); }
      25% { transform: translateX(-5px); }
      75% { transform: translateX(5px); }
    }
  `]
})
export class LoginComponent {
  username = '';
  password = '';
  loading = false;
  errorMessage = '';

  constructor(private authService: AuthService, private router: Router) {
    if (this.authService.isLoggedIn()) {
      this.router.navigate(['/dashboard']);
    }
  }

  onSubmit() {
    this.loading = true;
    this.errorMessage = '';
    
    this.authService.login(this.username, this.password).subscribe({
      next: () => {
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = err.error?.message || 'Login failed. Please try again.';
      }
    });
  }
}
