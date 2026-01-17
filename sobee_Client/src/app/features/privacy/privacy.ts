import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MainLayout } from '../../shared/layout/main-layout';

interface PolicySection {
  title: string;
  content: string[];
}

@Component({
  selector: 'app-privacy',
  imports: [CommonModule, RouterModule, MainLayout],
  templateUrl: './privacy.html',
  styleUrl: './privacy.css'
})
export class Privacy {
  lastUpdated = 'January 2026';

  sections: PolicySection[] = [
    {
      title: 'Information We Collect',
      content: [
        'We collect information you provide, such as name, email, shipping address, and payment details.',
        'We also collect usage data like device type, browser, and pages visited to improve the experience.'
      ]
    },
    {
      title: 'How We Use Information',
      content: [
        'To process orders, deliver products, and provide customer support.',
        'To personalize recommendations and improve site performance.'
      ]
    },
    {
      title: 'Sharing and Disclosure',
      content: [
        'We share data with trusted partners for payment processing and shipping.',
        'We do not sell your personal information to third parties.'
      ]
    },
    {
      title: 'Cookies and Analytics',
      content: [
        'We use cookies to keep you signed in, remember preferences, and understand usage patterns.',
        'You can manage cookie settings in your browser at any time.'
      ]
    },
    {
      title: 'Your Rights',
      content: [
        'You can request access, correction, or deletion of your personal data.',
        'Contact us and we will respond within a reasonable timeframe.'
      ]
    }
  ];
}
