# Phase 0 Response Schemas

Purpose
Document the expected response shapes for Phase 0 baseline tests. These schemas reflect current behavior and are used to lock API contracts before refactoring.

Conventions
- All timestamps are ISO-8601 strings.
- Decimals are JSON numbers.
- Error responses use ApiErrorResponse.

Shared Schemas

ApiErrorResponse
```json
{
  "error": "string",
  "code": "string",
  "details": { "any": "object" }
}
```

MessageResponse
```json
{
  "message": "string"
}
```

Cart Schemas

CartResponse
```json
{
  "cartId": 0,
  "owner": "user|guest",
  "userId": "string|null",
  "sessionId": "string|null",
  "created": "2026-01-01T00:00:00Z",
  "updated": "2026-01-01T00:00:00Z",
  "items": [
    {
      "cartItemId": 0,
      "productId": 0,
      "quantity": 0,
      "added": "2026-01-01T00:00:00Z",
      "product": {
        "id": 0,
        "name": "string",
        "description": "string",
        "price": 0.0,
        "primaryImageUrl": "string|null"
      },
      "lineTotal": 0.0
    }
  ],
  "promo": {
    "code": "string",
    "discountPercentage": 0.0
  },
  "subtotal": 0.0,
  "discount": 0.0,
  "total": 0.0
}
```

PromoAppliedResponse
```json
{
  "message": "string",
  "promoCode": "string",
  "discountPercentage": 0.0
}
```

Orders Schemas

OrderResponse
```json
{
  "orderId": 0,
  "orderDate": "2026-01-01T00:00:00Z",
  "totalAmount": 0.0,
  "orderStatus": "string",
  "ownerType": "user|guest",
  "userId": "string|null",
  "guestSessionId": "string|null",
  "subtotalAmount": 0.0,
  "discountAmount": 0.0,
  "discountPercentage": 0.0,
  "promoCode": "string|null",
  "items": [
    {
      "orderItemId": 0,
      "productId": 0,
      "productName": "string",
      "unitPrice": 0.0,
      "quantity": 0,
      "lineTotal": 0.0
    }
  ]
}
```

GetMyOrdersResponse
```json
[
  { "orderId": 0 }
]
```
Headers:
- X-Total-Count
- X-Page
- X-Page-Size

Products Schemas

ProductListResponse
```json
{
  "page": 1,
  "pageSize": 20,
  "totalCount": 0,
  "items": [
    {
      "id": 0,
      "name": "string",
      "description": "string",
      "price": 0.0,
      "cost": 0.0,
      "inStock": true,
      "primaryImageUrl": "string|null",
      "stockAmount": 0,
      "category": "string|null",
      "categoryId": 0
    }
  ]
}
```
Notes:
- cost and stockAmount appear only for admin requests.

ProductDetailResponse
```json
{
  "id": 0,
  "name": "string",
  "description": "string",
  "price": 0.0,
  "inStock": true,
  "stockAmount": 0,
  "category": "string|null",
  "categoryId": 0,
  "cost": 0.0,
  "images": [
    { "id": 0, "url": "string" }
  ]
}
```
Notes:
- stockAmount and cost appear only for admin requests.

ProductImageResponse
```json
{
  "id": 0,
  "productId": 0,
  "url": "string"
}
```

PaymentMethodsResponse
```json
[
  {
    "paymentMethodId": 0,
    "description": "string"
  }
]
```

Reviews Schemas (Smoke)

ReviewsByProductResponse
```json
{
  "productId": 0,
  "page": 1,
  "pageSize": 20,
  "totalCount": 0,
  "count": 0,
  "summary": {
    "total": 0,
    "average": 0.0,
    "counts": [0, 0, 0, 0, 0]
  },
  "reviews": [
    {
      "reviewId": 0,
      "productId": 0,
      "rating": 0,
      "reviewText": "string",
      "created": "2026-01-01T00:00:00Z",
      "userId": "string|null",
      "sessionId": "string|null",
      "replies": [
        {
          "replyId": 0,
          "reviewId": 0,
          "content": "string",
          "created": "2026-01-01T00:00:00Z",
          "userId": "string|null"
        }
      ]
    }
  ]
}
```

Mapping: Test IDs -> Schemas

Cart
- CART-001..CART-006, CART-011: CartResponse
- CART-007..CART-009: PromoAppliedResponse or ApiErrorResponse
- CART-010: MessageResponse or ApiErrorResponse

Orders
- ORDER-001..ORDER-003, ORDER-007..ORDER-010: OrderResponse or ApiErrorResponse
- ORDER-004..ORDER-006: OrderResponse or ApiErrorResponse (with headers for ORDER-006)

Products
- PROD-001..PROD-006: ProductListResponse
- PROD-007: ApiErrorResponse
- PROD-008..PROD-012: ProductDetailResponse or ProductImageResponse or MessageResponse

Smoke
- SMOKE-001: MessageResponse (register response body) or ApiErrorResponse
- SMOKE-002..SMOKE-004, SMOKE-006..SMOKE-009: endpoint-specific success schema
- SMOKE-005: ReviewsByProductResponse
