import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminPromoService } from '../../../core/services/admin-promo.service';
import { ToastService } from '../../../core/services/toast.service';
import { AdminPromo } from '../../../core/models';

interface PromoFormModel {
  code: string;
  discountPercentage: number;
  expirationDate: string;
}

@Component({
  selector: 'app-admin-promos',
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-promos.html',
  styleUrl: './admin-promos.css'
})
export class AdminPromos implements OnInit {
  promos = signal<AdminPromo[]>([]);
  loading = signal(true);
  error = signal('');
  page = signal(1);
  pageSize = signal(10);
  totalCount = signal(0);

  searchTerm = '';
  includeExpired = false;

  formOpen = signal(false);
  formSaving = signal(false);
  formError = signal('');
  editingPromoId = signal<number | null>(null);

  formModel: PromoFormModel = {
    code: '',
    discountPercentage: 10,
    expirationDate: ''
  };

  constructor(
    private adminPromoService: AdminPromoService,
    private toastService: ToastService
  ) {}

  ngOnInit() {
    this.loadPromos();
  }

  loadPromos() {
    this.loading.set(true);
    this.error.set('');

    this.adminPromoService.getPromos({
      search: this.searchTerm.trim() || undefined,
      includeExpired: this.includeExpired,
      page: this.page(),
      pageSize: this.pageSize()
    }).subscribe({
      next: (response) => {
        this.promos.set(response.items);
        this.totalCount.set(response.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load promotions.');
        this.loading.set(false);
      }
    });
  }

  onSearch() {
    this.page.set(1);
    this.loadPromos();
  }

  toggleIncludeExpired() {
    this.page.set(1);
    this.loadPromos();
  }

  openCreateForm() {
    this.editingPromoId.set(null);
    this.formModel = {
      code: '',
      discountPercentage: 10,
      expirationDate: ''
    };
    this.formError.set('');
    this.formOpen.set(true);
  }

  openEditForm(promo: AdminPromo) {
    this.editingPromoId.set(promo.id);
    this.formModel = {
      code: promo.code,
      discountPercentage: promo.discountPercentage,
      expirationDate: this.formatDateInput(promo.expirationDate)
    };
    this.formError.set('');
    this.formOpen.set(true);
  }

  closeForm() {
    this.formOpen.set(false);
  }

  savePromo() {
    const code = this.formModel.code.trim().toUpperCase();
    if (!code) {
      this.formError.set('Promo code is required.');
      return;
    }

    if (this.formModel.discountPercentage <= 0 || this.formModel.discountPercentage > 100) {
      this.formError.set('Discount percentage must be between 0 and 100.');
      return;
    }

    if (!this.formModel.expirationDate) {
      this.formError.set('Expiration date is required.');
      return;
    }

    const expirationDate = new Date(this.formModel.expirationDate);
    if (Number.isNaN(expirationDate.getTime())) {
      this.formError.set('Expiration date is invalid.');
      return;
    }

    this.formSaving.set(true);
    this.formError.set('');

    const payload = {
      code,
      discountPercentage: this.formModel.discountPercentage,
      expirationDate: expirationDate.toISOString()
    };

    const editingId = this.editingPromoId();
    const request = editingId
      ? this.adminPromoService.updatePromo(editingId, payload)
      : this.adminPromoService.createPromo(payload);

    request.subscribe({
      next: () => {
        this.formSaving.set(false);
        this.formOpen.set(false);
        this.toastService.success(editingId ? 'Promo updated.' : 'Promo created.');
        this.loadPromos();
      },
      error: () => {
        this.formSaving.set(false);
        this.toastService.error('Unable to save promo.');
      }
    });
  }

  deletePromo(promo: AdminPromo) {
    if (!confirm(`Delete promo ${promo.code}?`)) {
      return;
    }

    this.adminPromoService.deletePromo(promo.id).subscribe({
      next: () => {
        this.toastService.success('Promo deleted.');
        this.loadPromos();
      },
      error: () => {
        this.toastService.error('Failed to delete promo.');
      }
    });
  }

  goToPage(page: number) {
    if (page < 1 || page > this.totalPages) {
      return;
    }
    this.page.set(page);
    this.loadPromos();
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount() / this.pageSize()));
  }

  formatDate(value: string): string {
    return new Date(value).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }

  private formatDateInput(value: string): string {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return '';
    }
    return date.toISOString().slice(0, 10);
  }
}
