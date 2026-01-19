// Cart Models

export interface CartProduct {
  id: number;
  name: string | null;
  description: string | null;
  price: number;
  primaryImageUrl: string | null;
}

export interface CartItem {
  cartItemId: number;
  productId: number | null;
  quantity: number | null;
  added: string | null;
  product: CartProduct | null;
  lineTotal: number;
}

export interface CartPromo {
  code: string | null;
  discountPercentage: number;
}

export interface Cart {
  cartId: number;
  owner: string;
  userId: string | null;
  sessionId: string | null;
  created: string | null;
  updated: string | null;
  items: CartItem[];
  promo: CartPromo | null;
  subtotal: number;
  discount: number;
  total: number;
}

export interface AddCartItemRequest {
  productId: number;
  quantity: number;
}

export interface UpdateCartItemRequest {
  quantity: number;
}

export interface ApplyPromoRequest {
  promoCode: string;
}
