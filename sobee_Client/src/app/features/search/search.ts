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
  selector: 'app-search',
  imports: [CommonModule, FormsModule, RouterModule, MainLayout, ProductCard],
  templateUrl: './search.html',
  styleUrl: './search.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Search implements OnInit {
  products = signal<Product[]>([]);
  loading = signal(false);
  searchTerm = '';
  private readonly destroyRef = inject(DestroyRef);

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private productService: ProductService,
    private cartService: CartService,
    private toastService: ToastService
  ) {}

  ngOnInit() {
    this.route.queryParamMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(params => {
        const term = params.get('q') ?? '';
        this.searchTerm = term;
        this.loadResults(term);
      });
  }

  loadResults(term: string) {
    const trimmed = term.trim();
    if (!trimmed) {
      this.products.set([]);
      this.loading.set(false);
      return;
    }

    this.loading.set(true);
    this.productService.searchProducts(trimmed).subscribe({
      next: (products) => {
        this.products.set(products);
        this.loading.set(false);
      },
      error: () => {
        this.products.set([]);
        this.loading.set(false);
        this.toastService.error('Failed to load search results');
      }
    });
  }

  onSearchSubmit() {
    const trimmed = this.searchTerm.trim();
    if (!trimmed) {
      return;
    }

    this.router.navigate(['/search'], { queryParams: { q: trimmed } });
  }

  onAddToCart(event: { product: Product; quantity: number }) {
    this.cartService.addItem({ productId: event.product.id, quantity: event.quantity }).subscribe({
      next: () => {
        this.toastService.success(`Added ${event.quantity} ${event.product.name ?? 'item'} to cart!`);
      },
      error: () => {
        this.toastService.error('Failed to add item to cart');
      }
    });
  }
}
