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
import { Login } from './features/auth/login/login';
import { Register } from './features/auth/register/register';
import { ForgotPassword } from './features/auth/forgot-password/forgot-password';
import { ResetPassword } from './features/auth/reset-password/reset-password';
import { Account } from './features/account/account';
import { NotFound } from './features/not-found/not-found';
import { Cart } from './features/cart/cart';
import { ProductDetail } from './features/product-detail/product-detail';
import { Search } from './features/search/search';
import { authGuard, guestGuard } from './core/guards';

export const routes: Routes = [
  // Public pages
  { path: '', component: Home },
  { path: 'shop', component: Shop },
  { path: 'product/:id', component: ProductDetail },
  { path: 'search', component: Search },
  { path: 'about', component: About },
  { path: 'contact', component: Contact },
  { path: 'faq', component: Faq },

  // Authentication pages (standalone)
  { path: 'login', component: Login, canActivate: [guestGuard] },
  { path: 'register', component: Register, canActivate: [guestGuard] },
  { path: 'forgot-password', component: ForgotPassword, canActivate: [guestGuard] },
  { path: 'reset-password', component: ResetPassword },

  // Shopping flow
  { path: 'cart', component: Cart },
  { path: 'checkout', component: Checkout },
  { path: 'order-confirmation/:orderId', component: OrderConfirmation },

  // Authenticated routes
  { path: 'orders', component: Orders, canActivate: [authGuard] },
  { path: 'account', component: Account, canActivate: [authGuard] },

  // Dev/Test page (keep for debugging)
  { path: 'test', component: TestPage },

  // Redirects for convenience
  { path: 'story', redirectTo: '/about', pathMatch: 'full' },

  // Not found
  { path: '**', component: NotFound }
];
