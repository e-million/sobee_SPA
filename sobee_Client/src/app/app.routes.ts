import { Routes } from '@angular/router';
import { TestPage } from './features/test-page/test-page';
import { Checkout } from './features/checkout/checkout';
import { OrderConfirmation } from './features/order-confirmation/order-confirmation';
import { Orders } from './features/orders/orders';
import { Home } from './features/home/home';
import { Shop } from './features/shop/shop';
import { About } from './features/about/about';
import { Contact } from './features/contact/contact';
import { Faq } from './features/faq/faq';
import { authGuard } from './core/guards';

export const routes: Routes = [
  // Public pages
  { path: '', component: Home },
  { path: 'shop', component: Shop },
  { path: 'about', component: About },
  { path: 'contact', component: Contact },
  { path: 'faq', component: Faq },

  // Shopping flow
  { path: 'checkout', component: Checkout },
  { path: 'order-confirmation/:orderId', component: OrderConfirmation },

  // Authenticated routes
  { path: 'orders', component: Orders, canActivate: [authGuard] },

  // Dev/Test page (keep for debugging)
  { path: 'test', component: TestPage },

  // Redirects for convenience
  { path: 'story', redirectTo: '/about', pathMatch: 'full' },
];
