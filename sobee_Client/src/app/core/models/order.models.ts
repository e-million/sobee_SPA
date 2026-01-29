// Order Models

export interface Order {
  orderId: number;
  orderDate: string | null;
  totalAmount: number | null;
  taxAmount: number | null;
  taxRate: number | null;
  orderStatus: string | null;
  ownerType: string;
  userId: string | null;
  guestSessionId: string | null;
  shippingAddress: string | null;
  billingAddress: string | null;
  items: OrderItem[];
  subtotalAmount: number | null;
  discountAmount: number | null;
  discountPercentage: number | null;
  promoCode: string | null;
}

export interface OrderItem {
  orderItemId: number | null;
  productId: number | null;
  productName: string | null;
  unitPrice: number | null;
  quantity: number | null;
  lineTotal: number;
}

export interface CheckoutRequest {
  shippingAddress: string | null;
  billingAddress: string | null;
  paymentMethodId: number | null;
}

export interface PaymentMethod {
  paymentMethodId: number;
  description: string | null;
  isActive: boolean;
}

export interface UpdateOrderStatusRequest {
  status: string;
}
