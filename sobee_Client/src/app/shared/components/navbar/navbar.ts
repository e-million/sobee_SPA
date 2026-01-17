import { Component, DestroyRef, ElementRef, HostListener, ViewChild, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { catchError, debounceTime, distinctUntilChanged, map, of, Subject, switchMap, tap } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '../../../core/services/auth.service';
import { CartService } from '../../../core/services/cart.service';
import { ProductService } from '../../../core/services/product.service';
import { Product } from '../../../core/models';

@Component({
  selector: 'app-navbar',
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './navbar.html',
  styleUrl: './navbar.css'
})
export class Navbar {
  @ViewChild('desktopSearchContainer') desktopSearchContainer?: ElementRef<HTMLDivElement>;
  @ViewChild('mobileSearchContainer') mobileSearchContainer?: ElementRef<HTMLDivElement>;

  mobileMenuOpen = signal(false);
  searchResults = signal<Product[]>([]);
  searchOpen = signal(false);
  searchLoading = signal(false);
  searchTerm = '';

  private readonly searchSubject = new Subject<string>();
  private readonly destroyRef = inject(DestroyRef);

  constructor(
    public authService: AuthService,
    public cartService: CartService,
    private productService: ProductService,
    private router: Router
  ) {
    this.searchSubject.pipe(
      map(term => term.trim()),
      debounceTime(300),
      distinctUntilChanged(),
      tap(term => {
        if (term.length === 0) {
          this.searchResults.set([]);
          this.searchLoading.set(false);
        } else {
          this.searchLoading.set(true);
        }
      }),
      switchMap(term => term.length === 0
        ? of([])
        : this.productService.searchProducts(term).pipe(catchError(() => of([])))
      ),
      map(results => results.slice(0, 5))
    ).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(results => {
      this.searchResults.set(results);
      this.searchLoading.set(false);
    });
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent) {
    const target = event.target as Node;
    const clickedDesktop = this.desktopSearchContainer?.nativeElement.contains(target);
    const clickedMobile = this.mobileSearchContainer?.nativeElement.contains(target);

    if (!clickedDesktop && !clickedMobile) {
      this.searchOpen.set(false);
    }
  }

  @HostListener('document:keydown.escape')
  onEscape() {
    this.searchOpen.set(false);
  }

  toggleMobileMenu() {
    this.mobileMenuOpen.update(v => !v);
  }

  closeMobileMenu() {
    this.mobileMenuOpen.set(false);
  }

  logout() {
    this.authService.logout();
    this.closeMobileMenu();
  }

  onSearchInput() {
    this.searchSubject.next(this.searchTerm);
    this.searchOpen.set(true);
  }

  openSearch() {
    this.searchOpen.set(true);
  }

  closeSearch() {
    this.searchOpen.set(false);
  }

  clearSearch() {
    this.searchTerm = '';
    this.searchResults.set([]);
    this.searchOpen.set(false);
    this.searchSubject.next('');
  }

  onSearchSubmit() {
    const term = this.searchTerm.trim();
    if (!term) {
      return;
    }

    this.router.navigate(['/search'], { queryParams: { q: term } });
    this.closeSearch();
    this.closeMobileMenu();
  }

  selectSearchResult() {
    this.closeSearch();
    this.closeMobileMenu();
  }

  get hasSearchTerm(): boolean {
    return this.searchTerm.trim().length > 0;
  }

  get trimmedSearchTerm(): string {
    return this.searchTerm.trim();
  }

  get cartItemCount(): number {
    const cart = this.cartService.cart();
    if (!cart || !cart.items) return 0;
    return cart.items.reduce((total, item) => total + (item.quantity || 0), 0);
  }
}
