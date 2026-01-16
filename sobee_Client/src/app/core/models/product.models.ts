// Product Models

export interface Product {
  id: number;
  name: string | null;
  description: string | null;
  price: number;
  stockAmount?: number | null;
  inStock: boolean;
  primaryImageUrl: string | null;
}

export interface ProductDetails extends Product {
  images?: string[];
  category?: string;
  // Add other detailed fields as needed
}
