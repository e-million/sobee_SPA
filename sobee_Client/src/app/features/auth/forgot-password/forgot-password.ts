import { Component, OnDestroy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-forgot-password',
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './forgot-password.html',
  styleUrl: './forgot-password.css'
})
export class ForgotPassword implements OnDestroy {
  loading = signal(false);
  submitAttempted = signal(false);
  sent = signal(false);
  cooldown = signal(0);
  apiError = signal('');

  email = '';
  private cooldownTimer: ReturnType<typeof setInterval> | null = null;

  constructor(private authService: AuthService) {}

  onSubmit(form: NgForm) {
    this.submitAttempted.set(true);
    this.apiError.set('');

    if (form.invalid) {
      form.control.markAllAsTouched();
      return;
    }

    if (this.cooldown() > 0) {
      return;
    }

    this.loading.set(true);

    this.authService.forgotPassword({ email: this.email }).subscribe({
      next: () => {
        this.loading.set(false);
        this.sent.set(true);
        this.startCooldown(60);
      },
      error: (err) => {
        this.loading.set(false);
        this.apiError.set(err?.message || 'Unable to send reset email.');
      }
    });
  }

  ngOnDestroy() {
    if (this.cooldownTimer) {
      clearInterval(this.cooldownTimer);
    }
  }

  private startCooldown(seconds: number) {
    this.cooldown.set(seconds);

    if (this.cooldownTimer) {
      clearInterval(this.cooldownTimer);
    }

    this.cooldownTimer = setInterval(() => {
      this.cooldown.update(value => {
        if (value <= 1) {
          clearInterval(this.cooldownTimer!);
          this.cooldownTimer = null;
          return 0;
        }
        return value - 1;
      });
    }, 1000);
  }
}
