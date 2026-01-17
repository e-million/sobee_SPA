import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { MainLayout } from '../../shared/layout/main-layout';
import { ProductCard } from '../../shared/components/product-card/product-card';
import { ProductService } from '../../core/services/product.service';
import { CartService } from '../../core/services/cart.service';
import { ToastService } from '../../core/services/toast.service';
import { Product } from '../../core/models';

type ProductTab = 'description' | 'ingredients';

@Component({
  selector: 'app-product-detail',
  imports: [CommonModule, RouterModule, MainLayout, ProductCard],
  templateUrl: './product-detail.html',
  styleUrl: './product-detail.css'
})
export class ProductDetail implements OnInit {
  product = signal<Product | null>(null);
  relatedProducts = signal<Product[]>([]);
  loading = signal(true);
  notFound = signal(false);
  error = signal('');
  quantity = signal(1);
  activeTab = signal<ProductTab>('description');

  constructor(
    private route: ActivatedRoute,
    private productService: ProductService,
    private cartService: CartService,
    private toastService: ToastService
  ) {}

  ngOnInit() {
    this.route.paramMap.subscribe(params => {
      const idParam = params.get('id');
      const productId = Number(idParam);

      if (!idParam || Number.isNaN(productId)) {
        this.notFound.set(true);
        this.loading.set(false);
        this.product.set(null);
        this.relatedProducts.set([]);
        this.error.set('');
        return;
      }

      this.loadProduct(productId);
    });
  }

  loadProduct(productId: number) {
    this.loading.set(true);
    this.notFound.set(false);
    this.error.set('');
    this.product.set(null);
    this.relatedProducts.set([]);

    this.productService.getProduct(productId).subscribe({
      next: (product) => {
        this.product.set(product);
        this.loading.set(false);
        this.quantity.set(1);
        this.activeTab.set('description');
        this.loadRelatedProducts(product);
      },
      error: (err) => {
        this.loading.set(false);
        if (err?.status === 404) {
          this.notFound.set(true);
        } else {
          this.error.set('Failed to load product details. Please try again.');
        }
      }
    });
  }

  loadRelatedProducts(product: Product) {
    const category = product.category ?? undefined;
    const params = category ? { category } : undefined;

    this.productService.getProducts(params).subscribe({
      next: (products) => {
        const related = products.filter(item => item.id !== product.id).slice(0, 4);
        this.relatedProducts.set(related);
      },
      error: () => {
        this.relatedProducts.set([]);
      }
    });
  }

  updateQuantity(nextQuantity: number) {
    const max = this.maxQuantity;
    if (max === 0) {
      return;
    }

    const safeQuantity = Math.max(1, Math.min(max, nextQuantity));
    this.quantity.set(safeQuantity);
  }

  addToCart() {
    const product = this.product();
    if (!product || this.isOutOfStock) {
      return;
    }

    this.cartService.addItem({ productId: product.id, quantity: this.quantity() }).subscribe({
      next: () => {
        this.toastService.success(`Added ${this.quantity()} ${product.name ?? 'item'} to cart!`);
      },
      error: () => {
        this.toastService.error('Failed to add item to cart');
      }
    });
  }

  addRelatedToCart(event: { product: Product; quantity: number }) {
    this.cartService.addItem({ productId: event.product.id, quantity: event.quantity }).subscribe({
      next: () => {
        this.toastService.success(`Added ${event.quantity} ${event.product.name ?? 'item'} to cart!`);
      },
      error: () => {
        this.toastService.error('Failed to add item to cart');
      }
    });
  }

  setTab(tab: ProductTab) {
    this.activeTab.set(tab);
  }

  get maxQuantity(): number {
    const product = this.product();
    if (!product || !product.inStock) {
      return 0;
    }

    const stockLimit = product.stockAmount ?? 10;
    return Math.min(10, stockLimit);
  }

  get isOutOfStock(): boolean {
    const product = this.product();
    return !product || !product.inStock || product.stockAmount === 0;
  }

  get isLowStock(): boolean {
    const product = this.product();
    return !!product?.stockAmount && product.stockAmount > 0 && product.stockAmount < 5;
  }

  get productImage(): string {
    const product = this.product();
    if (!product) {
      return 'https://placehold.co/600x600/f59e0b/white?text=SoBee';
    }

    return product.primaryImageUrl || product.imageUrl || 'https://placehold.co/600x600/f59e0b/white?text=SoBee';
  }

  getStars(rating: number | null | undefined): number[] {
    const starRating = rating || 0;
    return Array(5).fill(0).map((_, i) => i < Math.round(starRating) ? 1 : 0);
  }
}
