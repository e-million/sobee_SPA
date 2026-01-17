# Sobee Admin Analytics Proposal

> **Scope**: Database-driven analytics only (no Google Analytics integration)
> **Date**: January 2026

---

## Executive Summary

This document outlines proposed analytics and KPI metrics for the Sobee admin dashboard. All data will be sourced from the application database, providing real-time insights into sales, products, customers, orders, reviews, and inventory.

---

## Current State

The existing admin dashboard ([admin-dashboard.ts](sobee_Client/src/app/features/admin/dashboard/admin-dashboard.ts)) already implements:

| Metric | Data Source | Visualization |
|--------|-------------|---------------|
| Summary Stats | `AdminSummary` | Cards |
| Orders Per Day | `AdminOrdersPerDay[]` (14 days) | Bar chart |
| Low Stock Products | `AdminLowStockProduct[]` | Table |
| Top Products | `AdminTopProduct[]` | Table |

---

## Proposed Analytics Expansion

### 1. Sales KPIs

| Metric | Description | Visualization | Priority |
|--------|-------------|---------------|----------|
| **Total Revenue** | Sum of completed order totals | Card with trend indicator | High |
| **Revenue by Period** | Daily/Weekly/Monthly breakdown | Line chart with period selector | High |
| **Average Order Value (AOV)** | Total revenue / number of orders | Card with trend arrow | High |
| **Revenue by Category** | Sales breakdown by product category | Pie/Donut chart | Medium |
| **Discount Impact** | Total discounts applied, promo usage rate | Card + breakdown table | Medium |
| **Revenue by Payment Method** | Credit card vs other methods | Bar chart | Low |

**Sample API Response**:
```typescript
interface RevenueByPeriod {
  date: string;           // ISO date
  revenue: number;        // Total revenue for period
  orderCount: number;     // Number of orders
  avgOrderValue: number;  // AOV for period
}

interface CategoryRevenue {
  categoryId: number;
  categoryName: string;
  revenue: number;
  percentage: number;     // % of total revenue
  orderCount: number;
}
```

---

### 2. Order Metrics

| Metric | Description | Visualization | Priority |
|--------|-------------|---------------|----------|
| **Orders by Status** | Pending/Processing/Shipped/Delivered/Cancelled | Stacked bar or pie chart | High |
| **Order Fulfillment Time** | Avg time from order placed to shipped | Card with trend | High |
| **Cancellation Rate** | % of orders cancelled | Card with threshold alert | Medium |
| **Orders by Hour** | Peak ordering times | Heatmap or bar chart | Low |
| **Repeat Order Rate** | % of orders from returning customers | Card | Low |

**Sample API Response**:
```typescript
interface OrderStatusBreakdown {
  pending: number;
  processing: number;
  shipped: number;
  delivered: number;
  cancelled: number;
  total: number;
}

interface FulfillmentMetrics {
  avgHoursToShip: number;
  avgHoursToDeliver: number;
  trend: number;          // % change from previous period
}
```

---

### 3. Product Performance

| Metric | Description | Visualization | Priority |
|--------|-------------|---------------|----------|
| **Best Sellers** | Top N products by units sold | Table with ranking (exists) | Done |
| **Worst Performers** | Products with lowest sales | Table | High |
| **Category Performance** | Sales by category with trends | Bar chart | High |
| **Product Views vs Purchases** | Conversion rate by product | Table with conversion % | Medium |
| **Inventory Turnover** | Units sold / avg inventory level | Per-product metric | Medium |
| **Price Point Analysis** | Sales volume by price ranges | Histogram | Low |

**Sample API Response**:
```typescript
interface ProductPerformance {
  productId: number;
  productName: string;
  unitsSold: number;
  revenue: number;
  stockLevel: number;
  turnoverRate: number;   // units sold / avg stock
}

interface CategoryPerformance {
  categoryId: number;
  categoryName: string;
  productCount: number;
  unitsSold: number;
  revenue: number;
  trend: number;          // % change from previous period
}
```

---

### 4. Customer Metrics

| Metric | Description | Visualization | Priority |
|--------|-------------|---------------|----------|
| **New vs Returning** | Customer acquisition vs retention | Pie chart | High |
| **Customer Growth** | New registrations over time | Line chart | High |
| **Top Customers** | Highest spenders | Table | Medium |
| **Customer Lifetime Value (CLV)** | Avg total spend per customer | Card | Medium |
| **Geographic Distribution** | Orders by region/city | Table or map | Low |

**Sample API Response**:
```typescript
interface CustomerBreakdown {
  newCustomers: number;       // First-time buyers in period
  returningCustomers: number; // Repeat buyers in period
  newCustomerRevenue: number;
  returningCustomerRevenue: number;
}

interface CustomerGrowthPoint {
  date: string;
  newRegistrations: number;
  cumulativeTotal: number;
}

interface TopCustomer {
  userId: string;
  email: string;
  name: string;
  totalSpent: number;
  orderCount: number;
  lastOrderDate: string;
}
```

---

### 5. Inventory & Stock

| Metric | Description | Visualization | Priority |
|--------|-------------|---------------|----------|
| **Low Stock Alerts** | Products below threshold | Table with severity (exists) | Done |
| **Out of Stock Count** | Products with 0 inventory | Card with alert | High |
| **Stock Value** | Total inventory value at cost | Card | Medium |
| **Days of Stock** | Estimated days until stockout | Table per product | Medium |
| **Reorder Suggestions** | Products needing restock based on velocity | Table | Low |

**Sample API Response**:
```typescript
interface InventorySummary {
  totalProducts: number;
  inStockCount: number;
  lowStockCount: number;      // Below threshold
  outOfStockCount: number;    // Zero stock
  totalStockValue: number;    // Sum of (cost * quantity)
}

interface ReorderSuggestion {
  productId: number;
  productName: string;
  currentStock: number;
  avgDailySales: number;
  daysUntilStockout: number;
  suggestedReorderQty: number;
}
```

---

### 6. Review & Rating Analytics

| Metric | Description | Visualization | Priority |
|--------|-------------|---------------|----------|
| **Average Rating** | Overall store rating | Star display | High |
| **Rating Distribution** | Count of 1-5 star reviews | Bar chart | High |
| **Recent Reviews** | Latest reviews needing attention | Table | High |
| **Products Needing Reviews** | Products with few/no reviews | Table | Medium |
| **Sentiment Trend** | Avg rating over time | Line chart | Low |

**Sample API Response**:
```typescript
interface RatingDistribution {
  averageRating: number;
  totalReviews: number;
  distribution: {
    oneStar: number;
    twoStar: number;
    threeStar: number;
    fourStar: number;
    fiveStar: number;
  };
}

interface ReviewSummary {
  reviewId: number;
  productId: number;
  productName: string;
  rating: number;
  comment: string;
  customerName: string;
  createdAt: string;
  hasReplies: boolean;
}

interface RatingTrendPoint {
  date: string;
  averageRating: number;
  reviewCount: number;
}
```

---

### 7. Wishlist Analytics

| Metric | Description | Visualization | Priority |
|--------|-------------|---------------|----------|
| **Most Wishlisted** | Products added to wishlists most | Table | Medium |
| **Wishlist Conversion** | % of wishlisted items purchased | Card | Medium |
| **Wishlist to Cart Rate** | Items moved from wishlist to cart | Card | Low |

**Sample API Response**:
```typescript
interface WishlistProduct {
  productId: number;
  productName: string;
  wishlistCount: number;      // Times added to wishlists
  purchaseCount: number;      // Times purchased by wishlisters
  conversionRate: number;     // purchaseCount / wishlistCount
}

interface WishlistMetrics {
  totalWishlistItems: number;
  uniqueProducts: number;
  overallConversionRate: number;
}
```

---

## Proposed API Endpoints

### New Admin Analytics Endpoints

| Endpoint | Method | Description | Priority |
|----------|--------|-------------|----------|
| `/api/admin/analytics/revenue` | GET | Revenue by period with granularity | High |
| `/api/admin/analytics/revenue/categories` | GET | Revenue breakdown by category | Medium |
| `/api/admin/analytics/orders/status` | GET | Order status breakdown | High |
| `/api/admin/analytics/orders/fulfillment` | GET | Fulfillment time metrics | Medium |
| `/api/admin/analytics/products/worst` | GET | Worst performing products | High |
| `/api/admin/analytics/products/categories` | GET | Category performance | High |
| `/api/admin/analytics/customers/breakdown` | GET | New vs returning customers | High |
| `/api/admin/analytics/customers/growth` | GET | Registration growth over time | Medium |
| `/api/admin/analytics/customers/top` | GET | Top customers by spend | Medium |
| `/api/admin/analytics/inventory/summary` | GET | Inventory overview | High |
| `/api/admin/analytics/reviews/distribution` | GET | Rating distribution | High |
| `/api/admin/analytics/reviews/recent` | GET | Recent reviews | High |
| `/api/admin/analytics/reviews/trend` | GET | Rating trend over time | Low |
| `/api/admin/analytics/wishlist/top` | GET | Most wishlisted products | Medium |
| `/api/admin/analytics/wishlist/metrics` | GET | Wishlist conversion metrics | Low |

### Query Parameters

Most endpoints should support:
```
?startDate=2026-01-01&endDate=2026-01-31  // Date range
?granularity=day|week|month               // For time-series data
?limit=10                                 // For top/bottom lists
```

---

## Recommended Dashboard Layout

### Tab Structure

```
[Overview] [Sales] [Products] [Customers] [Orders] [Reviews]
```

### Overview Tab (Default)

```
+------------------+------------------+------------------+------------------+
|   Total Revenue  |   Orders Today   |  New Customers   |   Avg Rating     |
|     $12,450      |       23         |       12         |     4.5 stars    |
|    +5.2% ↑       |    +3 vs avg     |    +8% ↑         |    (142 reviews) |
+------------------+------------------+------------------+------------------+

+----------------------------------------+----------------------------------------+
|          Revenue (Last 30 Days)        |         Orders by Status               |
|                                        |                                        |
|  [Line chart with daily revenue]       |  [Pie chart: Pending/Processing/etc]   |
|                                        |                                        |
+----------------------------------------+----------------------------------------+

+----------------------------------------+----------------------------------------+
|          Low Stock Alerts (5)          |           Top Products                 |
|                                        |                                        |
|  Product A - 3 left                    |  1. Energy Boost - 142 sold            |
|  Product B - 2 left                    |  2. Citrus Blend - 98 sold             |
|  Product C - 4 left                    |  3. Berry Mix - 87 sold                |
+----------------------------------------+----------------------------------------+
```

### Sales Tab

```
+------------------+------------------+------------------+
|   Total Revenue  |       AOV        |  Discount Used   |
|     $45,230      |     $67.50       |     $2,340       |
+------------------+------------------+------------------+

+--------------------------------------------------------+
|              Revenue Over Time                          |
|    [Day] [Week] [Month] selector                        |
|                                                         |
|    [Line chart with selected granularity]               |
+--------------------------------------------------------+

+---------------------------+-----------------------------+
|   Revenue by Category     |    Top Promo Codes          |
|                           |                             |
|   [Pie/Donut chart]       |   SAVE10 - $890 discount    |
|                           |   WELCOME - $450 discount   |
+---------------------------+-----------------------------+
```

### Products Tab

```
+------------------+------------------+------------------+
|  Total Products  |   Out of Stock   |  Avg Turnover    |
|       45         |       3          |    2.3x/month    |
+------------------+------------------+------------------+

+---------------------------+-----------------------------+
|      Top Performers       |     Worst Performers        |
|                           |                             |
|   [Table with ranking]    |   [Table with ranking]      |
|                           |                             |
+---------------------------+-----------------------------+

+--------------------------------------------------------+
|              Category Performance                       |
|                                                         |
|    [Bar chart comparing categories]                     |
+--------------------------------------------------------+
```

### Customers Tab

```
+------------------+------------------+------------------+
| Total Customers  |  New This Month  |   Avg CLV        |
|      1,234       |       89         |    $156          |
+------------------+------------------+------------------+

+---------------------------+-----------------------------+
|   New vs Returning        |    Customer Growth          |
|                           |                             |
|   [Pie chart]             |   [Line chart over time]    |
|                           |                             |
+---------------------------+-----------------------------+

+--------------------------------------------------------+
|                   Top Customers                         |
|                                                         |
|   [Table: Name, Email, Total Spent, Orders, Last Order] |
+--------------------------------------------------------+
```

### Orders Tab

```
+------------------+------------------+------------------+
|  Orders Today    | Avg Fulfillment  | Cancellation %   |
|       23         |    18.5 hours    |      2.3%        |
+------------------+------------------+------------------+

+---------------------------+-----------------------------+
|    Orders by Status       |    Orders Per Day           |
|                           |                             |
|   [Stacked bar chart]     |   [Bar chart - existing]    |
|                           |                             |
+---------------------------+-----------------------------+

+--------------------------------------------------------+
|              Orders by Hour (Peak Times)                |
|                                                         |
|    [Heatmap or bar chart showing hourly distribution]   |
+--------------------------------------------------------+
```

### Reviews Tab

```
+------------------+------------------+------------------+
|   Avg Rating     |  Total Reviews   |  Pending Reply   |
|    4.5 stars     |      142         |       8          |
+------------------+------------------+------------------+

+---------------------------+-----------------------------+
|   Rating Distribution     |    Rating Trend             |
|                           |                             |
|   [Horizontal bar chart]  |   [Line chart over time]    |
|   5★ ████████████ 68      |                             |
|   4★ ████████ 42          |                             |
|   3★ ████ 18              |                             |
|   2★ ██ 9                 |                             |
|   1★ █ 5                  |                             |
+---------------------------+-----------------------------+

+--------------------------------------------------------+
|                   Recent Reviews                        |
|                                                         |
|   [Table: Product, Rating, Comment preview, Date]       |
|   [Click to expand and reply]                           |
+--------------------------------------------------------+
```

---

## Implementation Priority

### Phase 1 - High Priority (Implement First)

1. **Revenue by period** with day/week/month toggle
2. **Orders by status** breakdown (pie/stacked bar)
3. **Rating distribution** and recent reviews table
4. **Worst performing products** (complement existing top products)
5. **Out of stock count** card
6. **New vs returning customers** breakdown

### Phase 2 - Medium Priority

1. **Category performance** comparison
2. **Customer growth** line chart
3. **Top customers** table
4. **Inventory summary** with stock value
5. **Most wishlisted products**
6. **Fulfillment time** metrics

### Phase 3 - Lower Priority

1. **Orders by hour** heatmap
2. **Rating trend** over time
3. **Customer lifetime value** calculation
4. **Wishlist conversion** tracking
5. **Geographic distribution**
6. **Revenue by payment method**

---

## Technical Implementation Notes

### Frontend Components Needed

```
sobee_Client/src/app/features/admin/
├── dashboard/
│   ├── admin-dashboard.ts          # Main dashboard (exists)
│   ├── admin-dashboard.html        # Template (exists)
│   └── admin-dashboard.css         # Styles (exists)
├── analytics/
│   ├── components/
│   │   ├── revenue-chart/          # Line chart for revenue
│   │   ├── status-breakdown/       # Pie chart for order status
│   │   ├── rating-distribution/    # Bar chart for ratings
│   │   ├── metric-card/            # Reusable KPI card
│   │   └── data-table/             # Sortable table component
│   └── analytics.module.ts
```

### Service Updates

```typescript
// admin.service.ts additions
interface AdminService {
  // Existing
  getSummary(): Observable<AdminSummary>;
  getOrdersPerDay(days: number): Observable<AdminOrdersPerDay[]>;
  getLowStock(threshold: number): Observable<AdminLowStockProduct[]>;
  getTopProducts(limit: number): Observable<AdminTopProduct[]>;

  // New - Phase 1
  getRevenueByPeriod(start: string, end: string, granularity: string): Observable<RevenueByPeriod[]>;
  getOrderStatusBreakdown(): Observable<OrderStatusBreakdown>;
  getRatingDistribution(): Observable<RatingDistribution>;
  getRecentReviews(limit: number): Observable<ReviewSummary[]>;
  getWorstProducts(limit: number): Observable<ProductPerformance[]>;
  getInventorySummary(): Observable<InventorySummary>;
  getCustomerBreakdown(): Observable<CustomerBreakdown>;

  // New - Phase 2
  getCategoryPerformance(): Observable<CategoryPerformance[]>;
  getCustomerGrowth(days: number): Observable<CustomerGrowthPoint[]>;
  getTopCustomers(limit: number): Observable<TopCustomer[]>;
  getMostWishlisted(limit: number): Observable<WishlistProduct[]>;
  getFulfillmentMetrics(): Observable<FulfillmentMetrics>;

  // New - Phase 3
  getOrdersByHour(): Observable<HourlyOrders[]>;
  getRatingTrend(days: number): Observable<RatingTrendPoint[]>;
  getWishlistMetrics(): Observable<WishlistMetrics>;
}
```

### Chart Library Recommendation

For Angular 20, consider:
- **ngx-charts** - Angular-native, good for basic charts
- **Chart.js with ng2-charts** - More flexible, widely used
- **Lightweight option**: CSS-only charts for simple bar/pie charts

---

## Backend API Requirements

The following endpoints need to be implemented in [sobee_Core](sobee_Core/):

### Controllers Needed

```csharp
// AdminAnalyticsController.cs
[Route("api/admin/analytics")]
[Authorize(Roles = "Admin")]
public class AdminAnalyticsController : ControllerBase
{
    [HttpGet("revenue")]
    public async Task<ActionResult<List<RevenueByPeriod>>> GetRevenue(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] string granularity = "day") { }

    [HttpGet("orders/status")]
    public async Task<ActionResult<OrderStatusBreakdown>> GetOrderStatus() { }

    [HttpGet("reviews/distribution")]
    public async Task<ActionResult<RatingDistribution>> GetRatingDistribution() { }

    // ... etc
}
```

### Database Queries

Most metrics can be derived from existing tables:
- `Orders` - Revenue, order status, fulfillment time
- `OrderItems` - Product performance, category breakdown
- `Products` - Inventory, stock levels
- `Reviews` - Rating distribution, sentiment
- `Users` - Customer metrics, registration growth
- `Wishlists` - Wishlist analytics

---

## Summary

This proposal outlines 30+ analytics metrics across 7 categories. Implementation should follow the phased approach:

1. **Phase 1** (High Priority): 6 metrics - Core revenue, orders, reviews, products
2. **Phase 2** (Medium Priority): 6 metrics - Categories, customers, inventory
3. **Phase 3** (Low Priority): 6 metrics - Advanced time-based and conversion metrics

Each phase requires coordinated frontend and backend work. The existing admin dashboard provides a solid foundation to build upon.

---

*Document created: January 2026*
