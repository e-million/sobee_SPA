import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MainLayout } from '../../shared/layout/main-layout';
import { UserService } from '../../core/services/user.service';
import { UserProfile } from '../../core/models';

interface PasswordFormModel {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

@Component({
  selector: 'app-account',
  imports: [CommonModule, FormsModule, RouterModule, MainLayout],
  templateUrl: './account.html',
  styleUrl: './account.css'
})
export class Account implements OnInit {
  loading = signal(true);
  savingProfile = signal(false);
  savingPassword = signal(false);
  profileError = signal('');
  passwordError = signal('');
  profileSuccess = signal('');
  passwordSuccess = signal('');

  profile = signal<UserProfile | null>(null);
  profileForm: UserProfile = {
    email: '',
    firstName: '',
    lastName: '',
    billingAddress: '',
    shippingAddress: ''
  };

  passwordForm: PasswordFormModel = {
    currentPassword: '',
    newPassword: '',
    confirmPassword: ''
  };

  constructor(private userService: UserService) {}

  ngOnInit() {
    this.loadProfile();
  }

  loadProfile() {
    this.loading.set(true);
    this.profileError.set('');

    this.userService.getProfile().subscribe({
      next: (profile) => {
        this.profile.set(profile);
        this.profileForm = { ...profile };
        this.loading.set(false);
      },
      error: (err) => {
        this.profileError.set(err?.message || 'Failed to load profile.');
        this.loading.set(false);
      }
    });
  }

  saveProfile(form: NgForm) {
    this.profileSuccess.set('');
    this.profileError.set('');

    if (form.invalid) {
      form.control.markAllAsTouched();
      return;
    }

    this.savingProfile.set(true);

    this.userService.updateProfile(this.profileForm).subscribe({
      next: (profile) => {
        this.profile.set(profile);
        this.profileForm = { ...profile };
        this.savingProfile.set(false);
        this.profileSuccess.set('Profile updated successfully.');
      },
      error: (err) => {
        this.savingProfile.set(false);
        this.profileError.set(err?.message || 'Unable to update profile.');
      }
    });
  }

  savePassword(form: NgForm) {
    this.passwordSuccess.set('');
    this.passwordError.set('');

    if (this.passwordForm.newPassword !== this.passwordForm.confirmPassword) {
      this.passwordError.set('New passwords do not match.');
      return;
    }

    if (form.invalid) {
      form.control.markAllAsTouched();
      return;
    }

    this.savingPassword.set(true);

    this.userService.changePassword({
      currentPassword: this.passwordForm.currentPassword,
      newPassword: this.passwordForm.newPassword
    }).subscribe({
      next: () => {
        this.savingPassword.set(false);
        this.passwordSuccess.set('Password updated successfully.');
        this.passwordForm = { currentPassword: '', newPassword: '', confirmPassword: '' };
        form.resetForm();
      },
      error: (err) => {
        this.savingPassword.set(false);
        this.passwordError.set(err?.message || 'Unable to update password.');
      }
    });
  }
}
