import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MainLayout } from '../../shared/layout/main-layout';

interface FaqItem {
  question: string;
  answer: string;
  isOpen?: boolean;
}

interface FaqCategory {
  name: string;
  icon: string;
  items: FaqItem[];
}

@Component({
  selector: 'app-faq',
  imports: [CommonModule, RouterModule, MainLayout],
  templateUrl: './faq.html',
  styleUrl: './faq.css'
})
export class Faq {
  categories: FaqCategory[] = [
    {
      name: 'Products',
      icon: 'cube',
      items: [
        {
          question: 'What ingredients are in SoBee drinks?',
          answer: 'SoBee drinks are made with natural ingredients including green tea extract, B-vitamins, natural caffeine, and a blend of adaptogens. We use no artificial sweeteners, colors, or preservatives. Full ingredient lists are available on each product page.'
        },
        {
          question: 'How much caffeine is in each drink?',
          answer: 'Our standard SoBee Energy contains approximately 120mg of natural caffeine per can - about the same as a cup of coffee. Our SoBee Boost variant contains 160mg for those who need extra energy.'
        },
        {
          question: 'Are SoBee drinks vegan-friendly?',
          answer: 'Yes! All SoBee products are 100% vegan and contain no animal-derived ingredients.'
        }
      ]
    },
    {
      name: 'Orders & Shipping',
      icon: 'truck',
      items: [
        {
          question: 'How long does shipping take?',
          answer: 'Standard shipping takes 3-5 business days within the continental US. Express shipping (1-2 business days) is available at checkout for an additional fee.'
        },
        {
          question: 'Do you ship internationally?',
          answer: 'Currently, we only ship within the United States. We\'re working on expanding our shipping to international destinations soon!'
        },
        {
          question: 'How can I track my order?',
          answer: 'Once your order ships, you\'ll receive an email with tracking information. You can also track your order by logging into your account and viewing your order history.'
        }
      ]
    },
    {
      name: 'Returns & Refunds',
      icon: 'refresh',
      items: [
        {
          question: 'What is your return policy?',
          answer: 'We accept returns within 30 days of purchase for unopened products in their original packaging. If you\'re not satisfied with your purchase, please contact us and we\'ll make it right.'
        },
        {
          question: 'How long do refunds take to process?',
          answer: 'Once we receive your return, refunds are typically processed within 5-7 business days. The refund will appear on your original payment method.'
        }
      ]
    },
    {
      name: 'Account & Subscriptions',
      icon: 'user',
      items: [
        {
          question: 'Do you offer subscription plans?',
          answer: 'Yes! Subscribe and save 15% on your favorite SoBee products. You can choose weekly, bi-weekly, or monthly delivery schedules. Cancel or modify anytime.'
        },
        {
          question: 'How do I cancel my subscription?',
          answer: 'You can cancel your subscription anytime from your account settings. Go to My Account > Subscriptions > Manage, and click "Cancel Subscription".'
        },
        {
          question: 'Can I change my delivery address?',
          answer: 'Yes, you can update your delivery address in your account settings before your next shipment is processed. Changes made after processing may not apply to that shipment.'
        }
      ]
    }
  ];

  toggleItem(categoryIndex: number, itemIndex: number) {
    const item = this.categories[categoryIndex].items[itemIndex];
    item.isOpen = !item.isOpen;
  }
}
