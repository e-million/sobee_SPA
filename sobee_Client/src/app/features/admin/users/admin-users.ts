import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminUserService } from '../../../core/services/admin-user.service';
import { ToastService } from '../../../core/services/toast.service';
import { AdminUser } from '../../../core/models';

@Component({
  selector: 'app-admin-users',
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-users.html',
  styleUrl: './admin-users.css'
})
export class AdminUsers implements OnInit {
  users = signal<AdminUser[]>([]);
  loading = signal(true);
  error = signal('');
  page = signal(1);
  pageSize = signal(10);
  totalCount = signal(0);
  updatingUserId = signal<string | null>(null);

  searchTerm = '';

  constructor(
    private adminUserService: AdminUserService,
    private toastService: ToastService
  ) {}

  ngOnInit() {
    this.loadUsers();
  }

  loadUsers() {
    this.loading.set(true);
    this.error.set('');

    this.adminUserService.getUsers({
      search: this.searchTerm.trim() || undefined,
      page: this.page(),
      pageSize: this.pageSize()
    }).subscribe({
      next: (response) => {
        this.users.set(response.items);
        this.totalCount.set(response.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load users.');
        this.loading.set(false);
      }
    });
  }

  onSearch() {
    this.page.set(1);
    this.loadUsers();
  }

  toggleAdmin(user: AdminUser) {
    if (user.isCurrentUser) {
      this.toastService.error('You cannot remove your own admin access.');
      return;
    }

    this.updatingUserId.set(user.id);

    this.adminUserService.setAdmin(user.id, !user.isAdmin).subscribe({
      next: (updated) => {
        this.users.update(users =>
          users.map(item => item.id === updated.id ? updated : item)
        );
        this.updatingUserId.set(null);
        this.toastService.success(updated.isAdmin ? 'Admin access granted.' : 'Admin access removed.');
      },
      error: () => {
        this.updatingUserId.set(null);
        this.toastService.error('Unable to update user roles.');
      }
    });
  }

  goToPage(page: number) {
    if (page < 1 || page > this.totalPages) {
      return;
    }
    this.page.set(page);
    this.loadUsers();
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount() / this.pageSize()));
  }

  formatDate(value: string | null): string {
    if (!value) {
      return '—';
    }
    return new Date(value).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }
}
