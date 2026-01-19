import { Injectable, signal } from '@angular/core';

export type ToastType = 'success' | 'error' | 'warning' | 'info';

export interface Toast {
  id: number;
  message: string;
  type: ToastType;
  duration: number;
}

/**
 * Toast service for enqueueing and clearing UI notifications.
 */
@Injectable({
  providedIn: 'root'
})
export class ToastService {
  private nextId = 0;
  toasts = signal<Toast[]>([]);

  /**
   * Show a toast message.
   * @param message - Text to display.
   * @param type - Visual style of the toast.
   * @param duration - Auto-dismiss delay in ms (0 disables auto-dismiss).
   */
  show(message: string, type: ToastType = 'info', duration: number = 5000): void {
    const toast: Toast = {
      id: this.nextId++,
      message,
      type,
      duration
    };

    this.toasts.update(toasts => [...toasts, toast]);

    if (duration > 0) {
      setTimeout(() => this.remove(toast.id), duration);
    }
  }

  /**
   * Show a success toast.
   * @param message - Text to display.
   * @param duration - Auto-dismiss delay in ms.
   */
  success(message: string, duration: number = 5000): void {
    this.show(message, 'success', duration);
  }

  /**
   * Show an error toast.
   * @param message - Text to display.
   * @param duration - Auto-dismiss delay in ms.
   */
  error(message: string, duration: number = 7000): void {
    this.show(message, 'error', duration);
  }

  /**
   * Show a warning toast.
   * @param message - Text to display.
   * @param duration - Auto-dismiss delay in ms.
   */
  warning(message: string, duration: number = 5000): void {
    this.show(message, 'warning', duration);
  }

  /**
   * Show an info toast.
   * @param message - Text to display.
   * @param duration - Auto-dismiss delay in ms.
   */
  info(message: string, duration: number = 5000): void {
    this.show(message, 'info', duration);
  }

  /**
   * Remove a toast by ID.
   * @param id - Toast identifier.
   */
  remove(id: number): void {
    this.toasts.update(toasts => toasts.filter(t => t.id !== id));
  }

  /**
   * Clear all toasts.
   */
  clear(): void {
    this.toasts.set([]);
  }
}
