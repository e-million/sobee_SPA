import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MainLayout } from '../../shared/layout/main-layout';

interface PolicySection {
  title: string;
  content: string[];
}

@Component({
  selector: 'app-shipping',
  imports: [CommonModule, RouterModule, MainLayout],
  templateUrl: './shipping.html',
  styleUrl: './shipping.css'
})
export class Shipping {
  lastUpdated = 'January 2026';

  sections: PolicySection[] = [
    {
      title: 'Shipping Coverage',
      content: [
        'We currently ship within the United States.',
        'Shipping rates are calculated at checkout based on your location and order size.'
      ]
    },
    {
      title: 'Processing Times',
      content: [
        'Orders are processed within 1 to 2 business days.',
        'Orders placed after 2 PM local time ship the next business day.'
      ]
    },
    {
      title: 'Delivery Windows',
      content: [
        'Standard shipping typically arrives in 3 to 5 business days.',
        'Express shipping typically arrives in 1 to 2 business days.',
        'Delivery windows may vary during holidays or severe weather.'
      ]
    },
    {
      title: 'Tracking Your Order',
      content: [
        'Tracking details are emailed once your order ships.',
        'You can also find tracking links in your account order history.'
      ]
    },
    {
      title: 'Returns',
      content: [
        'Returns are accepted within 30 days for unopened products.',
        'See the Refund Policy for full eligibility details.'
      ]
    }
  ];
}
