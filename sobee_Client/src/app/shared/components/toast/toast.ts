import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastService } from '../../../core/services/toast.service';

/**
 * Toast container component for rendering notifications.
 */
@Component({
  selector: 'app-toast',
  imports: [CommonModule],
  templateUrl: './toast.html',
  styleUrl: './toast.css'
})
export class Toast {
  /**
   * Expose toast state for the template.
   * @param toastService - ToastService for toast state and actions.
   */
  constructor(public toastService: ToastService) {}

  /**
   * Resolve an icon string for a toast type.
   * @param type - Toast type identifier.
   * @returns Icon string used by the template.
   */
  getIcon(type: string): string {
    switch (type) {
      case 'success': return '';
      case 'error': return 'X';
      case 'warning': return '!';
      case 'info': return 'i';
      default: return 'i';
    }
  }
}
