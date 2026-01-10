using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sobee.Domain.Migrations.Sobee
{
    public partial class StockAsInt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Goal:
            // - End with ONE column named IntStockAmount (int, not null, default 0)
            // - Avoid failing if the DB already has IntStockAmount (or only has strStockAmount)

            migrationBuilder.Sql(@"
                -- If IntStockAmount already exists, do nothing (migration becomes idempotent).
                IF COL_LENGTH('dbo.TProducts', 'IntStockAmount') IS NOT NULL
                BEGIN
                    -- Ensure it has a default constraint (best-effort).
                    IF NOT EXISTS (
                        SELECT 1
                        FROM sys.default_constraints dc
                        JOIN sys.columns c
                            ON c.default_object_id = dc.object_id
                        JOIN sys.tables t
                            ON t.object_id = c.object_id
                        WHERE t.name = 'TProducts'
                          AND c.name = 'IntStockAmount'
                    )
                    BEGIN
                        ALTER TABLE dbo.TProducts
                        ADD CONSTRAINT DF_TProducts_IntStockAmount DEFAULT (0) FOR IntStockAmount;
                    END

                    RETURN;
                END

                -- If the old column exists (strStockAmount), rename it to IntStockAmount.
                IF COL_LENGTH('dbo.TProducts', 'strStockAmount') IS NOT NULL
                BEGIN
                    EXEC sp_rename 'dbo.TProducts.strStockAmount', 'IntStockAmount', 'COLUMN';

                    -- Ensure NOT NULL + default 0 (best-effort).
                    -- If there are NULLs, set them to 0 first.
                    UPDATE dbo.TProducts
                    SET IntStockAmount = 0
                    WHERE IntStockAmount IS NULL;

                    ALTER TABLE dbo.TProducts
                    ALTER COLUMN IntStockAmount int NOT NULL;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM sys.default_constraints dc
                        JOIN sys.columns c
                            ON c.default_object_id = dc.object_id
                        JOIN sys.tables t
                            ON t.object_id = c.object_id
                        WHERE t.name = 'TProducts'
                          AND c.name = 'IntStockAmount'
                    )
                    BEGIN
                        ALTER TABLE dbo.TProducts
                        ADD CONSTRAINT DF_TProducts_IntStockAmount DEFAULT (0) FOR IntStockAmount;
                    END

                    RETURN;
                END

                -- If neither exists, add the column fresh.
                ALTER TABLE dbo.TProducts
                ADD IntStockAmount int NOT NULL CONSTRAINT DF_TProducts_IntStockAmount DEFAULT (0);
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Best-effort rollback:
            // - If IntStockAmount exists, drop it
            // - Recreate strStockAmount as varchar(255) NOT NULL default '0'
            // NOTE: This is only for rollback scenarios; you likely won’t run it.

            migrationBuilder.Sql(@"
                IF COL_LENGTH('dbo.TProducts', 'IntStockAmount') IS NOT NULL
                BEGIN
                    -- Drop default constraint if present
                    DECLARE @df sysname;
                    SELECT @df = dc.name
                    FROM sys.default_constraints dc
                    JOIN sys.columns c
                        ON c.default_object_id = dc.object_id
                    JOIN sys.tables t
                        ON t.object_id = c.object_id
                    WHERE t.name = 'TProducts'
                      AND c.name = 'IntStockAmount';

                    IF @df IS NOT NULL
                        EXEC('ALTER TABLE dbo.TProducts DROP CONSTRAINT [' + @df + ']');

                    ALTER TABLE dbo.TProducts DROP COLUMN IntStockAmount;
                END

                IF COL_LENGTH('dbo.TProducts', 'strStockAmount') IS NULL
                BEGIN
                    ALTER TABLE dbo.TProducts
                    ADD strStockAmount varchar(255) NOT NULL CONSTRAINT DF_TProducts_strStockAmount DEFAULT ('0');
                END
            ");
        }
    }
}
