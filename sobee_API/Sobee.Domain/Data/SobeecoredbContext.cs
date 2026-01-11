using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Entities.Cart;
using Sobee.Domain.Entities.Orders;
using Sobee.Domain.Entities.Payments;
using Sobee.Domain.Entities.Products;
using Sobee.Domain.Entities.Promotions;
using Sobee.Domain.Entities.Reviews;
using Sobee.Domain.Identity;


namespace Sobee.Domain.Data;

public partial class SobeecoredbContext : DbContext {
	public SobeecoredbContext() {
	}

	public SobeecoredbContext(DbContextOptions<SobeecoredbContext> options)
		: base(options) {
	}



	


	public virtual DbSet<TcartItem> TcartItems { get; set; }

	public virtual DbSet<GuestSession> GuestSessions { get; set; }

	public virtual DbSet<TdrinkCategory> TdrinkCategories { get; set; }

	public virtual DbSet<Tfavorite> Tfavorites { get; set; }

	public virtual DbSet<Tflavor> Tflavors { get; set; }



	public virtual DbSet<Tingredient> Tingredients { get; set; }



	public virtual DbSet<Torder> Torders { get; set; }

	public virtual DbSet<TorderItem> TorderItems { get; set; }

	public virtual DbSet<TordersProduct> TordersProducts { get; set; }

	public virtual DbSet<Tpayment> Tpayments { get; set; }

	public virtual DbSet<TpaymentMethod> TpaymentMethods { get; set; }

	public virtual DbSet<Tproduct> Tproducts { get; set; }

	public virtual DbSet<TproductImage> TproductImages { get; set; }



	public virtual DbSet<TpromoCodeUsageHistory> TpromoCodeUsageHistories { get; set; }

	public virtual DbSet<Tpromotion> Tpromotions { get; set; }

	

	public virtual DbSet<Treview> Treviews { get; set; }


	public virtual DbSet<TshippingMethod> TshippingMethods { get; set; }

	public virtual DbSet<TshippingStatus> TshippingStatuses { get; set; }

	public virtual DbSet<TshoppingCart> TshoppingCarts { get; set; }


	public virtual DbSet<TtransactionType> TtransactionTypes { get; set; }

	public virtual DbSet<TReviewReplies> TReviewReplies { get; set; }




	

	protected override void OnModelCreating(ModelBuilder modelBuilder) {


		modelBuilder.Ignore<ApplicationUser>();

		modelBuilder.Entity<TcartItem>(entity => {
			entity.HasKey(e => e.IntCartItemId).HasName("TCartItems_PK");

			entity.ToTable("TCartItems");

			entity.Property(e => e.IntCartItemId).HasColumnName("intCartItemID");
			entity.Property(e => e.DtmDateAdded)
				.HasColumnType("datetime")
				.HasColumnName("dtmDateAdded");
			entity.Property(e => e.IntProductId).HasColumnName("intProductID");
			entity.Property(e => e.IntQuantity).HasColumnName("intQuantity");
			entity.Property(e => e.IntShoppingCartId).HasColumnName("intShoppingCartID");

			entity.HasOne(d => d.IntProduct).WithMany(p => p.TcartItems)
				.HasForeignKey(d => d.IntProductId)
				.OnDelete(DeleteBehavior.Cascade)
				.HasConstraintName("TCartItems_TProducts_FK");

			entity.HasOne(d => d.IntShoppingCart).WithMany(p => p.TcartItems)
				.HasForeignKey(d => d.IntShoppingCartId)
				.OnDelete(DeleteBehavior.Cascade)
				.HasConstraintName("TCartItems_TShoppingCarts_FK");
            entity.HasIndex(e => new { e.IntShoppingCartId, e.IntProductId })
				 .HasDatabaseName("IX_TCartItems_cart_product");
        });

		modelBuilder.Entity<GuestSession>(entity => {
			entity.HasKey(e => e.SessionId).HasName("GuestSessions_PK");

			entity.ToTable("GuestSessions");

			entity.Property(e => e.SessionId)
				.HasMaxLength(36)
				.HasColumnName("session_id");

			entity.Property(e => e.Secret)
				.HasMaxLength(200)
				.HasColumnName("secret");

			entity.Property(e => e.CreatedAtUtc)
				.HasColumnType("datetime2")
				.HasColumnName("created_at_utc");

			entity.Property(e => e.LastSeenAtUtc)
				.HasColumnType("datetime2")
				.HasColumnName("last_seen_at_utc");

			entity.Property(e => e.ExpiresAtUtc)
				.HasColumnType("datetime2")
				.HasColumnName("expires_at_utc");
		});
		

		modelBuilder.Entity<TdrinkCategory>(entity => {
			entity.HasKey(e => e.IntDrinkCategoryId).HasName("TDrinkCategories_PK");

			entity.ToTable("TDrinkCategories");

			entity.Property(e => e.IntDrinkCategoryId).HasColumnName("intDrinkCategoryID");
			entity.Property(e => e.StrDescription)
				.HasMaxLength(255)
				.IsUnicode(false)
				.HasColumnName("strDescription");
			entity.Property(e => e.StrName)
				.HasMaxLength(255)
				.IsUnicode(false)
				.HasColumnName("strName");
		});

		modelBuilder.Entity<Tfavorite>(entity => {
			entity.HasKey(e => e.IntFavoriteId).HasName("TFavorites_PK");

			entity.ToTable("TFavorites");


			entity.Property(e => e.IntFavoriteId).HasColumnName("intFavoriteID");

			entity.Property(e => e.DtmDateAdded)
				.HasColumnType("datetime")
				.HasColumnName("dtmDateAdded");

			entity.Property(e => e.IntProductId).HasColumnName("intProductID");

			entity.Property(e => e.UserId)
				.HasMaxLength(450)
				.HasColumnName("user_id");


			entity.HasOne(d => d.IntProduct).WithMany(p => p.Tfavorites)
				.HasForeignKey(d => d.IntProductId)
				.HasConstraintName("TFavorites_TProducts_FK");

            entity.HasIndex(e => new { e.UserId, e.IntProductId })
				.IsUnique()
				.HasDatabaseName("UX_TFavorites_user_product");

        });

		modelBuilder.Entity<Tflavor>(entity => {
			entity.HasKey(e => e.IntFlavorId).HasName("TFlavors_PK");

			entity.ToTable("TFlavors");

			entity.Property(e => e.IntFlavorId).HasColumnName("intFlavorID");
			entity.Property(e => e.StrFlavor)
				.HasMaxLength(255)
				.IsUnicode(false)
				.HasColumnName("strFlavor");
		});



		modelBuilder.Entity<Tingredient>(entity => {
			entity.HasKey(e => e.IntIngredientId).HasName("TIngredients_PK");

			entity.ToTable("TIngredients");

			entity.Property(e => e.IntIngredientId).HasColumnName("intIngredientID");
			entity.Property(e => e.StrIngredient)
				.HasMaxLength(255)
				.IsUnicode(false)
				.HasColumnName("strIngredient");
		});

		

		modelBuilder.Entity<Torder>(entity => {
			entity.HasKey(e => e.IntOrderId).HasName("TOrders_PK");

			entity.ToTable("TOrders");

			entity.Property(e => e.IntOrderId).HasColumnName("intOrderID");
			entity.Property(e => e.DecTotalAmount)
				.HasColumnType("decimal(18,2)")
				.HasColumnName("decTotalAmount");
			entity.Property(e => e.DtmOrderDate)
				.HasColumnType("datetime")
				.HasColumnName("dtmOrderDate");
			entity.Property(e => e.IntPaymentMethodId).HasColumnName("intPaymentMethodID");
			entity.Property(e => e.IntShippingStatusId).HasColumnName("intShippingStatusID");
			entity.Property(e => e.SessionId)
				.HasMaxLength(450)
				.HasColumnName("session_id");
			entity.Property(e => e.StrOrderStatus)
				.HasMaxLength(255)
				.IsUnicode(false)
				.HasColumnName("strOrderStatus");
			entity.Property(e => e.StrShippingAddress)
				.HasMaxLength(255)
				.IsUnicode(false)
				.HasColumnName("strShippingAddress");
			entity.Property(e => e.StrTrackingNumber)
				.HasMaxLength(255)
				.IsUnicode(false)
				.HasColumnName("strTrackingNumber");
			entity.Property(e => e.UserId)
				.HasMaxLength(450)
				.HasColumnName("user_id");

			entity.HasOne(d => d.IntPaymentMethod).WithMany(p => p.Torders)
				.HasForeignKey(d => d.IntPaymentMethodId)
				.OnDelete(DeleteBehavior.Cascade)
				.HasConstraintName("TOrders_TPaymentMethods_FK");

			entity.HasOne(d => d.IntShippingStatus).WithMany(p => p.Torders)
				.HasForeignKey(d => d.IntShippingStatusId)
				.OnDelete(DeleteBehavior.Cascade)
				.HasConstraintName("TOrders_TShippingStatus_FK");
          
			entity.HasIndex(e => e.UserId)
				 .HasDatabaseName("IX_TOrders_user_id");
            entity.HasIndex(e => e.SessionId)
                  .HasDatabaseName("IX_TOrders_session_id");

        });

		modelBuilder.Entity<TorderItem>(entity => {
			entity.HasKey(e => e.IntOrderItemId).HasName("TOrderItems_PK");

			entity.ToTable("TOrderItems");

			entity.Property(e => e.IntOrderItemId).HasColumnName("intOrderItemID");
			entity.Property(e => e.IntOrderId).HasColumnName("intOrderID");
			entity.Property(e => e.IntProductId).HasColumnName("intProductID");
			entity.Property(e => e.IntQuantity).HasColumnName("intQuantity");
			entity.Property(e => e.MonPricePerUnit)
				.HasColumnType("money")
				.HasColumnName("monPricePerUnit");

			entity.HasOne(d => d.IntOrder).WithMany(p => p.TorderItems)
				.HasForeignKey(d => d.IntOrderId)
				.OnDelete(DeleteBehavior.Cascade)
				.HasConstraintName("TOrderItems_TOrders_FK");

			entity.HasOne(d => d.IntProduct).WithMany(p => p.TorderItems)
				.HasForeignKey(d => d.IntProductId)
				.OnDelete(DeleteBehavior.Cascade)
				.HasConstraintName("TOrderItems_TProducts_FK");
		});

		modelBuilder.Entity<TordersProduct>(entity => {
			entity.HasKey(e => e.IntOrdersProductId).HasName("TOrdersProducts_PK");

			entity.ToTable("TOrdersProducts");

			entity.Property(e => e.IntOrdersProductId).HasColumnName("intOrdersProductID");
			entity.Property(e => e.IntProductId).HasColumnName("intProductID");
			entity.Property(e => e.StrOrdersProduct)
				.HasMaxLength(255)
				.IsUnicode(false)
				.HasColumnName("strOrdersProduct");
		});

		modelBuilder.Entity<Tpayment>(entity => {
			entity.HasKey(e => e.IntPaymentId).HasName("TPayments_PK");

			entity.ToTable("TPayments");

			entity.Property(e => e.IntPaymentId).HasColumnName("intPaymentID");
			entity.Property(e => e.IntPaymentMethod).HasColumnName("intPaymentMethod");
			entity.Property(e => e.IntPaymentMethodId).HasColumnName("intPaymentMethodID");
			entity.Property(e => e.StrBillingAddress)
				.HasMaxLength(255)
				.IsUnicode(false)
				.HasColumnName("strBillingAddress");

			entity.HasOne(d => d.IntPaymentMethodNavigation).WithMany(p => p.Tpayments)
				.HasForeignKey(d => d.IntPaymentMethodId)
				.OnDelete(DeleteBehavior.Cascade)
				.HasConstraintName("TPayments_TPaymentMethods_FK");
		});

		modelBuilder.Entity<TpaymentMethod>(entity => {
			entity.HasKey(e => e.IntPaymentMethodId).HasName("TPaymentMethods_PK");

			entity.ToTable("TPaymentMethods");

			entity.Property(e => e.IntPaymentMethodId).HasColumnName("intPaymentMethodID");
			entity.Property(e => e.StrBillingAddress)
				.HasMaxLength(255)
				.IsUnicode(false)
				.HasColumnName("strBillingAddress");
			entity.Property(e => e.StrCreditCardDetails)
				.HasMaxLength(255)
				.IsUnicode(false)
				.HasColumnName("strCreditCardDetails");
			entity.Property(e => e.StrDescription)
				.HasMaxLength(255)
				.IsUnicode(false)
				.HasColumnName("strDescription");
		});

		modelBuilder.Entity<Tproduct>(entity => {
			entity.HasKey(e => e.IntProductId).HasName("TProducts_PK");

			entity.ToTable("TProducts");

			entity.Property(e => e.IntProductId).HasColumnName("intProductID");
			entity.Property(e => e.DecPrice)
				.HasColumnType("decimal(18, 2)")
				.HasColumnName("decPrice");
			entity.Property(e => e.strDescription)
				.HasMaxLength(255)
				.IsUnicode(false)
				.HasColumnName("strDescription");
			entity.Property(e => e.StrName)
				.HasMaxLength(255)
				.IsUnicode(false)
				.HasColumnName("strName");
            entity.Property(e => e.IntStockAmount)
			  .HasColumnName("IntStockAmount")
			  .HasDefaultValue(0);

        });

        modelBuilder.Entity<TproductImage>(entity =>
        {
            entity.HasKey(e => e.IntProductImageId).HasName("TProductImages_PK");

            entity.ToTable("TProductImages");

            entity.Property(e => e.IntProductImageId).HasColumnName("intProductImageID");

            entity.Property(e => e.StrProductImageUrl)
                .HasMaxLength(1000)
                .IsUnicode(false)
                .HasColumnName("strProductImageURL");

            // NEW: FK column
            entity.Property(e => e.IntProductId).HasColumnName("intProductID");

            // NEW: index (recommended)
            entity.HasIndex(e => e.IntProductId).HasDatabaseName("IX_TProductImages_intProductID");

            // NEW: relationship
            entity.HasOne(d => d.IntProduct)
                .WithMany(p => p.TproductImages)
                .HasForeignKey(d => d.IntProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });




        modelBuilder.Entity<TpromoCodeUsageHistory>(entity => {
			entity.HasKey(e => e.IntUsageHistoryId).HasName("TPromoCodeUsageHistory_PK");

			entity.ToTable("TPromoCodeUsageHistory");

			entity.Property(e => e.IntUsageHistoryId).HasColumnName("intUsageHistoryID");
			entity.Property(e => e.IntShoppingCartId).HasColumnName("intShoppingCartID");
			entity.Property(e => e.PromoCode)
				.HasMaxLength(50)
				.IsUnicode(false);
			entity.Property(e => e.UsedDateTime).HasColumnType("datetime");

			entity.HasOne(d => d.IntShoppingCart).WithMany(p => p.TpromoCodeUsageHistories)
				.HasForeignKey(d => d.IntShoppingCartId)
				.HasConstraintName("TPromoCodeUsageHistory_TShoppingCarts_FK");
		});

		modelBuilder.Entity<Tpromotion>(entity => {
			entity.HasKey(e => e.IntPromotionId).HasName("TPromotions_PK");

			entity.ToTable("TPromotions");

			entity.Property(e => e.IntPromotionId).HasColumnName("intPromotionID");
			entity.Property(e => e.DecDiscountPercentage)
				.HasColumnType("decimal(18, 2)")
				.HasColumnName("decDiscountPercentage");
			entity.Property(e => e.DtmExpirationDate)
				.HasColumnType("datetime")
				.HasColumnName("dtmExpirationDate");
			entity.Property(e => e.StrDiscountPercentage)
				.HasMaxLength(255)
				.IsUnicode(false)
				.HasColumnName("strDiscountPercentage");
			entity.Property(e => e.StrPromoCode)
				.HasMaxLength(255)
				.IsUnicode(false)
				.HasColumnName("strPromoCode");
		});

		

		modelBuilder.Entity<Treview>(entity => {
			entity.HasKey(e => e.IntReviewId).HasName("TReviews_PK");

			entity.ToTable("TReviews");

			entity.Property(e => e.IntReviewId).HasColumnName("intReviewID");
			entity.Property(e => e.DtmReviewDate)
				.HasColumnType("datetime")
				.HasColumnName("dtmReviewDate");
			entity.Property(e => e.IntProductId).HasColumnName("intProductID");
			entity.Property(e => e.IntRating).HasColumnName("intRating");
			entity.Property(e => e.SessionId)
				.HasMaxLength(450)
				.HasColumnName("session_id");
			entity.Property(e => e.StrReviewText)
				.HasMaxLength(1000)
				.IsUnicode(false)
				.HasColumnName("strReviewText");
			entity.Property(e => e.UserId)
				.HasMaxLength(450)
				.HasColumnName("user_id");

			entity.HasOne(d => d.IntProduct).WithMany(p => p.Treviews)
				.HasForeignKey(d => d.IntProductId)
				.HasConstraintName("TReviews_TProducts_FK");

           
        });

		modelBuilder.Entity<TReviewReplies>(entity => {
			entity.HasKey(e => e.IntReviewReplyID).HasName("TReviewReplies_PK");

			entity.ToTable("TReviewReplies");

			entity.Property(e => e.IntReviewReplyID).HasColumnName("intReviewReplyID");

			entity.Property(e => e.IntReviewId).HasColumnName("review_id");

			entity.Property(e => e.UserId)
				.HasMaxLength(450)
				.HasColumnName("user_id");

			entity.Property(e => e.content)
				.HasColumnName("content")
				.HasColumnType("nvarchar(max)");

			entity.Property(e => e.created_at)
				.HasColumnType("datetime")
				.HasColumnName("created_at")
				.HasDefaultValueSql("(getdate())");

			entity.HasOne(d => d.Treview)
				.WithMany(p => p.TReviewReplies)
				.HasForeignKey(d => d.IntReviewId)
				.HasConstraintName("FK_ReviewReplies_Reviews");

           
        });



		modelBuilder.Entity<TshippingMethod>(entity => {
			entity.HasKey(e => e.IntShippingMethodId).HasName("TShippingMethods_PK");

			entity.ToTable("TShippingMethods");

			entity.Property(e => e.IntShippingMethodId).HasColumnName("intShippingMethodID");
			entity.Property(e => e.DtmEstimatedDelivery)
				.HasColumnType("datetime")
				.HasColumnName("dtmEstimatedDelivery");
			entity.Property(e => e.StrBillingAddress)
				.HasMaxLength(255)
				.IsUnicode(false)
				.HasColumnName("strBillingAddress");
			entity.Property(e => e.StrCost)
				.HasMaxLength(255)
				.IsUnicode(false)
				.HasColumnName("strCost");
			entity.Property(e => e.StrShippingName)
				.HasMaxLength(255)
				.IsUnicode(false)
				.HasColumnName("strShippingName");
		});

		modelBuilder.Entity<TshippingStatus>(entity => {
			entity.HasKey(e => e.IntShippingStatusId).HasName("TShippingStatus_PK");

			entity.ToTable("TShippingStatus");

			entity.Property(e => e.IntShippingStatusId).HasColumnName("intShippingStatusID");
			entity.Property(e => e.StrShippingStatus)
				.HasMaxLength(255)
				.IsUnicode(false)
				.HasColumnName("strShippingStatus");
		});

		modelBuilder.Entity<TshoppingCart>(entity => {
			entity.HasKey(e => e.IntShoppingCartId).HasName("TShoppingCarts_PK");

			entity.ToTable("TShoppingCarts");

			entity.Property(e => e.IntShoppingCartId).HasColumnName("intShoppingCartID");
			entity.Property(e => e.DtmDateCreated)
				.HasColumnType("datetime")
				.HasColumnName("dtmDateCreated");
			entity.Property(e => e.DtmDateLastUpdated)
				.HasColumnType("datetime")
				.HasColumnName("dtmDateLastUpdated");
			entity.Property(e => e.SessionId)
				.HasMaxLength(450)
				.HasColumnName("session_id");
			entity.Property(e => e.UserId)
				.HasMaxLength(450)
				.HasColumnName("user_id");
            entity.HasIndex(e => e.UserId)
			    .HasDatabaseName("IX_TShoppingCarts_user_id");
            entity.HasIndex(e => e.SessionId)
                .HasDatabaseName("IX_TShoppingCarts_session_id");

        });



		modelBuilder.Entity<TtransactionType>(entity => {
			entity.HasKey(e => e.IntTransactionTypeId).HasName("TTransactionTypes_PK");

			entity.ToTable("TTransactionTypes");

			entity.Property(e => e.IntTransactionTypeId).HasColumnName("intTransactionTypeID");
			entity.Property(e => e.StrTransactionType)
				.HasMaxLength(255)
				.IsUnicode(false)
				.HasColumnName("strTransactionType");
		});



		OnModelCreatingPartial(modelBuilder);
	}

	partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
