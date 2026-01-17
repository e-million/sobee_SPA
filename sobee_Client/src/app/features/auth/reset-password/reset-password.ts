import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-reset-password',
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './reset-password.html',
  styleUrl: './reset-password.css'
})
export class ResetPassword implements OnInit {
  loading = signal(false);
  submitAttempted = signal(false);
  success = signal(false);
  apiError = signal('');
  tokenMissing = signal(false);

  password = '';
  confirmPassword = '';
  private token = '';

  constructor(
    private authService: AuthService,
    private route: ActivatedRoute
  ) {}

  ngOnInit() {
    this.token = this.route.snapshot.queryParamMap.get('token') || '';
    if (!this.token) {
      this.tokenMissing.set(true);
      this.apiError.set('Reset token is missing. Please request a new link.');
    }
  }

  get passwordsMatch(): boolean {
    return this.password === this.confirmPassword;
  }

  onSubmit(form: NgForm) {
    this.submitAttempted.set(true);
    this.apiError.set('');

    if (this.tokenMissing()) {
      return;
    }

    if (!this.passwordsMatch) {
      return;
    }

    if (form.invalid) {
      form.control.markAllAsTouched();
      return;
    }

    this.loading.set(true);

    this.authService.resetPassword({ token: this.token, newPassword: this.password }).subscribe({
      next: () => {
        this.loading.set(false);
        this.success.set(true);
      },
      error: (err) => {
        this.loading.set(false);
        const code = err?.originalError?.error?.code;

        if (code === 'INVALID_TOKEN') {
          this.apiError.set('This reset link is invalid. Please request a new one.');
          return;
        }

        if (code === 'TOKEN_EXPIRED') {
          this.apiError.set('This reset link has expired. Please request a new one.');
          return;
        }

        this.apiError.set(err?.message || 'Unable to reset password.');
      }
    });
  }
}
