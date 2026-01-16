import { Routes } from '@angular/router';
import { TestPage } from './features/test-page/test-page';
import { Checkout } from './features/checkout/checkout';
import { OrderConfirmation } from './features/order-confirmation/order-confirmation';
import { Orders } from './features/orders/orders';

export const routes: Routes = [
  { path: '', redirectTo: '/test', pathMatch: 'full' },
  { path: 'test', component: TestPage },
  { path: 'checkout', component: Checkout },
  { path: 'order-confirmation/:orderId', component: OrderConfirmation },
  { path: 'orders', component: Orders }
];
