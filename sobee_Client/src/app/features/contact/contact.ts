import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MainLayout } from '../../shared/layout/main-layout';
import { ToastService } from '../../core/services/toast.service';

interface ContactForm {
  name: string;
  email: string;
  subject: string;
  message: string;
}

@Component({
  selector: 'app-contact',
  imports: [CommonModule, FormsModule, MainLayout],
  templateUrl: './contact.html',
  styleUrl: './contact.css'
})
export class Contact {
  form: ContactForm = {
    name: '',
    email: '',
    subject: '',
    message: ''
  };

  submitting = signal(false);
  submitted = signal(false);

  subjects = [
    'General Inquiry',
    'Order Support',
    'Product Question',
    'Wholesale Inquiry',
    'Partnership Opportunity',
    'Other'
  ];

  contactInfo = [
    {
      icon: 'mail',
      title: 'Email',
      value: 'hello@sobee.com',
      link: 'mailto:hello@sobee.com'
    },
    {
      icon: 'phone',
      title: 'Phone',
      value: '(555) 123-4567',
      link: 'tel:+15551234567'
    },
    {
      icon: 'location',
      title: 'Address',
      value: '123 Energy Blvd, Austin, TX 78701',
      link: null
    }
  ];

  constructor(private toastService: ToastService) {}

  onSubmit() {
    if (!this.form.name || !this.form.email || !this.form.message) {
      this.toastService.error('Please fill in all required fields');
      return;
    }

    this.submitting.set(true);

    // Simulate API call
    setTimeout(() => {
      this.submitting.set(false);
      this.submitted.set(true);
      this.toastService.success('Message sent successfully!');

      // Reset form
      this.form = {
        name: '',
        email: '',
        subject: '',
        message: ''
      };
    }, 1500);
  }

  resetForm() {
    this.submitted.set(false);
  }
}
