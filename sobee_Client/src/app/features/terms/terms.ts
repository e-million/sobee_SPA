import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MainLayout } from '../../shared/layout/main-layout';

interface PolicySection {
  title: string;
  content: string[];
}

@Component({
  selector: 'app-terms',
  imports: [CommonModule, RouterModule, MainLayout],
  templateUrl: './terms.html',
  styleUrl: './terms.css'
})
export class Terms {
  lastUpdated = 'January 2026';

  sections: PolicySection[] = [
    {
      title: 'Using Our Site',
      content: [
        'By using sobee.com, you agree to follow these terms and all applicable laws.',
        'You may not misuse the site, attempt to access restricted areas, or disrupt service.'
      ]
    },
    {
      title: 'Orders and Pricing',
      content: [
        'All prices are listed in USD and may change without notice.',
        'We reserve the right to cancel orders for pricing errors, fraud risk, or inventory issues.'
      ]
    },
    {
      title: 'Subscriptions',
      content: [
        'Subscription orders renew automatically until canceled.',
        'You can pause or cancel subscriptions from your account settings before the next renewal.'
      ]
    },
    {
      title: 'Intellectual Property',
      content: [
        'All content, branding, and visuals belong to SoBee or our licensors.',
        'You may not copy, modify, or distribute our content without permission.'
      ]
    },
    {
      title: 'Limitation of Liability',
      content: [
        'SoBee is not liable for indirect or incidental damages related to product use or site access.',
        'Our total liability is limited to the amount you paid for the order in question.'
      ]
    }
  ];
}
