// Favorite Models

export interface FavoriteItem {
  favoriteId: number;
  productId: number;
  added: string;
}

export interface FavoritesResponse {
  userId: string;
  count: number;
  favorites: FavoriteItem[];
}
