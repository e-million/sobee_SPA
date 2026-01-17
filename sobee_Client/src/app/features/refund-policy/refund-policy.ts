import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MainLayout } from '../../shared/layout/main-layout';

interface PolicySection {
  title: string;
  content: string[];
}

@Component({
  selector: 'app-refund-policy',
  imports: [CommonModule, RouterModule, MainLayout],
  templateUrl: './refund-policy.html',
  styleUrl: './refund-policy.css'
})
export class RefundPolicy {
  lastUpdated = 'January 2026';

  sections: PolicySection[] = [
    {
      title: 'Overview',
      content: [
        'We want you to love every Sip. If something is not right, we will make it right.',
        'This policy covers purchases made directly from sobee.com.'
      ]
    },
    {
      title: 'Return Eligibility',
      content: [
        'Returns are accepted within 30 days of delivery.',
        'Products must be unopened and in their original packaging.',
        'Bundles must be returned in full to be eligible for a refund.'
      ]
    },
    {
      title: 'Damaged or Incorrect Items',
      content: [
        'If your order arrives damaged or incorrect, contact us within 7 days.',
        'We will replace the item or issue a refund after review.'
      ]
    },
    {
      title: 'Refunds',
      content: [
        'Refunds are issued to the original payment method.',
        'Processing typically takes 5 to 7 business days after we receive your return.',
        'Shipping fees are non-refundable unless we made an error.'
      ]
    },
    {
      title: 'How to Start a Return',
      content: [
        'Email support@sobee.com with your order number and reason for return.',
        'We will provide instructions and a return authorization if eligible.'
      ]
    }
  ];
}
