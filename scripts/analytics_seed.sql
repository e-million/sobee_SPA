-- Analytics reset + seed script (no stored procedure)
-- Targets the Sobee database (core + identity tables in same DB).
-- Run the migration first so new columns exist.

BEGIN TRY
    BEGIN TRAN;

    -- Clear analytics-related data (leave AspNetUsers intact).
    DELETE FROM TReviewReplies;
    DELETE FROM TReviews;
    DELETE FROM TFavorites;

    DELETE FROM TOrderItems;
    DELETE FROM TOrders;
    DELETE FROM TPayments;

    DELETE FROM TPromoCodeUsageHistory;
    DELETE FROM TCartItems;
    DELETE FROM TShoppingCarts;

    DELETE FROM TProductImages;
    DELETE FROM TProducts;
    DELETE FROM TDrinkCategories;

    -- Use existing users from AspNetUsers (create them via the app/identity endpoints first).
    DECLARE @UserIds TABLE (UserId NVARCHAR(450));
    INSERT INTO @UserIds (UserId)
    SELECT TOP (6) Id
    FROM AspNetUsers
    ORDER BY CreatedDate;

    IF (SELECT COUNT(*) FROM @UserIds) < 6
    BEGIN
        THROW 51000, 'Not enough users in AspNetUsers. Create at least 6 users via the app, then rerun this script.', 1;
    END

    -- Seed categories.
    DECLARE @Categories TABLE (CategoryId INT, Name NVARCHAR(255));
    INSERT INTO TDrinkCategories (strName, strDescription)
    OUTPUT inserted.intDrinkCategoryID, inserted.strName INTO @Categories
    VALUES
        ('Citrus', 'Bright citrus-forward blends.'),
        ('Berry', 'Berry-forward sparkling flavors.'),
        ('Herbal', 'Botanical and herbal blends.'),
        ('Tropical', 'Island-inspired refreshers.'),
        ('Spiced', 'Warm spice and ginger profiles.');

    -- Seed products.
    DECLARE @Products TABLE (ProductId INT, Name NVARCHAR(255), Price DECIMAL(18, 2));
    INSERT INTO TProducts (strName, strDescription, decPrice, decCost, IntStockAmount, intDrinkCategoryID)
    OUTPUT inserted.intProductID, inserted.strName, inserted.decPrice INTO @Products
    VALUES
        ('Citrus Lift', 'Orange and yuzu with bright acidity.', 24.00, 9.50, 42, (SELECT TOP 1 CategoryId FROM @Categories WHERE Name = 'Citrus')),
        ('Lemon Spritz', 'Lemon peel with a crisp finish.', 22.00, 8.75, 12, (SELECT TOP 1 CategoryId FROM @Categories WHERE Name = 'Citrus')),
        ('Berry Glow', 'Blackberry, elderberry, and hibiscus.', 25.00, 10.25, 18, (SELECT TOP 1 CategoryId FROM @Categories WHERE Name = 'Berry')),
        ('Strawberry Tonic', 'Strawberry and botanical fizz.', 23.00, 9.10, 6, (SELECT TOP 1 CategoryId FROM @Categories WHERE Name = 'Berry')),
        ('Herbal Focus', 'Rosemary, basil, and lime.', 26.00, 10.75, 28, (SELECT TOP 1 CategoryId FROM @Categories WHERE Name = 'Herbal')),
        ('Garden Calm', 'Cucumber, mint, and green tea.', 24.00, 9.85, 3, (SELECT TOP 1 CategoryId FROM @Categories WHERE Name = 'Herbal')),
        ('Tropical Wave', 'Pineapple and passionfruit blend.', 27.00, 11.40, 30, (SELECT TOP 1 CategoryId FROM @Categories WHERE Name = 'Tropical')),
        ('Island Citrus', 'Grapefruit with tropical botanicals.', 25.00, 10.15, 15, (SELECT TOP 1 CategoryId FROM @Categories WHERE Name = 'Tropical')),
        ('Spiced Ember', 'Ginger, cinnamon, and clove.', 28.00, 12.00, 8, (SELECT TOP 1 CategoryId FROM @Categories WHERE Name = 'Spiced')),
        ('Chai Spark', 'Black tea, cardamom, and vanilla.', 26.00, 11.10, 5, (SELECT TOP 1 CategoryId FROM @Categories WHERE Name = 'Spiced')),
        ('Citrus Night', 'Blood orange with floral notes.', 24.00, 9.30, 20, (SELECT TOP 1 CategoryId FROM @Categories WHERE Name = 'Citrus')),
        ('Berry Drift', 'Blueberry with subtle botanicals.', 24.50, 9.80, 9, (SELECT TOP 1 CategoryId FROM @Categories WHERE Name = 'Berry'));

    -- Seed orders.
    DECLARE @Orders TABLE (OrderId INT, DiscountPercent DECIMAL(5, 2));
    ;WITH OrderSeed AS (
        SELECT TOP (24)
            ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS RowNum,
            DATEADD(day, -ABS(CHECKSUM(NEWID())) % 30, CAST(SYSUTCDATETIME() AS date)) AS OrderDate,
            CASE
                WHEN ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) % 7 = 0 THEN 'Cancelled'
                WHEN ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) % 6 = 0 THEN 'Refunded'
                WHEN ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) % 5 = 0 THEN 'Delivered'
                WHEN ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) % 4 = 0 THEN 'Shipped'
                WHEN ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) % 3 = 0 THEN 'Processing'
                WHEN ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) % 2 = 0 THEN 'Paid'
                ELSE 'Pending'
            END AS Status,
            CASE WHEN ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) % 5 = 0 THEN 10.0 ELSE NULL END AS DiscountPercent
        FROM sys.all_objects
    )
    INSERT INTO TOrders (
        dtmOrderDate,
        strOrderStatus,
        user_id,
        strPromoCode,
        decDiscountPercentage,
        dtmShippedDate,
        dtmDeliveredDate
    )
    OUTPUT inserted.intOrderID, inserted.decDiscountPercentage INTO @Orders
    SELECT
        os.OrderDate,
        os.Status,
        (SELECT TOP 1 UserId FROM @UserIds ORDER BY NEWID()),
        CASE WHEN os.DiscountPercent IS NOT NULL THEN 'SAVE10' ELSE NULL END,
        os.DiscountPercent,
        CASE
            WHEN os.Status IN ('Shipped', 'Delivered') THEN DATEADD(day, 2, os.OrderDate)
            ELSE NULL
        END,
        CASE
            WHEN os.Status = 'Delivered' THEN DATEADD(day, 5, os.OrderDate)
            ELSE NULL
        END
    FROM OrderSeed os;

    -- Seed order items (2 items per order).
    INSERT INTO TOrderItems (intOrderID, intProductID, intQuantity, monPricePerUnit)
    SELECT
        o.OrderId,
        p.ProductId,
        (ABS(CHECKSUM(NEWID())) % 3) + 1,
        p.Price
    FROM @Orders o
    CROSS APPLY (
        SELECT TOP (2) ProductId, Price
        FROM @Products
        ORDER BY NEWID()
    ) p;

    -- Update order totals based on items.
    ;WITH Totals AS (
        SELECT
            oi.intOrderID AS OrderId,
            SUM((oi.intQuantity) * (oi.monPricePerUnit)) AS Subtotal
        FROM TOrderItems oi
        GROUP BY oi.intOrderID
    )
    UPDATE o
    SET
        o.decSubtotalAmount = t.Subtotal,
        o.decDiscountAmount = CASE
            WHEN o.decDiscountPercentage IS NULL THEN 0
            ELSE ROUND(t.Subtotal * (o.decDiscountPercentage / 100.0), 2)
        END,
        o.decTotalAmount = t.Subtotal - CASE
            WHEN o.decDiscountPercentage IS NULL THEN 0
            ELSE ROUND(t.Subtotal * (o.decDiscountPercentage / 100.0), 2)
        END
    FROM TOrders o
    INNER JOIN Totals t ON t.OrderId = o.intOrderID;

    -- Seed favorites for wishlisted analytics.
    INSERT INTO TFavorites (intProductID, dtmDateAdded, user_id)
    SELECT TOP (30)
        p.ProductId,
        DATEADD(day, -ABS(CHECKSUM(NEWID())) % 30, SYSUTCDATETIME()),
        u.UserId
    FROM @Products p
    CROSS JOIN @UserIds u
    ORDER BY NEWID();

    -- Seed reviews with a spread of ratings.
    INSERT INTO TReviews (intProductID, strReviewText, intRating, dtmReviewDate, user_id)
    SELECT TOP (40)
        p.ProductId,
        CONCAT('Flavor notes on ', p.Name, ' were impressive.'),
        (ABS(CHECKSUM(NEWID())) % 5) + 1,
        DATEADD(day, -ABS(CHECKSUM(NEWID())) % 30, SYSUTCDATETIME()),
        u.UserId
    FROM @Products p
    CROSS JOIN @UserIds u
    ORDER BY NEWID();

    -- Seed replies for recent reviews.
    INSERT INTO TReviewReplies (review_id, user_id, content, created_at)
    SELECT TOP (10)
        r.intReviewID,
        (SELECT TOP 1 UserId FROM @UserIds ORDER BY NEWID()),
        'Thanks for sharing your feedback!',
        DATEADD(day, 1, r.dtmReviewDate)
    FROM TReviews r
    ORDER BY r.dtmReviewDate DESC;

    COMMIT TRAN;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRAN;

    THROW;
END CATCH;
