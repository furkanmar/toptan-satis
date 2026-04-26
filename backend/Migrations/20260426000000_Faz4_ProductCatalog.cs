using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WholesaleApi.Migrations
{
    /// <inheritdoc />
    public partial class Faz4_ProductCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // CatalogItems — temel ürün kataloğu
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""CatalogItems"" (
                    ""Id""           uuid    NOT NULL,
                    ""Name""         text    NOT NULL,
                    ""Description""  text,
                    ""Brand""        text,
                    ""Manufacturer"" text,
                    ""Unit""         text    NOT NULL,
                    ""IsActive""     boolean NOT NULL DEFAULT true,
                    ""CreatedAt""    timestamp without time zone NOT NULL,
                    CONSTRAINT ""PK_CatalogItems"" PRIMARY KEY (""Id"")
                );
            ");

            // CatalogItemBarcodes — ürün başına çoklu barkod
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""CatalogItemBarcodes"" (
                    ""Id""            uuid NOT NULL,
                    ""CatalogItemId"" uuid NOT NULL,
                    ""Barcode""       text NOT NULL,
                    ""Note""          text,
                    CONSTRAINT ""PK_CatalogItemBarcodes"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_CatalogItemBarcodes_CatalogItems_CatalogItemId""
                        FOREIGN KEY (""CatalogItemId"")
                        REFERENCES ""CatalogItems""(""Id"") ON DELETE CASCADE
                );
            ");

            // CatalogItemImages — katalog görselleri
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""CatalogItemImages"" (
                    ""Id""            uuid    NOT NULL,
                    ""CatalogItemId"" uuid    NOT NULL,
                    ""FilePath""      text    NOT NULL,
                    ""IsMain""        boolean NOT NULL DEFAULT false,
                    ""CreatedAt""     timestamp without time zone NOT NULL,
                    CONSTRAINT ""PK_CatalogItemImages"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_CatalogItemImages_CatalogItems_CatalogItemId""
                        FOREIGN KEY (""CatalogItemId"")
                        REFERENCES ""CatalogItems""(""Id"") ON DELETE CASCADE
                );
            ");

            // Products tablosuna CatalogItemId ekle
            migrationBuilder.Sql(@"
                ALTER TABLE ""Products""
                    ADD COLUMN IF NOT EXISTS ""CatalogItemId"" uuid;
            ");

            // Index'ler — IF NOT EXISTS ile güvenli
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_CatalogItemBarcodes_CatalogItemId""
                    ON ""CatalogItemBarcodes""(""CatalogItemId"");

                CREATE INDEX IF NOT EXISTS ""IX_CatalogItemBarcodes_Barcode""
                    ON ""CatalogItemBarcodes""(""Barcode"");

                CREATE INDEX IF NOT EXISTS ""IX_CatalogItemImages_CatalogItemId""
                    ON ""CatalogItemImages""(""CatalogItemId"");

                CREATE INDEX IF NOT EXISTS ""IX_Products_CatalogItemId""
                    ON ""Products""(""CatalogItemId"");
            ");

            // Products → CatalogItems FK (SET NULL — ürün silinse de product kalır)
            migrationBuilder.Sql(@"
                ALTER TABLE ""Products""
                    DROP CONSTRAINT IF EXISTS ""FK_Products_CatalogItems_CatalogItemId"";

                ALTER TABLE ""Products""
                    ADD CONSTRAINT ""FK_Products_CatalogItems_CatalogItemId""
                    FOREIGN KEY (""CatalogItemId"")
                    REFERENCES ""CatalogItems""(""Id"") ON DELETE SET NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""Products"" DROP CONSTRAINT IF EXISTS ""FK_Products_CatalogItems_CatalogItemId"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Products_CatalogItemId"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Products"" DROP COLUMN IF EXISTS ""CatalogItemId"";");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""CatalogItemImages"" CASCADE;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""CatalogItemBarcodes"" CASCADE;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""CatalogItems"" CASCADE;");
        }
    }
}
