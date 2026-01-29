import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { CartService } from '../../../core/services/cart.service';

/**
 * Navbar component with auth links, cart badge, and search dropdown.
 */
@Component({
  selector: 'app-navbar',
  imports: [CommonModule, RouterModule],
  templateUrl: './navbar.html',
  styleUrl: './navbar.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Navbar {
  mobileMenuOpen = signal(false);

  /**
   * Initialize services and wire up search input stream.
   * @param authService - AuthService for login/logout state.
   * @param cartService - CartService for cart badge.
   * @param productService - ProductService for search suggestions.
   * @param router - Router for search navigation.
   */
  constructor(
    public authService: AuthService,
    public cartService: CartService
  ) {}

  /**
   * Toggle mobile menu visibility.
   */
  toggleMobileMenu() {
    this.mobileMenuOpen.update(v => !v);
  }

  /**
   * Close the mobile menu.
   */
  closeMobileMenu() {
    this.mobileMenuOpen.set(false);
  }

  /**
   * Log out and close the mobile menu.
   */
  logout() {
    this.authService.logout();
    this.closeMobileMenu();
  }

  /**
   * Total count of items in the cart.
   */
  get cartItemCount(): number {
    const cart = this.cartService.cart();
    if (!cart || !cart.items) return 0;
    return cart.items.reduce((total, item) => total + (item.quantity || 0), 0);
  }
}
