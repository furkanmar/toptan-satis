using Microsoft.EntityFrameworkCore;
using WholesaleApi.Entities;

namespace WholesaleApi.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Users.AnyAsync()) return; // Zaten seed edilmiş

        // ─────────────────────────────────────────────────────────────────────
        // USERS
        // ─────────────────────────────────────────────────────────────────────
        var adminUser = new User { Email = "admin@marifoglu.trade",        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),   Role = UserRole.Admin };
        var wUser1    = new User { Email = "karadeniz@marifoglu.trade",    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Toptan123!"),  Role = UserRole.Wholesaler };
        var wUser2    = new User { Email = "anadolu@marifoglu.trade",      PasswordHash = BCrypt.Net.BCrypt.HashPassword("Toptan123!"),  Role = UserRole.Wholesaler };
        var wUser3    = new User { Email = "marmara@marifoglu.trade",      PasswordHash = BCrypt.Net.BCrypt.HashPassword("Toptan123!"),  Role = UserRole.Wholesaler };
        var sUser1    = new User { Email = "bakkal.ali@marifoglu.trade",   PasswordHash = BCrypt.Net.BCrypt.HashPassword("Magaza123!"),  Role = UserRole.Store };
        var sUser2    = new User { Email = "market.veli@marifoglu.trade",  PasswordHash = BCrypt.Net.BCrypt.HashPassword("Magaza123!"),  Role = UserRole.Store };
        var sUser3    = new User { Email = "dukkan.ayse@marifoglu.trade",  PasswordHash = BCrypt.Net.BCrypt.HashPassword("Magaza123!"),  Role = UserRole.Store };
        var sUser4    = new User { Email = "kose.mehmet@marifoglu.trade",  PasswordHash = BCrypt.Net.BCrypt.HashPassword("Magaza123!"),  Role = UserRole.Store };
        var sUser5    = new User { Email = "cadde.fatma@marifoglu.trade",  PasswordHash = BCrypt.Net.BCrypt.HashPassword("Magaza123!"),  Role = UserRole.Store };
        var sUser6    = new User { Email = "mahalle.hasan@marifoglu.trade",PasswordHash = BCrypt.Net.BCrypt.HashPassword("Magaza123!"),  Role = UserRole.Store };

        db.Users.AddRange(adminUser, wUser1, wUser2, wUser3, sUser1, sUser2, sUser3, sUser4, sUser5, sUser6);
        await db.SaveChangesAsync();

        // ─────────────────────────────────────────────────────────────────────
        // WHOLESALERS & STORES
        // ─────────────────────────────────────────────────────────────────────
        var w1 = new Wholesaler { UserId = wUser1.Id, CompanyName = "Karadeniz Gıda Toptan",  Phone = "0462 111 22 33", Address = "Trabzon, Ortahisar",   Description = "Doğu Karadeniz bölgesinin en büyük gıda toptancısı. Temel gıda, temizlik ve içecek." };
        var w2 = new Wholesaler { UserId = wUser2.Id, CompanyName = "Anadolu İçecek Dağıtım", Phone = "0312 222 33 44", Address = "Ankara, Sincan OSB",    Description = "İçecek ve atıştırmalık kategorisinde uzman dağıtımcı. 500+ ürün çeşidi." };
        var w3 = new Wholesaler { UserId = wUser3.Id, CompanyName = "Marmara Market Tedarik",  Phone = "0212 333 44 55", Address = "İstanbul, Esenyurt",    Description = "Marmara bölgesi market rafı toptancısı. Temizlik, kişisel bakım ve gıda." };
        db.Wholesalers.AddRange(w1, w2, w3);

        var s1 = new Store { UserId = sUser1.Id, StoreName = "Ali'nin Bakkali",    Phone = "0462 511 00 11", Address = "Trabzon, Akçaabat Merkez" };
        var s2 = new Store { UserId = sUser2.Id, StoreName = "Veli Market",        Phone = "0312 611 00 22", Address = "Ankara, Çankaya Kızılay" };
        var s3 = new Store { UserId = sUser3.Id, StoreName = "Ayşe'nin Dükkanı",   Phone = "0216 711 00 33", Address = "İstanbul, Kadıköy Moda" };
        var s4 = new Store { UserId = sUser4.Id, StoreName = "Köşe Marketi",       Phone = "0224 811 00 44", Address = "Bursa, Nilüfer Özlüce" };
        var s5 = new Store { UserId = sUser5.Id, StoreName = "Cadde Süpermarket",  Phone = "0232 911 00 55", Address = "İzmir, Bornova Cumhuriyet" };
        var s6 = new Store { UserId = sUser6.Id, StoreName = "Mahalle Bayi",       Phone = "0462 411 00 66", Address = "Trabzon, Yomra Merkez" };
        db.Stores.AddRange(s1, s2, s3, s4, s5, s6);
        await db.SaveChangesAsync();

        // ─────────────────────────────────────────────────────────────────────
        // STORE ↔ WHOLESALER ATAMALARI
        // ─────────────────────────────────────────────────────────────────────
        db.StoreWholesalers.AddRange(
            new StoreWholesaler { StoreId = s1.Id, WholesalerId = w1.Id },
            new StoreWholesaler { StoreId = s1.Id, WholesalerId = w2.Id },
            new StoreWholesaler { StoreId = s2.Id, WholesalerId = w2.Id },
            new StoreWholesaler { StoreId = s2.Id, WholesalerId = w3.Id },
            new StoreWholesaler { StoreId = s3.Id, WholesalerId = w3.Id },
            new StoreWholesaler { StoreId = s3.Id, WholesalerId = w2.Id },
            new StoreWholesaler { StoreId = s4.Id, WholesalerId = w1.Id },
            new StoreWholesaler { StoreId = s4.Id, WholesalerId = w3.Id },
            new StoreWholesaler { StoreId = s5.Id, WholesalerId = w2.Id },
            new StoreWholesaler { StoreId = s6.Id, WholesalerId = w1.Id }
        );
        await db.SaveChangesAsync();

        // ─────────────────────────────────────────────────────────────────────
        // KATEGORİLER
        // ─────────────────────────────────────────────────────────────────────
        var cat_w1_gida     = new Category { WholesalerId = w1.Id, Name = "Temel Gıda",       Slug = "temel-gida" };
        var cat_w1_icecek   = new Category { WholesalerId = w1.Id, Name = "İçecek",           Slug = "icecek" };
        var cat_w1_temizlik = new Category { WholesalerId = w1.Id, Name = "Temizlik",          Slug = "temizlik" };
        var cat_w1_diger    = new Category { WholesalerId = w1.Id, Name = "Diğer",             Slug = "diger" };

        var cat_w2_icecek   = new Category { WholesalerId = w2.Id, Name = "Gazlı İçecek",     Slug = "gazli-icecek" };
        var cat_w2_su       = new Category { WholesalerId = w2.Id, Name = "Su & Meyve Suyu",  Slug = "su-meyve-suyu" };
        var cat_w2_atistir  = new Category { WholesalerId = w2.Id, Name = "Atıştırmalık",     Slug = "atistirmalik" };
        var cat_w2_diger    = new Category { WholesalerId = w2.Id, Name = "Diğer",             Slug = "diger" };

        var cat_w3_temizlik = new Category { WholesalerId = w3.Id, Name = "Ev Temizliği",     Slug = "ev-temizligi" };
        var cat_w3_bakim    = new Category { WholesalerId = w3.Id, Name = "Kişisel Bakım",    Slug = "kisisel-bakim" };
        var cat_w3_gida     = new Category { WholesalerId = w3.Id, Name = "Paketli Gıda",     Slug = "paketli-gida" };
        var cat_w3_diger    = new Category { WholesalerId = w3.Id, Name = "Diğer",             Slug = "diger" };

        db.Categories.AddRange(
            cat_w1_gida, cat_w1_icecek, cat_w1_temizlik, cat_w1_diger,
            cat_w2_icecek, cat_w2_su, cat_w2_atistir, cat_w2_diger,
            cat_w3_temizlik, cat_w3_bakim, cat_w3_gida, cat_w3_diger
        );
        await db.SaveChangesAsync();

        // ─────────────────────────────────────────────────────────────────────
        // ÜRÜNLER + UNIT CONFIGS + BARKODLAR
        // ─────────────────────────────────────────────────────────────────────

        // Helper: unit config + barkodlarını kaydet, product.Price'ı güncelle
        async Task<Product> AddProduct(Product product, List<(string type, int qty, decimal price, int order, string[] barcodes)> units)
        {
            db.Products.Add(product);
            await db.SaveChangesAsync();

            foreach (var (type, qty, price, sortOrder, barcodes) in units)
            {
                var cfg = new ProductUnitConfig { ProductId = product.Id, UnitType = type, ContentQty = qty, Price = price, SortOrder = sortOrder };
                db.ProductUnitConfigs.Add(cfg);
                await db.SaveChangesAsync();
                foreach (var b in barcodes)
                    db.ProductBarcodes.Add(new ProductBarcode { UnitConfigId = cfg.Id, Barcode = b });
            }

            if (units.Count > 0)
                product.Price = units.Min(u => u.price);

            await db.SaveChangesAsync();
            return product;
        }

        // Karadeniz Gıda
        var p1 = await AddProduct(
            new Product { WholesalerId = w1.Id, CategoryId = cat_w1_icecek.Id, Name = "Coca-Cola Kutu",     Brand = "Coca-Cola",  Manufacturer = "Coca-Cola İçecek A.Ş.", Price = 18.50m, MinOrderQty = 24, Stock = 480 },
            [("Adet", 1, 18.50m, 0, ["5449000000996"]), ("Paket", 6, 105.00m, 1, ["5449000131922"]), ("Koli", 24, 400.00m, 2, ["5449000131966"])]
        );
        var p2 = await AddProduct(
            new Product { WholesalerId = w1.Id, CategoryId = cat_w1_icecek.Id, Name = "Erikli Su 0.5L",     Brand = "Erikli",     Manufacturer = "Nestlé Waters Türkiye",  Price = 5.00m,  MinOrderQty = 12, Stock = 1200 },
            [("Adet", 1, 5.00m, 0, ["8690627010008"]), ("Paket", 6, 27.00m, 1, []), ("Koli", 12, 52.00m, 2, [])]
        );
        var p3 = await AddProduct(
            new Product { WholesalerId = w1.Id, CategoryId = cat_w1_gida.Id,   Name = "Torku Toz Şeker 1kg", Brand = "Torku",     Manufacturer = "Torku Konya Şeker",      Price = 42.00m, MinOrderQty = 10, Stock = 300 },
            [("Adet", 1, 42.00m, 0, ["8690587440110"]), ("Koli", 10, 400.00m, 1, [])]
        );
        var p4 = await AddProduct(
            new Product { WholesalerId = w1.Id, CategoryId = cat_w1_gida.Id,   Name = "Pınar UHT Süt 1lt",  Brand = "Pınar",     Manufacturer = "Yaşar Holding A.Ş.",     Price = 28.00m, MinOrderQty = 12, Stock = 240 },
            [("Adet", 1, 28.00m, 0, ["8690504011088"]), ("Koli", 12, 320.00m, 1, [])]
        );
        var p5 = await AddProduct(
            new Product { WholesalerId = w1.Id, CategoryId = cat_w1_temizlik.Id, Name = "Domestos Çamaşır Suyu 1.5lt", Brand = "Domestos", Manufacturer = "Unilever Türkiye", Price = 35.00m, MinOrderQty = 6, Stock = 144 },
            [("Adet", 1, 35.00m, 0, ["8714100778098"]), ("Koli", 6, 198.00m, 1, [])]
        );
        var p6 = await AddProduct(
            new Product { WholesalerId = w1.Id, CategoryId = cat_w1_gida.Id, Name = "Karadeniz Tereyağı 500g", Brand = "Karadeniz Çiftlik", Manufacturer = "Karadeniz Süt A.Ş.", Description = "Yöresel köy tereyağı, vakum ambalaj", Price = 155.00m, MinOrderQty = 5, Stock = 60 },
            [("Adet", 1, 155.00m, 0, []), ("Koli", 5, 750.00m, 1, [])]
        );
        var p7 = await AddProduct(
            new Product { WholesalerId = w1.Id, CategoryId = cat_w1_gida.Id, Name = "Karadeniz Mısır Unu 1kg", Brand = "Karadeniz Un", Manufacturer = "Karadeniz Gıda Ltd.", Description = "Yerli mısır unu, tuzsuz", Price = 28.00m, MinOrderQty = 20, Stock = 500 },
            [("Adet", 1, 28.00m, 0, []), ("Koli", 20, 530.00m, 1, [])]
        );

        // Anadolu İçecek
        var p8 = await AddProduct(
            new Product { WholesalerId = w2.Id, CategoryId = cat_w2_icecek.Id, Name = "Coca-Cola Kutu",    Brand = "Coca-Cola", Manufacturer = "Coca-Cola İçecek A.Ş.", Price = 19.00m, MinOrderQty = 24, Stock = 720 },
            [("Adet", 1, 19.00m, 0, ["5449000000996"]), ("Paket", 6, 108.00m, 1, []), ("Koli", 24, 420.00m, 2, ["5449000131966"])]
        );
        var p9 = await AddProduct(
            new Product { WholesalerId = w2.Id, CategoryId = cat_w2_icecek.Id, Name = "Pepsi Kutu",        Brand = "Pepsi",     Manufacturer = "PepsiCo Türkiye",       Price = 17.50m, MinOrderQty = 24, Stock = 600 },
            [("Adet", 1, 17.50m, 0, ["4002359007391"]), ("Paket", 6, 99.00m, 1, []), ("Koli", 24, 385.00m, 2, [])]
        );
        var p10 = await AddProduct(
            new Product { WholesalerId = w2.Id, CategoryId = cat_w2_su.Id,    Name = "Erikli Su 0.5L",    Brand = "Erikli",    Manufacturer = "Nestlé Waters Türkiye", Price = 4.80m,  MinOrderQty = 12, Stock = 2400 },
            [("Adet", 1, 4.80m, 0, ["8690627010008"]), ("Koli", 12, 52.80m, 1, [])]
        );
        var p11 = await AddProduct(
            new Product { WholesalerId = w2.Id, CategoryId = cat_w2_su.Id,    Name = "Uludağ Maden Suyu 200ml", Brand = "Uludağ", Manufacturer = "Uludağ İçecek A.Ş.", Price = 8.00m, MinOrderQty = 12, Stock = 360 },
            [("Adet", 1, 8.00m, 0, ["8690627010015"]), ("Koli", 12, 90.00m, 1, [])]
        );
        var p12 = await AddProduct(
            new Product { WholesalerId = w2.Id, CategoryId = cat_w2_atistir.Id, Name = "Ülker Çikolatalı Gofret 36g", Brand = "Ülker", Manufacturer = "Ülker Bisküvi A.Ş.", Price = 9.50m, MinOrderQty = 12, Stock = 480 },
            [("Adet", 1, 9.50m, 0, ["8690504014362"]), ("Koli", 24, 216.00m, 1, [])]
        );
        var p13 = await AddProduct(
            new Product { WholesalerId = w2.Id, CategoryId = cat_w2_atistir.Id, Name = "Eti Tutku Bisküvi 31g", Brand = "Eti", Manufacturer = "Eti Gıda San. A.Ş.", Price = 8.00m, MinOrderQty = 12, Stock = 360 },
            [("Adet", 1, 8.00m, 0, ["8690526617613"]), ("Koli", 24, 180.00m, 1, [])]
        );
        var p14 = await AddProduct(
            new Product { WholesalerId = w2.Id, CategoryId = cat_w2_atistir.Id, Name = "Anadolu Leblebi 250g", Description = "Sarı leblebi, tuzlu", Price = 22.00m, MinOrderQty = 10, Stock = 200 },
            [("Adet", 1, 22.00m, 0, []), ("Koli", 10, 210.00m, 1, [])]
        );

        // Marmara Market
        var p15 = await AddProduct(
            new Product { WholesalerId = w3.Id, CategoryId = cat_w3_temizlik.Id, Name = "Ariel Matik Toz 4kg", Brand = "Ariel", Manufacturer = "Procter & Gamble", Price = 195.00m, MinOrderQty = 3, Stock = 90 },
            [("Adet", 1, 195.00m, 0, ["8001090304247"]), ("Koli", 3, 570.00m, 1, [])]
        );
        var p16 = await AddProduct(
            new Product { WholesalerId = w3.Id, CategoryId = cat_w3_temizlik.Id, Name = "Domestos Çamaşır Suyu 1.5lt", Brand = "Domestos", Manufacturer = "Unilever Türkiye", Price = 33.00m, MinOrderQty = 6, Stock = 120 },
            [("Adet", 1, 33.00m, 0, ["8714100778098"]), ("Koli", 6, 186.00m, 1, [])]
        );
        var p17 = await AddProduct(
            new Product { WholesalerId = w3.Id, CategoryId = cat_w3_gida.Id, Name = "Ülker Çikolatalı Gofret 36g", Brand = "Ülker", Manufacturer = "Ülker Bisküvi A.Ş.", Price = 10.00m, MinOrderQty = 24, Stock = 600 },
            [("Adet", 1, 10.00m, 0, ["8690504014362"]), ("Koli", 24, 228.00m, 1, [])]
        );
        var p18 = await AddProduct(
            new Product { WholesalerId = w3.Id, CategoryId = cat_w3_gida.Id, Name = "Pınar UHT Süt 1lt", Brand = "Pınar", Manufacturer = "Yaşar Holding A.Ş.", Price = 27.50m, MinOrderQty = 12, Stock = 180 },
            [("Adet", 1, 27.50m, 0, ["8690504011088"]), ("Koli", 12, 315.00m, 1, [])]
        );
        var p19 = await AddProduct(
            new Product { WholesalerId = w3.Id, CategoryId = cat_w3_bakim.Id, Name = "Head & Shoulders Şampuan 400ml", Description = "Kepek önleyici şampuan", Price = 85.00m, MinOrderQty = 6, Stock = 72 },
            [("Adet", 1, 85.00m, 0, []), ("Koli", 6, 492.00m, 1, [])]
        );
        var p20 = await AddProduct(
            new Product { WholesalerId = w3.Id, CategoryId = cat_w3_bakim.Id, Name = "Dove Sabun 90g x4", Description = "Nemlendirici sabun 4'lü paket", Price = 65.00m, MinOrderQty = 6, Stock = 96 },
            [("Paket", 4, 65.00m, 0, []), ("Koli", 6, 375.00m, 1, [])]
        );

        // ─────────────────────────────────────────────────────────────────────
        // SİPARİŞLER
        // ─────────────────────────────────────────────────────────────────────
        var now = DateTime.UtcNow;

        async Task<Order> AddOrder(Order order, List<(Product p, int qty, string unitType, int contentQty)> items)
        {
            db.Orders.Add(order);
            await db.SaveChangesAsync();
            foreach (var (p, qty, unitType, contentQty) in items)
            {
                var cfg = db.ProductUnitConfigs.Local.FirstOrDefault(u => u.ProductId == p.Id && u.UnitType == unitType)
                    ?? await db.ProductUnitConfigs.FirstOrDefaultAsync(u => u.ProductId == p.Id && u.UnitType == unitType);
                var unitPrice = cfg?.Price ?? p.Price;
                db.OrderItems.Add(new OrderItem { OrderId = order.Id, ProductId = p.Id, Quantity = qty, UnitPrice = unitPrice, UnitType = unitType, ContentQty = contentQty });
            }
            await db.SaveChangesAsync();
            order.TotalAmount = await db.OrderItems.Where(i => i.OrderId == order.Id).SumAsync(i => i.Quantity * i.UnitPrice);
            await db.SaveChangesAsync();
            return order;
        }

        var o1 = await AddOrder(
            new Order { StoreId = s1.Id, WholesalerId = w1.Id, Status = OrderStatus.Delivered, TotalAmount = 0, Note = "Acil, yarına kadar teslim", WholesalerNote = "Saat 14:00 teslim edildi", DueDate = now.AddDays(30), CreatedAt = now.AddDays(-10), UpdatedAt = now.AddDays(-9) },
            [(p1, 2, "Koli", 24), (p2, 4, "Paket", 6), (p3, 10, "Adet", 1)]
        );
        var o2 = await AddOrder(
            new Order { StoreId = s1.Id, WholesalerId = w2.Id, Status = OrderStatus.Confirmed, TotalAmount = 0, Note = "Haftalık sipariş", DueDate = now.AddDays(15), CreatedAt = now.AddDays(-3), UpdatedAt = now.AddDays(-2) },
            [(p8, 1, "Koli", 24), (p12, 2, "Koli", 24), (p13, 1, "Koli", 24)]
        );
        var o3 = await AddOrder(
            new Order { StoreId = s2.Id, WholesalerId = w3.Id, Status = OrderStatus.Pending, TotalAmount = 0, Note = "Temizlik ürünleri stoğu azaldı", CreatedAt = now.AddDays(-1) },
            [(p15, 3, "Adet", 1), (p16, 6, "Adet", 1)]
        );
        var o4 = await AddOrder(
            new Order { StoreId = s3.Id, WholesalerId = w3.Id, Status = OrderStatus.Delivered, TotalAmount = 0, DueDate = now.AddDays(-5), CreatedAt = now.AddDays(-20), UpdatedAt = now.AddDays(-18) },
            [(p17, 1, "Koli", 24), (p18, 12, "Adet", 1), (p19, 6, "Adet", 1)]
        );
        var o5 = await AddOrder(
            new Order { StoreId = s4.Id, WholesalerId = w1.Id, Status = OrderStatus.Confirmed, TotalAmount = 0, Note = "Ramazan öncesi stok", DueDate = now.AddDays(20), CreatedAt = now.AddDays(-5), UpdatedAt = now.AddDays(-4) },
            [(p3, 5, "Koli", 10), (p4, 2, "Koli", 12), (p6, 5, "Adet", 1)]
        );
        var o6 = await AddOrder(
            new Order { StoreId = s5.Id, WholesalerId = w2.Id, Status = OrderStatus.Rejected, TotalAmount = 0, WholesalerNote = "Stok yetersiz, önümüzdeki hafta tekrar dene", CreatedAt = now.AddDays(-7), UpdatedAt = now.AddDays(-6) },
            [(p9, 5, "Koli", 24), (p10, 10, "Koli", 12)]
        );
        var o7 = await AddOrder(
            new Order { StoreId = s6.Id, WholesalerId = w1.Id, Status = OrderStatus.Pending, TotalAmount = 0, Note = "Her zamanki gibi", CreatedAt = now.AddHours(-2) },
            [(p1, 1, "Koli", 24), (p5, 6, "Adet", 1), (p7, 2, "Koli", 20)]
        );

        // ─────────────────────────────────────────────────────────────────────
        // KREDİ İŞLEMLERİ
        // ─────────────────────────────────────────────────────────────────────
        db.CreditTransactions.AddRange(
            new CreditTransaction { StoreId = s1.Id, WholesalerId = w1.Id, Type = CreditTransactionType.OrderDebit,  Amount = o1.TotalAmount, Description = "Sipariş borcu — teslimat",  OrderId = o1.Id, DueDate = now.AddDays(30), CreatedAt = now.AddDays(-9) },
            new CreditTransaction { StoreId = s1.Id, WholesalerId = w1.Id, Type = CreditTransactionType.Payment,    Amount = 1000.00m,       Description = "Havale ile kısmi ödeme",                      CreatedAt = now.AddDays(-5) },
            new CreditTransaction { StoreId = s1.Id, WholesalerId = w2.Id, Type = CreditTransactionType.OrderDebit,  Amount = o2.TotalAmount, Description = "Sipariş borcu — onaylandı", OrderId = o2.Id, DueDate = now.AddDays(15), CreatedAt = now.AddDays(-2) },
            new CreditTransaction { StoreId = s3.Id, WholesalerId = w3.Id, Type = CreditTransactionType.OrderDebit,  Amount = o4.TotalAmount, Description = "Sipariş borcu — teslimat",  OrderId = o4.Id, DueDate = now.AddDays(-5), CreatedAt = now.AddDays(-18) },
            new CreditTransaction { StoreId = s3.Id, WholesalerId = w3.Id, Type = CreditTransactionType.ManualDebit, Amount = 250.00m,        Description = "Nakliye farkı — ek borç",                     CreatedAt = now.AddDays(-18) },
            new CreditTransaction { StoreId = s4.Id, WholesalerId = w1.Id, Type = CreditTransactionType.OrderDebit,  Amount = o5.TotalAmount, Description = "Sipariş borcu — onaylandı", OrderId = o5.Id, DueDate = now.AddDays(20), CreatedAt = now.AddDays(-4) }
        );
        await db.SaveChangesAsync();
    }

    // ─── Image Seeder (local uploads → migration test için) ───────────────────
    public static async Task SeedImagesAsync(AppDbContext db, string? webRootPath)
    {
        if (await db.ProductImages.AnyAsync()) return;

        // wwwroot yoksa fallback
        webRootPath ??= Path.Combine(AppContext.BaseDirectory, "wwwroot");

        var products = await db.Products.ToListAsync();
        if (products.Count == 0) return;

        var seedDir = Path.Combine(webRootPath, "uploads", "seed");
        Directory.CreateDirectory(seedDir);

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        var rng = new Random(42); // deterministik

        foreach (var product in products)
        {
            var imgCount = rng.Next(1, 4); // 1-3 görsel
            for (var i = 0; i < imgCount; i++)
            {
                try
                {
                    // picsum.photos seed=productId+i → deterministik görsel
                    var picsumSeed = $"{product.Id:N}{i}";
                    var url = $"https://picsum.photos/seed/{picsumSeed}/600/600";
                    var bytes = await http.GetByteArrayAsync(url);

                    var filename = $"product_{product.Id:N}_{i}.jpg";
                    await File.WriteAllBytesAsync(Path.Combine(seedDir, filename), bytes);

                    db.ProductImages.Add(new ProductImage
                    {
                        ProductId = product.Id,
                        FilePath  = $"uploads/seed/{filename}",
                        IsMain    = i == 0
                    });
                }
                catch
                {
                    // Ağ hatası olursa bu görseli atla
                }
            }
        }

        await db.SaveChangesAsync();
    }
}
