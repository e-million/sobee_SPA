import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { CartService } from '../../../core/services/cart.service';

interface LoginFormModel {
  email: string;
  password: string;
  rememberMe: boolean;
}

@Component({
  selector: 'app-login',
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './login.html',
  styleUrl: './login.css'
})
export class Login {
  loading = signal(false);
  submitAttempted = signal(false);
  apiError = signal('');
  fieldErrors = signal<Record<string, string>>({});

  form: LoginFormModel = {
    email: '',
    password: '',
    rememberMe: false
  };

  constructor(
    private authService: AuthService,
    private cartService: CartService,
    private router: Router
  ) {}

  onSubmit(form: NgForm) {
    this.submitAttempted.set(true);
    this.apiError.set('');
    this.fieldErrors.set({});

    if (form.invalid) {
      form.control.markAllAsTouched();
      return;
    }

    this.loading.set(true);

    this.authService.login({ email: this.form.email, password: this.form.password }).subscribe({
      next: () => {
        this.syncCartAndRedirect();
      },
      error: (err) => {
        this.loading.set(false);
        const code = err?.originalError?.error?.code;

        if (code === 'INVALID_CREDENTIALS') {
          this.apiError.set('Invalid email or password.');
          return;
        }

        if (code === 'ACCOUNT_LOCKED') {
          this.apiError.set('Your account is locked. Please contact support.');
          return;
        }

        const serverErrors = err?.originalError?.error?.details?.errors;
        if (Array.isArray(serverErrors)) {
          const errorMap: Record<string, string> = {};
          for (const serverError of serverErrors) {
            if (serverError?.field) {
              errorMap[serverError.field] = serverError.message || 'Invalid value';
            }
          }
          this.fieldErrors.set(errorMap);
          return;
        }

        this.apiError.set(err?.message || 'Login failed. Please try again.');
      }
    });
  }

  private syncCartAndRedirect() {
    this.cartService.getCart().subscribe({
      next: () => this.finishLogin(),
      error: () => this.finishLogin()
    });
  }

  private finishLogin() {
    if (this.authService.isAuthenticated() && localStorage.getItem('guestSessionId')) {
      this.authService.clearGuestSession();
    }

    const returnUrl = localStorage.getItem('returnUrl') || '/';
    localStorage.removeItem('returnUrl');
    this.loading.set(false);
    this.router.navigateByUrl(returnUrl);
  }
}
