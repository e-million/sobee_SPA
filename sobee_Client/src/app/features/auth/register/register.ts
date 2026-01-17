import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { CartService } from '../../../core/services/cart.service';
import { ToastService } from '../../../core/services/toast.service';

interface RegisterFormModel {
  firstName: string;
  lastName: string;
  email: string;
  billingAddress: string;
  shippingAddress: string;
  password: string;
  confirmPassword: string;
  acceptTerms: boolean;
}

@Component({
  selector: 'app-register',
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './register.html',
  styleUrl: './register.css'
})
export class Register {
  loading = signal(false);
  submitAttempted = signal(false);
  apiError = signal('');
  fieldErrors = signal<Record<string, string>>({});

  form: RegisterFormModel = {
    firstName: '',
    lastName: '',
    email: '',
    billingAddress: '',
    shippingAddress: '',
    password: '',
    confirmPassword: '',
    acceptTerms: false
  };

  constructor(
    private authService: AuthService,
    private cartService: CartService,
    private toastService: ToastService,
    private router: Router
  ) {}

  get passwordsMatch(): boolean {
    return this.form.password === this.form.confirmPassword;
  }

  onSubmit(form: NgForm) {
    this.submitAttempted.set(true);
    this.apiError.set('');
    this.fieldErrors.set({});

    if (!this.passwordsMatch) {
      return;
    }

    if (form.invalid) {
      form.control.markAllAsTouched();
      return;
    }

    this.loading.set(true);

    this.authService.register({
      email: this.form.email,
      password: this.form.password,
      firstName: this.form.firstName,
      lastName: this.form.lastName,
      billingAddress: this.form.billingAddress,
      shippingAddress: this.form.shippingAddress
    }).subscribe({
      next: (response) => {
        const accessToken = (response as { accessToken?: string } | null)?.accessToken;
        const refreshToken = (response as { refreshToken?: string } | null)?.refreshToken;

        if (accessToken && refreshToken) {
          this.syncCartAndRedirect();
          return;
        }

        this.loading.set(false);
        this.toastService.success('Registration successful. Please sign in.');
        this.router.navigate(['/login']);
      },
      error: (err) => {
        this.loading.set(false);
        const code = err?.originalError?.error?.code;

        if (code === 'EMAIL_EXISTS') {
          this.fieldErrors.set({ email: 'That email is already in use.' });
          return;
        }

        if (code === 'WEAK_PASSWORD') {
          this.fieldErrors.set({ password: 'Password does not meet requirements.' });
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

        this.apiError.set(err?.message || 'Registration failed. Please try again.');
      }
    });
  }

  private syncCartAndRedirect() {
    this.cartService.getCart().subscribe({
      next: () => this.finishRegister(),
      error: () => this.finishRegister()
    });
  }

  private finishRegister() {
    if (this.authService.isAuthenticated() && localStorage.getItem('guestSessionId')) {
      this.authService.clearGuestSession();
    }

    this.loading.set(false);
    this.router.navigateByUrl('/');
  }
}
