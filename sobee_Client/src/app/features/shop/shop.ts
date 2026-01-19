import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
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
  styleUrl: './shop.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Shop implements OnInit {
  products = signal<Product[]>([]);
  allProducts = signal<Product[]>([]);
  filteredProducts = signal<Product[]>([]);
  categories = signal<string[]>([]);
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
  selectedCategory = 'all';
  minPrice = '';
  maxPrice = '';
  private readonly destroyRef = inject(DestroyRef);

  constructor(
    private productService: ProductService,
    private cartService: CartService,
    private toastService: ToastService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit() {
    this.route.queryParamMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(params => {
        this.selectedCategory = params.get('category') ?? 'all';
        this.minPrice = params.get('minPrice') ?? '';
        this.maxPrice = params.get('maxPrice') ?? '';
        this.selectedSort = params.get('sort') ?? 'newest';
        this.currentPage.set(1);
        this.loadProducts();
      });

    this.loadCategories();
  }

  loadProducts() {
    this.loading.set(true);
    const category = this.selectedCategory && this.selectedCategory !== 'all' ? this.selectedCategory : undefined;
    this.productService.getProducts({ category }).subscribe({
      next: (products) => {
        this.allProducts.set(products);
        this.ensureCategoriesFromProducts(products);
        this.applyFilters();
        this.loading.set(false);
      },
      error: () => {
        this.toastService.error('Failed to load products');
        this.loading.set(false);
      }
    });
  }

  loadCategories() {
    this.productService.getCategories().subscribe({
      next: (categories) => {
        const cleaned = categories.filter(Boolean).sort();
        this.categories.set(cleaned);
      },
      error: () => {
        this.categories.set([]);
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
    this.currentPage.set(1);
    this.updateQueryParams();
  }

  onCategoryChange() {
    this.currentPage.set(1);
    this.updateQueryParams();
  }

  onPriceChange() {
    this.currentPage.set(1);
    this.updateQueryParams();
  }

  clearFilters() {
    this.selectedCategory = 'all';
    this.minPrice = '';
    this.maxPrice = '';
    this.selectedSort = 'newest';
    this.currentPage.set(1);
    this.updateQueryParams();
  }

  applyFilters() {
    let filtered = [...this.allProducts()];
    const min = Number(this.minPrice);
    const max = Number(this.maxPrice);

    if (this.minPrice && !Number.isNaN(min)) {
      filtered = filtered.filter(product => product.price >= min);
    }

    if (this.maxPrice && !Number.isNaN(max)) {
      filtered = filtered.filter(product => product.price <= max);
    }

    filtered = this.sortProducts(filtered);

    this.filteredProducts.set(filtered);
    this.totalProducts.set(filtered.length);

    if (this.currentPage() > this.totalPages) {
      this.currentPage.set(1);
    }

    this.updatePagedProducts();
  }

  updatePagedProducts() {
    const start = (this.currentPage() - 1) * this.pageSize();
    const end = start + this.pageSize();
    this.products.set(this.filteredProducts().slice(start, end));
  }

  get totalPages(): number {
    return Math.ceil(this.totalProducts() / this.pageSize());
  }

  goToPage(page: number) {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage.set(page);
      this.updatePagedProducts();
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  }

  private updateQueryParams() {
    const queryParams: Record<string, string> = {};

    if (this.selectedCategory && this.selectedCategory !== 'all') {
      queryParams['category'] = this.selectedCategory;
    }

    if (this.minPrice) {
      queryParams['minPrice'] = this.minPrice;
    }

    if (this.maxPrice) {
      queryParams['maxPrice'] = this.maxPrice;
    }

    if (this.selectedSort && this.selectedSort !== 'newest') {
      queryParams['sort'] = this.selectedSort;
    }

    this.router.navigate([], {
      relativeTo: this.route,
      queryParams
    });
  }

  private sortProducts(products: Product[]): Product[] {
    const sorted = [...products];

    switch (this.selectedSort) {
      case 'price-low':
        return sorted.sort((a, b) => a.price - b.price);
      case 'price-high':
        return sorted.sort((a, b) => b.price - a.price);
      case 'name':
        return sorted.sort((a, b) => (a.name ?? '').localeCompare(b.name ?? ''));
      case 'newest':
      default:
        return sorted.sort((a, b) => b.id - a.id);
    }
  }

  private ensureCategoriesFromProducts(products: Product[]) {
    if (this.categories().length > 0) {
      return;
    }

    const categories = Array.from(new Set(
      products.map(product => product.category).filter((category): category is string => !!category)
    )).sort();

    if (categories.length > 0) {
      this.categories.set(categories);
    }
  }
}
