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

export interface AdminProduct {
  id: number;
  name: string;
  description: string | null;
  price: number;
  stockAmount: number | null;
  primaryImageUrl?: string | null;
}

export interface CreateAdminProductRequest {
  name: string;
  description?: string | null;
  price: number;
  stockAmount: number;
}

export interface UpdateAdminProductRequest {
  name?: string | null;
  description?: string | null;
  price?: number | null;
  stockAmount?: number | null;
}
