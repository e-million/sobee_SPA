import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

interface AdminNavItem {
  label: string;
  route: string;
  icon: string;
}

@Component({
  selector: 'app-admin-layout',
  imports: [CommonModule, RouterModule],
  templateUrl: './admin-layout.html',
  styleUrl: './admin-layout.css'
})
export class AdminLayout {
  mobileMenuOpen = signal(false);

  navItems: AdminNavItem[] = [
    { label: 'Dashboard', route: '/admin/dashboard', icon: 'chart' },
    { label: 'Products', route: '/admin/products', icon: 'cube' },
    { label: 'Categories', route: '/admin/categories', icon: 'layers' },
    { label: 'Orders', route: '/admin/orders', icon: 'receipt' },
    { label: 'Users', route: '/admin/users', icon: 'users' },
    { label: 'Promos', route: '/admin/promos', icon: 'tag' },
  ];

  toggleMobileMenu() {
    this.mobileMenuOpen.update(value => !value);
  }

  closeMobileMenu() {
    this.mobileMenuOpen.set(false);
  }

  trackByRoute(_index: number, item: AdminNavItem) {
    return item.route;
  }
}
