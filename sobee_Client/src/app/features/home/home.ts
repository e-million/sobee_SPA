import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MainLayout } from '../../shared/layout/main-layout';
import { ProductCard } from '../../shared/components/product-card/product-card';
import { ProductService } from '../../core/services/product.service';
import { CartService } from '../../core/services/cart.service';
import { ToastService } from '../../core/services/toast.service';
import { Product } from '../../core/models';

@Component({
  selector: 'app-home',
  imports: [CommonModule, RouterModule, MainLayout, ProductCard],
  templateUrl: './home.html',
  styleUrl: './home.css'
})
export class Home implements OnInit {
  featuredProducts = signal<Product[]>([]);
  loading = signal(true);

  features = [
    {
      icon: 'bolt',
      title: 'Natural Energy',
      description: 'Powered by natural ingredients that give you sustained energy without the crash.'
    },
    {
      icon: 'leaf',
      title: 'Clean Ingredients',
      description: 'No artificial sweeteners, colors, or preservatives. Just pure, clean energy.'
    },
    {
      icon: 'heart',
      title: 'Great Taste',
      description: 'Bold, refreshing flavors that make every sip enjoyable.'
    },
    {
      icon: 'shield',
      title: 'Lab Tested',
      description: 'Every batch is tested for quality and purity. We never compromise on safety.'
    }
  ];

  constructor(
    private productService: ProductService,
    private cartService: CartService,
    private toastService: ToastService
  ) {}

  ngOnInit() {
    this.loadFeaturedProducts();
  }

  loadFeaturedProducts() {
    this.productService.getProducts().subscribe({
      next: (products) => {
        // Take first 4 products as featured
        this.featuredProducts.set(products.slice(0, 4));
        this.loading.set(false);
      },
      error: () => {
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
}
