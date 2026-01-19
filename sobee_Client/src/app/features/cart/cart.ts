import { ChangeDetectionStrategy, Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MainLayout } from '../../shared/layout/main-layout';
import { CartService } from '../../core/services/cart.service';
import { ToastService } from '../../core/services/toast.service';
import { CartItem } from '../../core/models';

@Component({
  selector: 'app-cart',
  imports: [CommonModule, FormsModule, RouterModule, MainLayout],
  templateUrl: './cart.html',
  styleUrl: './cart.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Cart implements OnInit {
  loading = signal(true);
  error = signal('');
  promoApplying = signal(false);
  busyItemIds = signal<Set<number>>(new Set());
  promoCode = '';

  constructor(
    public cartService: CartService,
    private toastService: ToastService
  ) {}

  ngOnInit() {
    this.loadCart();
  }

  loadCart() {
    this.loading.set(true);
    this.error.set('');

    this.cartService.getCart().subscribe({
      next: (cart) => {
        this.loading.set(false);
        if (cart.promo?.code) {
          this.promoCode = cart.promo.code;
        }
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Failed to load your cart. Please try again.');
      }
    });
  }

  updateQuantity(item: CartItem, nextQuantity: number) {
    if (!item.cartItemId) {
      return;
    }

    const safeQuantity = Math.max(1, Math.min(10, nextQuantity));
    this.setItemBusy(item.cartItemId, true);

    this.cartService.updateItem(item.cartItemId, { quantity: safeQuantity }).subscribe({
      next: () => {
        this.setItemBusy(item.cartItemId, false);
      },
      error: () => {
        this.setItemBusy(item.cartItemId, false);
        this.toastService.error('Failed to update quantity');
      }
    });
  }

  removeItem(item: CartItem) {
    if (!item.cartItemId) {
      return;
    }

    this.setItemBusy(item.cartItemId, true);

    this.cartService.removeItem(item.cartItemId).subscribe({
      next: () => {
        this.setItemBusy(item.cartItemId, false);
        this.toastService.success('Item removed from cart');
      },
      error: () => {
        this.setItemBusy(item.cartItemId, false);
        this.toastService.error('Failed to remove item');
      }
    });
  }

  applyPromo() {
    const code = this.promoCode.trim();
    if (!code) {
      this.toastService.error('Please enter a promo code');
      return;
    }

    this.promoApplying.set(true);
    this.cartService.applyPromo({ promoCode: code }).subscribe({
      next: () => {
        this.promoApplying.set(false);
        this.toastService.success('Promo code applied');
      },
      error: () => {
        this.promoApplying.set(false);
        this.toastService.error('Failed to apply promo code');
      }
    });
  }

  removePromo() {
    this.promoApplying.set(true);
    this.cartService.removePromo().subscribe({
      next: () => {
        this.promoApplying.set(false);
        this.promoCode = '';
        this.toastService.success('Promo code removed');
      },
      error: () => {
        this.promoApplying.set(false);
        this.toastService.error('Failed to remove promo code');
      }
    });
  }

  isItemBusy(cartItemId: number): boolean {
    return this.busyItemIds().has(cartItemId);
  }

  private setItemBusy(cartItemId: number, isBusy: boolean) {
    const updated = new Set(this.busyItemIds());
    if (isBusy) {
      updated.add(cartItemId);
    } else {
      updated.delete(cartItemId);
    }
    this.busyItemIds.set(updated);
  }
}
