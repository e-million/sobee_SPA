// Admin Models

export interface AdminSummary {
  totalOrders: number;
  totalRevenue: number;
  totalDiscounts: number;
  averageOrderValue: number;
}

export interface AdminOrdersPerDay {
  date: string;
  count: number;
  revenue: number;
}

export interface AdminLowStockProduct {
  productId: number;
  name: string;
  stockAmount: number;
}

export interface AdminTopProduct {
  productId: number;
  name: string;
  quantitySold: number;
  revenue: number;
}

export interface AdminRevenuePoint {
  date: string;
  revenue: number;
  orderCount: number;
  avgOrderValue: number;
}

export interface AdminOrderStatusBreakdown {
  total: number;
  pending: number;
  paid: number;
  processing: number;
  shipped: number;
  delivered: number;
  cancelled: number;
  refunded: number;
  other?: number;
}

export interface AdminRatingDistributionCounts {
  oneStar: number;
  twoStar: number;
  threeStar: number;
  fourStar: number;
  fiveStar: number;
}

export interface AdminRatingDistribution {
  averageRating: number;
  totalReviews: number;
  distribution: AdminRatingDistributionCounts;
}

export interface AdminReviewSummary {
  reviewId: number;
  productId: number;
  productName: string;
  rating: number;
  comment: string;
  createdAt: string;
  userId: string | null;
  hasReplies: boolean;
}

export interface AdminWorstProduct {
  productId: number;
  name: string;
  unitsSold: number;
  revenue: number;
}

export interface AdminInventorySummary {
  totalProducts: number;
  inStockCount: number;
  lowStockCount: number;
  outOfStockCount: number;
  totalStockValue: number;
}

export interface AdminCustomerBreakdown {
  newCustomers: number;
  returningCustomers: number;
  newCustomerRevenue: number;
  returningCustomerRevenue: number;
}

export interface AdminCategoryPerformance {
  categoryId: number | null;
  categoryName: string;
  productCount: number;
  unitsSold: number;
  revenue: number;
}

export interface AdminFulfillmentMetrics {
  avgHoursToShip: number;
  avgHoursToDeliver: number;
  trend: number;
}

export interface AdminCustomerGrowthPoint {
  date: string;
  newRegistrations: number;
  cumulativeTotal: number;
}

export interface AdminTopCustomer {
  userId: string;
  email: string | null;
  name: string | null;
  totalSpent: number;
  orderCount: number;
  lastOrderDate: string | null;
}

export interface AdminWishlistProduct {
  productId: number;
  name: string;
  wishlistCount: number;
}

export interface AdminUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  createdDate: string;
  lastLoginDate: string | null;
  roles: string[];
  isAdmin: boolean;
  isLocked: boolean;
  isCurrentUser: boolean;
}

export interface AdminPromo {
  id: number;
  code: string;
  discountPercentage: number;
  expirationDate: string;
  usageCount: number;
  isExpired: boolean;
}

export interface AdminCategory {
  id: number;
  name: string;
  description: string | null;
}

export interface CreateAdminCategoryRequest {
  name: string;
  description?: string | null;
}

export interface UpdateAdminCategoryRequest {
  name?: string | null;
  description?: string | null;
}

export interface AdminProduct {
  id: number;
  name: string;
  description: string | null;
  price: number;
  cost?: number | null;
  stockAmount: number | null;
  category?: string | null;
  categoryId?: number | null;
  primaryImageUrl?: string | null;
}

export interface CreateAdminProductRequest {
  name: string;
  description?: string | null;
  price: number;
  cost?: number | null;
  stockAmount: number;
  categoryId?: number | null;
}

export interface UpdateAdminProductRequest {
  name?: string | null;
  description?: string | null;
  price?: number | null;
  cost?: number | null;
  stockAmount?: number | null;
  categoryId?: number | null;
}
