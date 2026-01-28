import { Injectable, signal } from '@angular/core';

export type ToastType = 'success' | 'error' | 'warning' | 'info';

export type ToastActionStyle = 'primary' | 'outline' | 'danger';

export interface ToastAction {
  label: string;
  style?: ToastActionStyle;
  onClick: () => void;
}

export interface Toast {
  id: number;
  message: string;
  type: ToastType;
  duration: number;
  actions?: ToastAction[];
  dismissible?: boolean;
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
  show(
    message: string,
    type: ToastType = 'info',
    duration: number = 5000,
    options?: { actions?: ToastAction[]; dismissible?: boolean }
  ): void {
    const toast: Toast = {
      id: this.nextId++,
      message,
      type,
      duration,
      actions: options?.actions,
      dismissible: options?.dismissible ?? true
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
   * Show a confirmation toast with OK/Cancel actions.
   * @param message - Text to display.
   * @param onConfirm - Callback when user confirms.
   * @param options - Custom labels and callbacks.
   */
  confirm(
    message: string,
    onConfirm: () => void,
    options?: {
      confirmLabel?: string;
      cancelLabel?: string;
      onCancel?: () => void;
      type?: ToastType;
    }
  ): void {
    const toastId = this.nextId++;
    const removeToast = () => this.remove(toastId);
    const confirmLabel = options?.confirmLabel ?? 'OK';
    const cancelLabel = options?.cancelLabel ?? 'Cancel';

    const toast: Toast = {
      id: toastId,
      message,
      type: options?.type ?? 'warning',
      duration: 0,
      dismissible: false,
      actions: [
        {
          label: cancelLabel,
          style: 'outline',
          onClick: () => {
            options?.onCancel?.();
            removeToast();
          }
        },
        {
          label: confirmLabel,
          style: 'primary',
          onClick: () => {
            onConfirm();
            removeToast();
          }
        }
      ]
    };

    this.toasts.update(toasts => [...toasts, toast]);
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
