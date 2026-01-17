import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminProductService } from '../../../core/services/admin-product.service';
import { ToastService } from '../../../core/services/toast.service';
import { AdminProduct } from '../../../core/models';

interface ProductFormModel {
  name: string;
  description: string;
  price: number;
  stockAmount: number;
}

@Component({
  selector: 'app-admin-products',
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-products.html',
  styleUrl: './admin-products.css'
})
export class AdminProducts implements OnInit {
  products = signal<AdminProduct[]>([]);
  loading = signal(true);
  error = signal('');
  page = signal(1);
  pageSize = signal(10);
  totalCount = signal(0);

  searchTerm = '';
  sortOption = 'default';

  formOpen = signal(false);
  formSaving = signal(false);
  formError = signal('');
  editingProductId = signal<number | null>(null);

  formModel: ProductFormModel = {
    name: '',
    description: '',
    price: 0,
    stockAmount: 0
  };

  constructor(
    private adminProductService: AdminProductService,
    private toastService: ToastService
  ) {}

  ngOnInit() {
    this.loadProducts();
  }

  loadProducts() {
    this.loading.set(true);
    this.error.set('');

    this.adminProductService.getProducts({
      search: this.searchTerm.trim() || undefined,
      page: this.page(),
      pageSize: this.pageSize(),
      sort: this.sortOption === 'default' ? undefined : (this.sortOption as 'priceAsc' | 'priceDesc')
    }).subscribe({
      next: (response) => {
        this.products.set(response.items);
        this.totalCount.set(response.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load products.');
        this.loading.set(false);
      }
    });
  }

  onSearch() {
    this.page.set(1);
    this.loadProducts();
  }

  onSortChange() {
    this.page.set(1);
    this.loadProducts();
  }

  openCreateForm() {
    this.editingProductId.set(null);
    this.formModel = {
      name: '',
      description: '',
      price: 0,
      stockAmount: 0
    };
    this.formError.set('');
    this.formOpen.set(true);
  }

  openEditForm(product: AdminProduct) {
    this.editingProductId.set(product.id);
    this.formModel = {
      name: product.name ?? '',
      description: product.description ?? '',
      price: Number(product.price ?? 0),
      stockAmount: Number(product.stockAmount ?? 0)
    };
    this.formError.set('');
    this.formOpen.set(true);
  }

  closeForm() {
    this.formOpen.set(false);
  }

  saveProduct() {
    const name = this.formModel.name.trim();
    if (!name) {
      this.formError.set('Product name is required.');
      return;
    }

    if (this.formModel.price < 0) {
      this.formError.set('Price must be zero or greater.');
      return;
    }

    if (this.formModel.stockAmount < 0) {
      this.formError.set('Stock must be zero or greater.');
      return;
    }

    this.formSaving.set(true);
    this.formError.set('');

    const payload = {
      name,
      description: this.formModel.description.trim(),
      price: this.formModel.price,
      stockAmount: this.formModel.stockAmount
    };

    const editingId = this.editingProductId();
    const request = editingId
      ? this.adminProductService.updateProduct(editingId, payload)
      : this.adminProductService.createProduct(payload);

    request.subscribe({
      next: () => {
        this.toastService.success(editingId ? 'Product updated.' : 'Product created.');
        this.formSaving.set(false);
        this.formOpen.set(false);
        this.loadProducts();
      },
      error: () => {
        this.toastService.error('Unable to save product.');
        this.formSaving.set(false);
      }
    });
  }

  deleteProduct(product: AdminProduct) {
    if (!confirm(`Delete ${product.name}?`)) {
      return;
    }

    this.adminProductService.deleteProduct(product.id).subscribe({
      next: () => {
        this.toastService.success('Product deleted.');
        this.loadProducts();
      },
      error: () => {
        this.toastService.error('Failed to delete product.');
      }
    });
  }

  goToPage(page: number) {
    if (page < 1 || page > this.totalPages) {
      return;
    }
    this.page.set(page);
    this.loadProducts();
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount() / this.pageSize()));
  }
}
