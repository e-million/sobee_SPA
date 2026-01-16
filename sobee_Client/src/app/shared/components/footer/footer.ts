import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-footer',
  imports: [CommonModule, RouterModule],
  templateUrl: './footer.html',
  styleUrl: './footer.css'
})
export class Footer {
  currentYear = new Date().getFullYear();

  helpLinks = [
    { label: 'FAQ', route: '/faq' },
    { label: 'Shipping & Returns', route: '/shipping' },
    { label: 'Contact Us', route: '/contact' },
    { label: 'Refund Policy', route: '/refund-policy' },
  ];

  shopLinks = [
    { label: 'All Products', route: '/shop' },
    { label: 'Best Sellers', route: '/shop?sort=popular' },
    { label: 'New Arrivals', route: '/shop?sort=newest' },
  ];

  companyLinks = [
    { label: 'Our Story', route: '/about' },
    { label: 'Terms of Service', route: '/terms' },
    { label: 'Privacy Policy', route: '/privacy' },
  ];
}
