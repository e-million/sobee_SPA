import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MainLayout } from '../../shared/layout/main-layout';
import { ProductCard } from '../../shared/components/product-card/product-card';
import { ProductService } from '../../core/services/product.service';
import { CartService } from '../../core/services/cart.service';
import { ToastService } from '../../core/services/toast.service';
import { FavoritesService } from '../../core/services/favorites.service';
import { ReviewService } from '../../core/services/review.service';
import { AuthService } from '../../core/services/auth.service';
import { Product, Review } from '../../core/models';

type ProductTab = 'description' | 'ingredients' | 'reviews';
type ReviewSummary = {
  total: number;
  average: number;
  counts: number[];
};

@Component({
  selector: 'app-product-detail',
  imports: [CommonModule, FormsModule, RouterModule, MainLayout, ProductCard],
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
  reviews = signal<Review[]>([]);
  reviewSummary = signal<ReviewSummary>({ total: 0, average: 0, counts: [0, 0, 0, 0, 0] });
  reviewsLoading = signal(false);
  reviewsError = signal('');
  reviewRating = 5;
  reviewText = '';
  reviewSubmitting = signal(false);
  isFavorite = signal(false);
  favoriteLoading = signal(false);
  replyOpenIds = signal<Set<number>>(new Set());
  replyDrafts = signal<Record<number, string>>({});
  replySubmitting = signal(false);

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private productService: ProductService,
    private cartService: CartService,
    private toastService: ToastService,
    private favoritesService: FavoritesService,
    private reviewService: ReviewService,
    private authService: AuthService
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
        this.reviews.set([]);
        this.reviewSummary.set({ total: 0, average: 0, counts: [0, 0, 0, 0, 0] });
        this.replyOpenIds.set(new Set());
        this.replyDrafts.set({});
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
        this.reviews.set([]);
        this.reviewSummary.set({ total: 0, average: 0, counts: [0, 0, 0, 0, 0] });
        this.reviewsError.set('');
        this.reviewText = '';
        this.reviewRating = 5;
        this.isFavorite.set(false);
        this.replyOpenIds.set(new Set());
        this.replyDrafts.set({});
        this.loadRelatedProducts(product);
        this.loadReviews(product.id);
        this.loadFavoriteState(product.id);
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

  toggleFavorite() {
    const product = this.product();
    if (!product) {
      return;
    }

    if (!this.authService.isAuthenticated()) {
      localStorage.setItem('returnUrl', this.router.url);
      this.router.navigate(['/login']);
      return;
    }

    this.favoriteLoading.set(true);

    const request = this.isFavorite()
      ? this.favoritesService.removeFavorite(product.id)
      : this.favoritesService.addFavorite(product.id);

    request.subscribe({
      next: () => {
        const newState = !this.isFavorite();
        this.isFavorite.set(newState);
        this.favoriteLoading.set(false);
        this.toastService.success(newState ? 'Added to wishlist.' : 'Removed from wishlist.');
      },
      error: () => {
        this.favoriteLoading.set(false);
        this.toastService.error('Unable to update wishlist.');
      }
    });
  }

  submitReview() {
    const product = this.product();
    if (!product) {
      return;
    }

    if (!this.authService.isAuthenticated()) {
      localStorage.setItem('returnUrl', this.router.url);
      this.router.navigate(['/login']);
      return;
    }

    const text = this.reviewText.trim();
    if (!text) {
      this.reviewsError.set('Please add a review message.');
      return;
    }

    if (this.reviewRating < 1 || this.reviewRating > 5) {
      this.reviewsError.set('Select a rating between 1 and 5.');
      return;
    }

    this.reviewSubmitting.set(true);
    this.reviewsError.set('');

    this.reviewService.createReview(product.id, {
      rating: this.reviewRating,
      reviewText: text
    }).subscribe({
      next: () => {
        this.reviewText = '';
        this.reviewRating = 5;
        this.reviewSubmitting.set(false);
        this.loadReviews(product.id);
        this.toastService.success('Review submitted.');
      },
      error: () => {
        this.reviewSubmitting.set(false);
        this.reviewsError.set('Unable to submit review. Make sure you are signed in.');
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

  getStarOptions(): number[] {
    return [1, 2, 3, 4, 5];
  }

  canSubmitReview(): boolean {
    return this.authService.isAuthenticated();
  }

  canReply(review: Review): boolean {
    if (!this.authService.isAuthenticated()) {
      return false;
    }

    if (this.authService.isAdmin()) {
      return true;
    }

    const userId = this.authService.getUserId();
    return !!userId && review.userId === userId;
  }

  isReviewOwner(review: Review): boolean {
    const userId = this.authService.getUserId();
    return !!userId && review.userId === userId;
  }

  toggleReply(reviewId: number) {
    const updated = new Set(this.replyOpenIds());
    if (updated.has(reviewId)) {
      updated.delete(reviewId);
    } else {
      updated.add(reviewId);
    }
    this.replyOpenIds.set(updated);
  }

  getReplyDraft(reviewId: number): string {
    return this.replyDrafts()[reviewId] ?? '';
  }

  setReplyDraft(reviewId: number, value: string) {
    this.replyDrafts.update(drafts => ({ ...drafts, [reviewId]: value }));
  }

  submitReply(review: Review) {
    if (!this.canReply(review)) {
      this.reviewsError.set('You are not allowed to reply to this review.');
      return;
    }

    const content = this.getReplyDraft(review.reviewId).trim();
    if (!content) {
      this.reviewsError.set('Please enter a reply.');
      return;
    }

    this.replySubmitting.set(true);
    this.reviewsError.set('');

    this.reviewService.createReply(review.reviewId, { content }).subscribe({
      next: () => {
        this.replySubmitting.set(false);
        this.setReplyDraft(review.reviewId, '');
        this.toggleReply(review.reviewId);
        this.loadReviews(review.productId);
        this.toastService.success('Reply posted.');
      },
      error: () => {
        this.replySubmitting.set(false);
        this.reviewsError.set('Unable to post reply.');
      }
    });
  }

  private loadReviews(productId: number) {
    this.reviewsLoading.set(true);
    this.reviewsError.set('');

    this.reviewService.getReviews(productId).subscribe({
      next: (response) => {
        this.reviews.set(response.reviews);
        this.reviewSummary.set(this.buildReviewSummary(response.reviews));
        this.reviewsLoading.set(false);
      },
      error: () => {
        this.reviewsError.set('Unable to load reviews.');
        this.reviewSummary.set({ total: 0, average: 0, counts: [0, 0, 0, 0, 0] });
        this.reviewsLoading.set(false);
      }
    });
  }

  private loadFavoriteState(productId: number) {
    if (!this.authService.isAuthenticated()) {
      this.isFavorite.set(false);
      return;
    }

    this.favoritesService.getFavorites().subscribe({
      next: (response) => {
        const hasFavorite = response.favorites.some(fav => fav.productId === productId);
        this.isFavorite.set(hasFavorite);
      },
      error: () => {
        this.isFavorite.set(false);
      }
    });
  }

  private buildReviewSummary(reviews: Review[]): ReviewSummary {
    const counts = [0, 0, 0, 0, 0];
    let totalRating = 0;

    for (const review of reviews) {
      if (review.rating >= 1 && review.rating <= 5) {
        counts[review.rating - 1] += 1;
        totalRating += review.rating;
      }
    }

    const total = counts.reduce((sum, count) => sum + count, 0);
    const average = total === 0 ? 0 : totalRating / total;

    return { total, average, counts };
  }

  getReviewRatingRows(): { label: string; count: number; stars: number }[] {
    const summary = this.reviewSummary();
    return [5, 4, 3, 2, 1].map(stars => ({
      stars,
      label: `${stars} star${stars === 1 ? '' : 's'}`,
      count: summary.counts[stars - 1] ?? 0
    }));
  }

  getReviewRatingWidth(count: number): number {
    const total = this.reviewSummary().total;
    if (total === 0) {
      return 0;
    }
    return Math.round((count / total) * 100);
  }
}
