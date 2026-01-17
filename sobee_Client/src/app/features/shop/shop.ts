import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MainLayout } from '../../shared/layout/main-layout';
import { ProductCard } from '../../shared/components/product-card/product-card';
import { ProductService } from '../../core/services/product.service';
import { CartService } from '../../core/services/cart.service';
import { ToastService } from '../../core/services/toast.service';
import { Product } from '../../core/models';

@Component({
  selector: 'app-shop',
  imports: [CommonModule, RouterModule, FormsModule, MainLayout, ProductCard],
  templateUrl: './shop.html',
  styleUrl: './shop.css'
})
export class Shop implements OnInit {
  products = signal<Product[]>([]);
  loading = signal(true);
  currentPage = signal(1);
  pageSize = signal(12);
  totalProducts = signal(0);

  sortOptions = [
    { value: 'newest', label: 'Newest' },
    { value: 'price-low', label: 'Price: Low to High' },
    { value: 'price-high', label: 'Price: High to Low' },
    { value: 'name', label: 'Name A-Z' },
  ];

  selectedSort = 'newest';

  constructor(
    private productService: ProductService,
    private cartService: CartService,
    private toastService: ToastService
  ) {}

  ngOnInit() {
    this.loadProducts();
  }

  loadProducts() {
    this.loading.set(true);
    this.productService.getProducts().subscribe({
      next: (products) => {
        this.totalProducts.set(products.length);
        // Client-side pagination for now
        const start = (this.currentPage() - 1) * this.pageSize();
        const end = start + this.pageSize();
        this.products.set(products.slice(start, end));
        this.loading.set(false);
      },
      error: () => {
        this.toastService.error('Failed to load products');
        this.loading.set(false);
      }
    });
  }

  onAddToCart(event: { product: Product; quantity: number }) {
    this.cartService.addItem({ productId: event.product.id, quantity: event.quantity }).subscribe({
      next: () => {
        this.toastService.success(`Added ${event.quantity} ${event.product.name} to cart!`);
      },
      error: () => {
        this.toastService.error('Failed to add item to cart');
      }
    });
  }

  onSortChange() {
    // In a real app, this would re-fetch with sort parameter
    this.loadProducts();
  }

  get totalPages(): number {
    return Math.ceil(this.totalProducts() / this.pageSize());
  }

  goToPage(page: number) {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage.set(page);
      this.loadProducts();
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  }
}
