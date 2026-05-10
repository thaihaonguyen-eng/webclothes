using Microsoft.EntityFrameworkCore;

namespace webclothes.Data
{
    public static class SchemaBootstrapper
    {
        public static async Task EnsureLatestSchemaAsync(ApplicationDbContext dbContext)
        {
            await dbContext.Database.MigrateAsync();

            const string sql = """
                IF COL_LENGTH('Products', 'SalePrice') IS NULL
                    ALTER TABLE [Products] ADD [SalePrice] decimal(18,2) NULL;

                IF COL_LENGTH('Products', 'SaleEndDate') IS NULL
                    ALTER TABLE [Products] ADD [SaleEndDate] datetime2 NULL;

                IF COL_LENGTH('Orders', 'ShipperNote') IS NULL
                    ALTER TABLE [Orders] ADD [ShipperNote] nvarchar(max) NULL;

                IF COL_LENGTH('Orders', 'ShipperProofImageUrl') IS NULL
                    ALTER TABLE [Orders] ADD [ShipperProofImageUrl] nvarchar(max) NULL;

                IF COL_LENGTH('Vouchers', 'DiscountType') IS NULL
                    ALTER TABLE [Vouchers] ADD [DiscountType] nvarchar(max) NOT NULL CONSTRAINT [DF_Vouchers_DiscountType] DEFAULT N'Fixed';

                IF COL_LENGTH('Vouchers', 'MaxDiscount') IS NULL
                    ALTER TABLE [Vouchers] ADD [MaxDiscount] decimal(18,2) NOT NULL CONSTRAINT [DF_Vouchers_MaxDiscount] DEFAULT 0;

                IF COL_LENGTH('Vouchers', 'MinOrderAmount') IS NULL
                    ALTER TABLE [Vouchers] ADD [MinOrderAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_Vouchers_MinOrderAmount] DEFAULT 0;

                IF OBJECT_ID(N'[Wishlists]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [Wishlists](
                        [Id] int NOT NULL IDENTITY,
                        [UserId] nvarchar(450) NOT NULL,
                        [ProductId] int NOT NULL,
                        [AddedDate] datetime2 NOT NULL,
                        CONSTRAINT [PK_Wishlists] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_Wishlists_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_Wishlists_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
                    );
                END;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Wishlists_UserId' AND object_id = OBJECT_ID(N'[Wishlists]'))
                    CREATE INDEX [IX_Wishlists_UserId] ON [Wishlists] ([UserId]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Wishlists_ProductId' AND object_id = OBJECT_ID(N'[Wishlists]'))
                    CREATE INDEX [IX_Wishlists_ProductId] ON [Wishlists] ([ProductId]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Wishlists_UserId_ProductId' AND object_id = OBJECT_ID(N'[Wishlists]'))
                    CREATE UNIQUE INDEX [IX_Wishlists_UserId_ProductId] ON [Wishlists] ([UserId], [ProductId]);

                IF OBJECT_ID(N'[Notifications]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [Notifications](
                        [Id] int NOT NULL IDENTITY,
                        [UserId] nvarchar(max) NULL,
                        [TargetRole] nvarchar(max) NULL,
                        [Title] nvarchar(max) NOT NULL,
                        [Message] nvarchar(max) NULL,
                        [Type] nvarchar(max) NOT NULL,
                        [LinkUrl] nvarchar(max) NULL,
                        [IsRead] bit NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id])
                    );
                END;

                IF OBJECT_ID(N'[CartEntries]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [CartEntries](
                        [Id] int NOT NULL IDENTITY,
                        [UserId] nvarchar(450) NOT NULL,
                        [ProductId] int NOT NULL,
                        [Quantity] int NOT NULL,
                        [UpdatedAt] datetime2 NOT NULL,
                        CONSTRAINT [PK_CartEntries] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_CartEntries_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_CartEntries_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
                    );
                END;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CartEntries_UserId' AND object_id = OBJECT_ID(N'[CartEntries]'))
                    CREATE INDEX [IX_CartEntries_UserId] ON [CartEntries] ([UserId]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CartEntries_ProductId' AND object_id = OBJECT_ID(N'[CartEntries]'))
                    CREATE INDEX [IX_CartEntries_ProductId] ON [CartEntries] ([ProductId]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CartEntries_UserId_ProductId' AND object_id = OBJECT_ID(N'[CartEntries]'))
                    CREATE UNIQUE INDEX [IX_CartEntries_UserId_ProductId] ON [CartEntries] ([UserId], [ProductId]);

                IF OBJECT_ID(N'[OrderStatusHistories]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [OrderStatusHistories](
                        [Id] int NOT NULL IDENTITY,
                        [OrderId] int NOT NULL,
                        [Status] nvarchar(max) NULL,
                        [ChangedBy] nvarchar(max) NULL,
                        [Note] nvarchar(max) NULL,
                        [ChangedAt] datetime2 NOT NULL,
                        CONSTRAINT [PK_OrderStatusHistories] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_OrderStatusHistories_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE
                    );
                END;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OrderStatusHistories_OrderId' AND object_id = OBJECT_ID(N'[OrderStatusHistories]'))
                    CREATE INDEX [IX_OrderStatusHistories_OrderId] ON [OrderStatusHistories] ([OrderId]);

                IF COL_LENGTH('Products', 'Slug') IS NULL
                    ALTER TABLE [Products] ADD [Slug] nvarchar(max) NULL;

                IF COL_LENGTH('Products', 'IsDeleted') IS NULL
                    ALTER TABLE [Products] ADD [IsDeleted] bit NOT NULL CONSTRAINT [DF_Products_IsDeleted] DEFAULT 0;

                IF OBJECT_ID(N'[ProductImages]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [ProductImages](
                        [Id] int NOT NULL IDENTITY,
                        [ImageUrl] nvarchar(max) NOT NULL,
                        [IsMain] bit NOT NULL DEFAULT 0,
                        [ProductId] int NOT NULL,
                        CONSTRAINT [PK_ProductImages] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_ProductImages_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
                    );
                END;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProductImages_ProductId' AND object_id = OBJECT_ID(N'[ProductImages]'))
                    CREATE INDEX [IX_ProductImages_ProductId] ON [ProductImages] ([ProductId]);

                -- Fix: chuyển đổi trạng thái đơn hàng từ ASCII sang Unicode
                UPDATE [Orders] SET [Status] = N'Chờ xử lý' WHERE [Status] = N'Cho xu ly';
                UPDATE [Orders] SET [Status] = N'Chờ thanh toán VNPay' WHERE [Status] = N'Cho thanh toan VNPay';
                UPDATE [Orders] SET [Status] = N'Đã thanh toán (VNPay)' WHERE [Status] = N'Da thanh toan (VNPay)';
                UPDATE [OrderStatusHistories] SET [ChangedBy] = N'Hệ thống' WHERE [ChangedBy] = N'He thong';
                UPDATE [OrderStatusHistories] SET [Status] = N'Chờ xử lý' WHERE [Status] = N'Cho xu ly';
                """;

            await dbContext.Database.ExecuteSqlRawAsync(sql);
        }
    }
}
