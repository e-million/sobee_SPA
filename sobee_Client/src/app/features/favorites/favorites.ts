import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { MainLayout } from '../../shared/layout/main-layout';
import { FavoritesService } from '../../core/services/favorites.service';
import { ProductService } from '../../core/services/product.service';
import { CartService } from '../../core/services/cart.service';
import { ToastService } from '../../core/services/toast.service';
import { FavoriteItem, Product } from '../../core/models';

interface FavoriteViewItem extends FavoriteItem {
  product: Product | null;
}

@Component({
  selector: 'app-favorites',
  imports: [CommonModule, RouterModule, MainLayout],
  templateUrl: './favorites.html',
  styleUrl: './favorites.css'
})
export class Favorites implements OnInit {
  favorites = signal<FavoriteViewItem[]>([]);
  loading = signal(true);
  error = signal('');

  constructor(
    private favoritesService: FavoritesService,
    private productService: ProductService,
    private cartService: CartService,
    private toastService: ToastService
  ) {}

  ngOnInit() {
    this.loadFavorites();
  }

  loadFavorites() {
    this.loading.set(true);
    this.error.set('');

    this.favoritesService.getFavorites().subscribe({
      next: (response) => {
        if (response.favorites.length === 0) {
          this.favorites.set([]);
          this.loading.set(false);
          return;
        }

        const requests = response.favorites.map(favorite =>
          this.productService.getProduct(favorite.productId).pipe(
            map(product => ({ ...favorite, product })),
            catchError(() => of({ ...favorite, product: null }))
          )
        );

        forkJoin(requests).subscribe({
          next: (items) => {
            this.favorites.set(items);
            this.loading.set(false);
          },
          error: () => {
            this.error.set('Failed to load favorites.');
            this.loading.set(false);
          }
        });
      },
      error: () => {
        this.error.set('Failed to load favorites.');
        this.loading.set(false);
      }
    });
  }

  removeFavorite(item: FavoriteViewItem) {
    this.favoritesService.removeFavorite(item.productId).subscribe({
      next: () => {
        this.favorites.update(list => list.filter(fav => fav.productId !== item.productId));
        this.toastService.success('Removed from wishlist.');
      },
      error: () => {
        this.toastService.error('Unable to remove favorite.');
      }
    });
  }

  addToCart(item: FavoriteViewItem) {
    if (!item.product) {
      this.toastService.error('Product details unavailable.');
      return;
    }

    this.cartService.addItem({ productId: item.productId, quantity: 1 }).subscribe({
      next: () => {
        this.toastService.success(`Added ${item.product?.name ?? 'item'} to cart!`);
      },
      error: () => {
        this.toastService.error('Failed to add item to cart.');
      }
    });
  }
}
