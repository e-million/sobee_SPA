import { Component, Input, Output, EventEmitter, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Product } from '../../../core/models';

@Component({
  selector: 'app-product-card',
  imports: [CommonModule, RouterModule],
  templateUrl: './product-card.html',
  styleUrl: './product-card.css'
})
export class ProductCard {
  @Input({ required: true }) product!: Product;
  @Output() addToCart = new EventEmitter<{ product: Product; quantity: number }>();

  quantity = signal(1);
  isAdding = signal(false);

  decrementQuantity() {
    if (this.quantity() > 1) {
      this.quantity.update(q => q - 1);
    }
  }

  incrementQuantity() {
    if (this.quantity() < 10) {
      this.quantity.update(q => q + 1);
    }
  }

  onAddToCart() {
    this.isAdding.set(true);
    this.addToCart.emit({ product: this.product, quantity: this.quantity() });

    // Reset after animation
    setTimeout(() => {
      this.isAdding.set(false);
      this.quantity.set(1);
    }, 500);
  }

  getStars(rating: number | null): number[] {
    const starRating = rating || 0;
    return Array(5).fill(0).map((_, i) => i < Math.round(starRating) ? 1 : 0);
  }

  get productImage(): string {
    // Return a placeholder or actual image URL
    return this.product.imageUrl || 'https://placehold.co/400x400/f59e0b/white?text=SoBee';
  }
}
