import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminCategoryService } from '../../../core/services/admin-category.service';
import { ToastService } from '../../../core/services/toast.service';
import { AdminCategory } from '../../../core/models';

interface CategoryFormModel {
  name: string;
  description: string;
}

@Component({
  selector: 'app-admin-categories',
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-categories.html',
  styleUrl: './admin-categories.css'
})
export class AdminCategories implements OnInit {
  categories = signal<AdminCategory[]>([]);
  loading = signal(true);
  error = signal('');

  searchTerm = '';

  formOpen = signal(false);
  formSaving = signal(false);
  formError = signal('');
  editingCategoryId = signal<number | null>(null);
  deleteModalOpen = signal(false);
  deletingCategory = signal<AdminCategory | null>(null);

  formModel: CategoryFormModel = {
    name: '',
    description: ''
  };

  constructor(
    private adminCategoryService: AdminCategoryService,
    private toastService: ToastService
  ) {}

  ngOnInit() {
    this.loadCategories();
  }

  loadCategories() {
    this.loading.set(true);
    this.error.set('');

    this.adminCategoryService.getCategories().subscribe({
      next: (categories) => {
        this.categories.set(categories);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load categories.');
        this.loading.set(false);
      }
    });
  }

  onSearch() {
    // Client-side filtering only; no API call needed.
  }

  openCreateForm() {
    this.editingCategoryId.set(null);
    this.formModel = {
      name: '',
      description: ''
    };
    this.formError.set('');
    this.formOpen.set(true);
  }

  openEditForm(category: AdminCategory) {
    this.editingCategoryId.set(category.id);
    this.formModel = {
      name: category.name ?? '',
      description: category.description ?? ''
    };
    this.formError.set('');
    this.formOpen.set(true);
  }

  closeForm() {
    this.formOpen.set(false);
  }

  saveCategory() {
    const name = this.formModel.name.trim();
    if (!name) {
      this.formError.set('Category name is required.');
      return;
    }

    this.formSaving.set(true);
    this.formError.set('');

    const payload = {
      name,
      description: this.formModel.description.trim()
    };

    const editingId = this.editingCategoryId();
    const request = editingId
      ? this.adminCategoryService.updateCategory(editingId, payload)
      : this.adminCategoryService.createCategory(payload);

    request.subscribe({
      next: () => {
        this.formSaving.set(false);
        this.formOpen.set(false);
        this.toastService.success(editingId ? 'Category updated.' : 'Category created.');
        this.loadCategories();
      },
      error: () => {
        this.formSaving.set(false);
        this.toastService.error('Unable to save category.');
      }
    });
  }

  deleteCategory(category: AdminCategory) {
    this.deletingCategory.set(category);
    this.deleteModalOpen.set(true);
  }

  closeDeleteModal() {
    this.deleteModalOpen.set(false);
    this.deletingCategory.set(null);
  }

  confirmDeleteCategory() {
    const category = this.deletingCategory();
    if (!category) {
      return;
    }

    this.adminCategoryService.forceDeleteCategory(category.id).subscribe({
      next: (response) => {
        this.toastService.success(response.message ?? 'Category deleted.');
        this.loadCategories();
        this.closeDeleteModal();
      },
      error: () => {
        this.toastService.error('Failed to delete category.');
      }
    });
  }

  get filteredCategories(): AdminCategory[] {
    const term = this.searchTerm.trim().toLowerCase();
    const categories = this.categories();
    if (!term) {
      return categories;
    }

    return categories.filter(category =>
      category.name.toLowerCase().includes(term) ||
      (category.description ?? '').toLowerCase().includes(term)
    );
  }
}
