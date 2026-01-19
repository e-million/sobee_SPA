import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Product } from '../../../core/models';
import { StarRatingPipe } from '../../pipes/star-rating.pipe';

/**
 * Product card component with quantity controls and add-to-cart emit.
 */
@Component({
  selector: 'app-product-card',
  imports: [CommonModule, RouterModule, StarRatingPipe],
  templateUrl: './product-card.html',
  styleUrl: './product-card.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProductCard {
  @Input({ required: true }) product!: Product;
  @Output() addToCart = new EventEmitter<{ product: Product; quantity: number }>();

  quantity = signal(1);
  isAdding = signal(false);

  /**
   * Maximum quantity based on stock and per-item cap.
   * @returns Max quantity allowed for the product.
   */
  get maxQuantity(): number {
    if (!this.product.inStock) {
      return 0;
    }

    const stockLimit = this.product.stockAmount ?? 10;
    return Math.min(10, stockLimit);
  }

  /**
   * Decrease quantity by one, down to minimum of 1.
   */
  decrementQuantity() {
    if (this.quantity() > 1) {
      this.quantity.update(q => q - 1);
    }
  }

  /**
   * Increase quantity by one, up to the max quantity.
   */
  incrementQuantity() {
    if (this.quantity() < this.maxQuantity) {
      this.quantity.update(q => q + 1);
    }
  }

  /**
   * Emit add-to-cart event and reset UI state after animation.
   */
  onAddToCart() {
    if (!this.product.inStock || this.maxQuantity === 0) {
      return;
    }

    this.isAdding.set(true);
    this.addToCart.emit({ product: this.product, quantity: this.quantity() });

    // Reset after animation
    setTimeout(() => {
      this.isAdding.set(false);
      this.quantity.set(1);
    }, 500);
  }

  /**
   * Resolve the best image URL or fallback placeholder.
   * @returns Image URL string.
   */
  get productImage(): string {
    // Return a placeholder or actual image URL
    return this.product.primaryImageUrl || this.product.imageUrl || 'https://placehold.co/400x400/f59e0b/white?text=SoBee';
  }

  /**
   * Whether the product stock is low.
   * @returns True if stock is below the low-stock threshold.
   */
  get isLowStock(): boolean {
    return !!this.product.stockAmount && this.product.stockAmount > 0 && this.product.stockAmount < 5;
  }

  /**
   * Whether the product is out of stock.
   * @returns True if the item is unavailable.
   */
  get isOutOfStock(): boolean {
    return !this.product.inStock || this.product.stockAmount === 0;
  }
}
