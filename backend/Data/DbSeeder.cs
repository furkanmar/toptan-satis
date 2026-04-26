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
        // WHOLESALERS
        // ─────────────────────────────────────────────────────────────────────
        var w1 = new Wholesaler { UserId = wUser1.Id, CompanyName = "Karadeniz Gıda Toptan",  Phone = "0462 111 22 33", Address = "Trabzon, Ortahisar",   Description = "Doğu Karadeniz bölgesinin en büyük gıda toptancısı. Temel gıda, temizlik ve içecek." };
        var w2 = new Wholesaler { UserId = wUser2.Id, CompanyName = "Anadolu İçecek Dağıtım", Phone = "0312 222 33 44", Address = "Ankara, Sincan OSB",    Description = "İçecek ve atıştırmalık kategorisinde uzman dağıtımcı. 500+ ürün çeşidi." };
        var w3 = new Wholesaler { UserId = wUser3.Id, CompanyName = "Marmara Market Tedarik",  Phone = "0212 333 44 55", Address = "İstanbul, Esenyurt",    Description = "Marmara bölgesi market rafı toptancısı. Temizlik, kişisel bakım ve gıda." };

        db.Wholesalers.AddRange(w1, w2, w3);
        await db.SaveChangesAsync();

        // ─────────────────────────────────────────────────────────────────────
        // STORES
        // ─────────────────────────────────────────────────────────────────────
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
            new StoreWholesaler { StoreId = s1.Id, WholesalerId = w1.Id }, // Ali → Karadeniz
            new StoreWholesaler { StoreId = s1.Id, WholesalerId = w2.Id }, // Ali → Anadolu
            new StoreWholesaler { StoreId = s2.Id, WholesalerId = w2.Id }, // Veli → Anadolu
            new StoreWholesaler { StoreId = s2.Id, WholesalerId = w3.Id }, // Veli → Marmara
            new StoreWholesaler { StoreId = s3.Id, WholesalerId = w3.Id }, // Ayşe → Marmara
            new StoreWholesaler { StoreId = s3.Id, WholesalerId = w2.Id }, // Ayşe → Anadolu
            new StoreWholesaler { StoreId = s4.Id, WholesalerId = w1.Id }, // Köşe → Karadeniz
            new StoreWholesaler { StoreId = s4.Id, WholesalerId = w3.Id }, // Köşe → Marmara
            new StoreWholesaler { StoreId = s5.Id, WholesalerId = w2.Id }, // Cadde → Anadolu
            new StoreWholesaler { StoreId = s6.Id, WholesalerId = w1.Id }  // Mahalle → Karadeniz
        );
        await db.SaveChangesAsync();

        // ─────────────────────────────────────────────────────────────────────
        // KATEGORİLER (per-wholesaler)
        // ─────────────────────────────────────────────────────────────────────
        var cat_w1_gida     = new Category { WholesalerId = w1.Id, Name = "Temel Gıda",      Slug = "temel-gida" };
        var cat_w1_icecek   = new Category { WholesalerId = w1.Id, Name = "İçecek",          Slug = "icecek" };
        var cat_w1_temizlik = new Category { WholesalerId = w1.Id, Name = "Temizlik",         Slug = "temizlik" };
        var cat_w1_diger    = new Category { WholesalerId = w1.Id, Name = "Diğer",            Slug = "diger" };

        var cat_w2_icecek   = new Category { WholesalerId = w2.Id, Name = "Gazlı İçecek",    Slug = "gazli-icecek" };
        var cat_w2_su       = new Category { WholesalerId = w2.Id, Name = "Su & Meyve Suyu", Slug = "su-meyve-suyu" };
        var cat_w2_atistir  = new Category { WholesalerId = w2.Id, Name = "Atıştırmalık",    Slug = "atistirmalik" };
        var cat_w2_diger    = new Category { WholesalerId = w2.Id, Name = "Diğer",            Slug = "diger" };

        var cat_w3_temizlik = new Category { WholesalerId = w3.Id, Name = "Ev Temizliği",    Slug = "ev-temizligi" };
        var cat_w3_bakim    = new Category { WholesalerId = w3.Id, Name = "Kişisel Bakım",   Slug = "kisisel-bakim" };
        var cat_w3_gida     = new Category { WholesalerId = w3.Id, Name = "Paketli Gıda",    Slug = "paketli-gida" };
        var cat_w3_diger    = new Category { WholesalerId = w3.Id, Name = "Diğer",            Slug = "diger" };

        db.Categories.AddRange(
            cat_w1_gida, cat_w1_icecek, cat_w1_temizlik, cat_w1_diger,
            cat_w2_icecek, cat_w2_su, cat_w2_atistir, cat_w2_diger,
            cat_w3_temizlik, cat_w3_bakim, cat_w3_gida, cat_w3_diger
        );
        await db.SaveChangesAsync();

        // ─────────────────────────────────────────────────────────────────────
        // GLOBAL KATALOG
        // ─────────────────────────────────────────────────────────────────────
        var cat_cocacola = new CatalogItem { Name = "Coca-Cola Kutu 330ml",        Brand = "Coca-Cola",  Manufacturer = "Coca-Cola İçecek A.Ş.", Unit = ProductUnit.Adet, Description = "330ml teneke kutu" };
        var cat_pepsi    = new CatalogItem { Name = "Pepsi Kutu 330ml",            Brand = "Pepsi",      Manufacturer = "PepsiCo Türkiye",        Unit = ProductUnit.Adet, Description = "330ml teneke kutu" };
        var cat_uludagsu = new CatalogItem { Name = "Uludağ Maden Suyu 200ml",     Brand = "Uludağ",     Manufacturer = "Uludağ İçecek A.Ş.",     Unit = ProductUnit.Adet, Description = "200ml cam şişe" };
        var cat_erikli   = new CatalogItem { Name = "Erikli Su 0.5L",              Brand = "Erikli",     Manufacturer = "Nestlé Waters Türkiye",   Unit = ProductUnit.Adet, Description = "500ml PET şişe" };
        var cat_ulker    = new CatalogItem { Name = "Ülker Çikolatalı Gofret 36g", Brand = "Ülker",      Manufacturer = "Ülker Bisküvi A.Ş.",      Unit = ProductUnit.Adet, Description = "36g gofret" };
        var cat_eti      = new CatalogItem { Name = "Eti Tutku 31g",               Brand = "Eti",        Manufacturer = "Eti Gıda San. A.Ş.",      Unit = ProductUnit.Adet, Description = "31g çikolatalı bisküvi" };
        var cat_torku    = new CatalogItem { Name = "Torku Toz Şeker 1kg",         Brand = "Torku",      Manufacturer = "Torku Konya Şeker",        Unit = ProductUnit.Kg,   Description = "1kg rafine şeker" };
        var cat_pinar    = new CatalogItem { Name = "Pınar Tam Yağlı UHT Süt 1lt", Brand = "Pınar",     Manufacturer = "Yaşar Holding A.Ş.",       Unit = ProductUnit.Adet, Description = "1lt UHT süt" };
        var cat_ariel    = new CatalogItem { Name = "Ariel Matik Toz 4kg",         Brand = "Ariel",      Manufacturer = "Procter & Gamble",         Unit = ProductUnit.Kg,   Description = "4kg çamaşır deterjanı" };
        var cat_domestos = new CatalogItem { Name = "Domestos Çamaşır Suyu 1.5lt", Brand = "Domestos",   Manufacturer = "Unilever Türkiye",         Unit = ProductUnit.Adet, Description = "1.5lt çamaşır suyu" };

        db.CatalogItems.AddRange(cat_cocacola, cat_pepsi, cat_uludagsu, cat_erikli, cat_ulker, cat_eti, cat_torku, cat_pinar, cat_ariel, cat_domestos);
        await db.SaveChangesAsync();

        db.CatalogItemBarcodes.AddRange(
            new CatalogItemBarcode { CatalogItemId = cat_cocacola.Id, Barcode = "5449000000996",  Note = "TR standart" },
            new CatalogItemBarcode { CatalogItemId = cat_cocacola.Id, Barcode = "5449000131966",  Note = "koli barkodu" },
            new CatalogItemBarcode { CatalogItemId = cat_pepsi.Id,    Barcode = "4002359007391" },
            new CatalogItemBarcode { CatalogItemId = cat_uludagsu.Id, Barcode = "8690627010015" },
            new CatalogItemBarcode { CatalogItemId = cat_erikli.Id,   Barcode = "8690627010008" },
            new CatalogItemBarcode { CatalogItemId = cat_ulker.Id,    Barcode = "8690504014362" },
            new CatalogItemBarcode { CatalogItemId = cat_eti.Id,      Barcode = "8690526617613" },
            new CatalogItemBarcode { CatalogItemId = cat_torku.Id,    Barcode = "8690587440110" },
            new CatalogItemBarcode { CatalogItemId = cat_pinar.Id,    Barcode = "8690504011088" },
            new CatalogItemBarcode { CatalogItemId = cat_ariel.Id,    Barcode = "8001090304247" },
            new CatalogItemBarcode { CatalogItemId = cat_domestos.Id, Barcode = "8714100778098" }
        );
        await db.SaveChangesAsync();

        // ─────────────────────────────────────────────────────────────────────
        // ÜRÜNLER
        // ─────────────────────────────────────────────────────────────────────

        // Karadeniz Gıda
        var p1  = new Product { WholesalerId = w1.Id, CategoryId = cat_w1_icecek.Id,   CatalogItemId = cat_cocacola.Id, Name = "Coca-Cola Kutu 330ml",        Price = 18.50m,  Unit = ProductUnit.Adet,   MinOrderQty = 24, Stock = 480  };
        var p2  = new Product { WholesalerId = w1.Id, CategoryId = cat_w1_icecek.Id,   CatalogItemId = cat_erikli.Id,   Name = "Erikli Su 0.5L",              Price = 5.00m,   Unit = ProductUnit.Adet,   MinOrderQty = 12, Stock = 1200 };
        var p3  = new Product { WholesalerId = w1.Id, CategoryId = cat_w1_gida.Id,     CatalogItemId = cat_torku.Id,    Name = "Torku Toz Şeker 1kg",          Price = 42.00m,  Unit = ProductUnit.Kg,     MinOrderQty = 10, Stock = 300  };
        var p4  = new Product { WholesalerId = w1.Id, CategoryId = cat_w1_gida.Id,     CatalogItemId = cat_pinar.Id,    Name = "Pınar UHT Süt 1lt",            Price = 28.00m,  Unit = ProductUnit.Adet,   MinOrderQty = 12, Stock = 240  };
        var p5  = new Product { WholesalerId = w1.Id, CategoryId = cat_w1_temizlik.Id, CatalogItemId = cat_domestos.Id, Name = "Domestos Çamaşır Suyu 1.5lt",  Price = 35.00m,  Unit = ProductUnit.Adet,   MinOrderQty = 6,  Stock = 144  };
        var p6  = new Product { WholesalerId = w1.Id, CategoryId = cat_w1_gida.Id,     CatalogItemId = null,            Name = "Karadeniz Tereyağı 500g",      Price = 155.00m, Unit = ProductUnit.Adet,   MinOrderQty = 5,  Stock = 60,  Description = "Yöresel köy tereyağı, vakum ambalaj" };
        var p7  = new Product { WholesalerId = w1.Id, CategoryId = cat_w1_gida.Id,     CatalogItemId = null,            Name = "Karadeniz Mısır Unu 1kg",      Price = 28.00m,  Unit = ProductUnit.Kg,     MinOrderQty = 20, Stock = 500, Description = "Yerli mısır unu, tuzsuz" };

        // Anadolu İçecek
        var p8  = new Product { WholesalerId = w2.Id, CategoryId = cat_w2_icecek.Id,   CatalogItemId = cat_cocacola.Id, Name = "Coca-Cola Kutu 330ml",        Price = 19.00m,  Unit = ProductUnit.Adet,   MinOrderQty = 24, Stock = 720  };
        var p9  = new Product { WholesalerId = w2.Id, CategoryId = cat_w2_icecek.Id,   CatalogItemId = cat_pepsi.Id,    Name = "Pepsi Kutu 330ml",             Price = 17.50m,  Unit = ProductUnit.Adet,   MinOrderQty = 24, Stock = 600  };
        var p10 = new Product { WholesalerId = w2.Id, CategoryId = cat_w2_su.Id,       CatalogItemId = cat_erikli.Id,   Name = "Erikli Su 0.5L",              Price = 4.80m,   Unit = ProductUnit.Adet,   MinOrderQty = 12, Stock = 2400 };
        var p11 = new Product { WholesalerId = w2.Id, CategoryId = cat_w2_su.Id,       CatalogItemId = cat_uludagsu.Id, Name = "Uludağ Maden Suyu 200ml",     Price = 8.00m,   Unit = ProductUnit.Adet,   MinOrderQty = 12, Stock = 360  };
        var p12 = new Product { WholesalerId = w2.Id, CategoryId = cat_w2_atistir.Id,  CatalogItemId = cat_ulker.Id,    Name = "Ülker Çikolatalı Gofret 36g", Price = 9.50m,   Unit = ProductUnit.Adet,   MinOrderQty = 12, Stock = 480  };
        var p13 = new Product { WholesalerId = w2.Id, CategoryId = cat_w2_atistir.Id,  CatalogItemId = cat_eti.Id,      Name = "Eti Tutku Bisküvi 31g",        Price = 8.00m,   Unit = ProductUnit.Adet,   MinOrderQty = 12, Stock = 360  };
        var p14 = new Product { WholesalerId = w2.Id, CategoryId = cat_w2_atistir.Id,  CatalogItemId = null,            Name = "Anadolu Leblebi 250g",         Price = 22.00m,  Unit = ProductUnit.Adet,   MinOrderQty = 10, Stock = 200, Description = "Sarı leblebi, tuzlu" };

        // Marmara Market
        var p15 = new Product { WholesalerId = w3.Id, CategoryId = cat_w3_temizlik.Id, CatalogItemId = cat_ariel.Id,    Name = "Ariel Matik Toz 4kg",         Price = 195.00m, Unit = ProductUnit.Kg,     MinOrderQty = 3,  Stock = 90   };
        var p16 = new Product { WholesalerId = w3.Id, CategoryId = cat_w3_temizlik.Id, CatalogItemId = cat_domestos.Id, Name = "Domestos Çamaşır Suyu 1.5lt", Price = 33.00m,  Unit = ProductUnit.Adet,   MinOrderQty = 6,  Stock = 120  };
        var p17 = new Product { WholesalerId = w3.Id, CategoryId = cat_w3_gida.Id,     CatalogItemId = cat_ulker.Id,    Name = "Ülker Çikolatalı Gofret 36g", Price = 10.00m,  Unit = ProductUnit.Adet,   MinOrderQty = 24, Stock = 600  };
        var p18 = new Product { WholesalerId = w3.Id, CategoryId = cat_w3_gida.Id,     CatalogItemId = cat_pinar.Id,    Name = "Pınar UHT Süt 1lt",            Price = 27.50m,  Unit = ProductUnit.Adet,   MinOrderQty = 12, Stock = 180  };
        var p19 = new Product { WholesalerId = w3.Id, CategoryId = cat_w3_bakim.Id,    CatalogItemId = null,            Name = "Head & Shoulders Şampuan 400ml",Price = 85.00m, Unit = ProductUnit.Adet,   MinOrderQty = 6,  Stock = 72,  Description = "Kepek önleyici şampuan" };
        var p20 = new Product { WholesalerId = w3.Id, CategoryId = cat_w3_bakim.Id,    CatalogItemId = null,            Name = "Dove Sabun 90g x4",            Price = 65.00m,  Unit = ProductUnit.Paket,  MinOrderQty = 6,  Stock = 96,  Description = "Nemlendirici sabun 4'lü paket" };

        db.Products.AddRange(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14, p15, p16, p17, p18, p19, p20);
        await db.SaveChangesAsync();

        // ─────────────────────────────────────────────────────────────────────
        // SİPARİŞLER
        // ─────────────────────────────────────────────────────────────────────
        var now = DateTime.UtcNow;

        // Sipariş 1: Ali → Karadeniz (Delivered)
        var o1 = new Order { StoreId = s1.Id, WholesalerId = w1.Id, Status = OrderStatus.Delivered, TotalAmount = 0, Note = "Acil, yarına kadar teslim", WholesalerNote = "Saat 14:00 teslim edildi", DueDate = now.AddDays(30), CreatedAt = now.AddDays(-10), UpdatedAt = now.AddDays(-9) };
        db.Orders.Add(o1); await db.SaveChangesAsync();
        var oi1a = new OrderItem { OrderId = o1.Id, ProductId = p1.Id, Quantity = 48, UnitPrice = p1.Price };
        var oi1b = new OrderItem { OrderId = o1.Id, ProductId = p2.Id, Quantity = 24, UnitPrice = p2.Price };
        var oi1c = new OrderItem { OrderId = o1.Id, ProductId = p3.Id, Quantity = 20, UnitPrice = p3.Price };
        db.OrderItems.AddRange(oi1a, oi1b, oi1c);
        o1.TotalAmount = oi1a.Quantity * oi1a.UnitPrice + oi1b.Quantity * oi1b.UnitPrice + oi1c.Quantity * oi1c.UnitPrice;
        await db.SaveChangesAsync();

        // Sipariş 2: Ali → Anadolu (Confirmed)
        var o2 = new Order { StoreId = s1.Id, WholesalerId = w2.Id, Status = OrderStatus.Confirmed, TotalAmount = 0, Note = "Haftalık sipariş", DueDate = now.AddDays(15), CreatedAt = now.AddDays(-3), UpdatedAt = now.AddDays(-2) };
        db.Orders.Add(o2); await db.SaveChangesAsync();
        var oi2a = new OrderItem { OrderId = o2.Id, ProductId = p8.Id,  Quantity = 24, UnitPrice = p8.Price };
        var oi2b = new OrderItem { OrderId = o2.Id, ProductId = p12.Id, Quantity = 12, UnitPrice = p12.Price };
        var oi2c = new OrderItem { OrderId = o2.Id, ProductId = p13.Id, Quantity = 12, UnitPrice = p13.Price };
        db.OrderItems.AddRange(oi2a, oi2b, oi2c);
        o2.TotalAmount = oi2a.Quantity * oi2a.UnitPrice + oi2b.Quantity * oi2b.UnitPrice + oi2c.Quantity * oi2c.UnitPrice;
        await db.SaveChangesAsync();

        // Sipariş 3: Veli → Marmara (Pending)
        var o3 = new Order { StoreId = s2.Id, WholesalerId = w3.Id, Status = OrderStatus.Pending, TotalAmount = 0, Note = "Temizlik ürünleri stoğu azaldı", CreatedAt = now.AddDays(-1) };
        db.Orders.Add(o3); await db.SaveChangesAsync();
        var oi3a = new OrderItem { OrderId = o3.Id, ProductId = p15.Id, Quantity = 6,  UnitPrice = p15.Price };
        var oi3b = new OrderItem { OrderId = o3.Id, ProductId = p16.Id, Quantity = 12, UnitPrice = p16.Price };
        db.OrderItems.AddRange(oi3a, oi3b);
        o3.TotalAmount = oi3a.Quantity * oi3a.UnitPrice + oi3b.Quantity * oi3b.UnitPrice;
        await db.SaveChangesAsync();

        // Sipariş 4: Ayşe → Marmara (Delivered, vadesi geçmiş)
        var o4 = new Order { StoreId = s3.Id, WholesalerId = w3.Id, Status = OrderStatus.Delivered, TotalAmount = 0, DueDate = now.AddDays(-5), CreatedAt = now.AddDays(-20), UpdatedAt = now.AddDays(-18) };
        db.Orders.Add(o4); await db.SaveChangesAsync();
        var oi4a = new OrderItem { OrderId = o4.Id, ProductId = p17.Id, Quantity = 24, UnitPrice = p17.Price };
        var oi4b = new OrderItem { OrderId = o4.Id, ProductId = p18.Id, Quantity = 12, UnitPrice = p18.Price };
        var oi4c = new OrderItem { OrderId = o4.Id, ProductId = p19.Id, Quantity = 6,  UnitPrice = p19.Price };
        db.OrderItems.AddRange(oi4a, oi4b, oi4c);
        o4.TotalAmount = oi4a.Quantity * oi4a.UnitPrice + oi4b.Quantity * oi4b.UnitPrice + oi4c.Quantity * oi4c.UnitPrice;
        await db.SaveChangesAsync();

        // Sipariş 5: Köşe → Karadeniz (Confirmed)
        var o5 = new Order { StoreId = s4.Id, WholesalerId = w1.Id, Status = OrderStatus.Confirmed, TotalAmount = 0, Note = "Ramazan öncesi stok", DueDate = now.AddDays(20), CreatedAt = now.AddDays(-5), UpdatedAt = now.AddDays(-4) };
        db.Orders.Add(o5); await db.SaveChangesAsync();
        var oi5a = new OrderItem { OrderId = o5.Id, ProductId = p3.Id, Quantity = 50, UnitPrice = p3.Price };
        var oi5b = new OrderItem { OrderId = o5.Id, ProductId = p4.Id, Quantity = 24, UnitPrice = p4.Price };
        var oi5c = new OrderItem { OrderId = o5.Id, ProductId = p6.Id, Quantity = 10, UnitPrice = p6.Price };
        db.OrderItems.AddRange(oi5a, oi5b, oi5c);
        o5.TotalAmount = oi5a.Quantity * oi5a.UnitPrice + oi5b.Quantity * oi5b.UnitPrice + oi5c.Quantity * oi5c.UnitPrice;
        await db.SaveChangesAsync();

        // Sipariş 6: Cadde → Anadolu (Rejected)
        var o6 = new Order { StoreId = s5.Id, WholesalerId = w2.Id, Status = OrderStatus.Rejected, TotalAmount = 0, WholesalerNote = "Stok yetersiz, önümüzdeki hafta tekrar dene", CreatedAt = now.AddDays(-7), UpdatedAt = now.AddDays(-6) };
        db.Orders.Add(o6); await db.SaveChangesAsync();
        var oi6a = new OrderItem { OrderId = o6.Id, ProductId = p9.Id,  Quantity = 120, UnitPrice = p9.Price };
        var oi6b = new OrderItem { OrderId = o6.Id, ProductId = p10.Id, Quantity = 240, UnitPrice = p10.Price };
        db.OrderItems.AddRange(oi6a, oi6b);
        o6.TotalAmount = oi6a.Quantity * oi6a.UnitPrice + oi6b.Quantity * oi6b.UnitPrice;
        await db.SaveChangesAsync();

        // Sipariş 7: Mahalle → Karadeniz (Pending, yeni)
        var o7 = new Order { StoreId = s6.Id, WholesalerId = w1.Id, Status = OrderStatus.Pending, TotalAmount = 0, Note = "Her zamanki gibi", CreatedAt = now.AddHours(-2) };
        db.Orders.Add(o7); await db.SaveChangesAsync();
        var oi7a = new OrderItem { OrderId = o7.Id, ProductId = p1.Id, Quantity = 24, UnitPrice = p1.Price };
        var oi7b = new OrderItem { OrderId = o7.Id, ProductId = p5.Id, Quantity = 6,  UnitPrice = p5.Price };
        var oi7c = new OrderItem { OrderId = o7.Id, ProductId = p7.Id, Quantity = 20, UnitPrice = p7.Price };
        db.OrderItems.AddRange(oi7a, oi7b, oi7c);
        o7.TotalAmount = oi7a.Quantity * oi7a.UnitPrice + oi7b.Quantity * oi7b.UnitPrice + oi7c.Quantity * oi7c.UnitPrice;
        await db.SaveChangesAsync();

        // ─────────────────────────────────────────────────────────────────────
        // KREDİ İŞLEMLERİ
        // ─────────────────────────────────────────────────────────────────────

        // Ali ↔ Karadeniz
        db.CreditTransactions.AddRange(
            new CreditTransaction { StoreId = s1.Id, WholesalerId = w1.Id, Type = CreditTransactionType.OrderDebit,   Amount = o1.TotalAmount, Description = $"Sipariş borcu — teslimat",      OrderId = o1.Id, DueDate = now.AddDays(30), CreatedAt = now.AddDays(-9) },
            new CreditTransaction { StoreId = s1.Id, WholesalerId = w1.Id, Type = CreditTransactionType.Payment,      Amount = 1000.00m,       Description = "Havale ile kısmi ödeme",                          CreatedAt = now.AddDays(-5) }
        );

        // Ali ↔ Anadolu
        db.CreditTransactions.Add(
            new CreditTransaction { StoreId = s1.Id, WholesalerId = w2.Id, Type = CreditTransactionType.OrderDebit,   Amount = o2.TotalAmount, Description = "Sipariş borcu — onaylandı",       OrderId = o2.Id, DueDate = now.AddDays(15),  CreatedAt = now.AddDays(-2) }
        );

        // Ayşe ↔ Marmara (vadesi geçmiş borç)
        db.CreditTransactions.AddRange(
            new CreditTransaction { StoreId = s3.Id, WholesalerId = w3.Id, Type = CreditTransactionType.OrderDebit,   Amount = o4.TotalAmount, Description = "Sipariş borcu — teslimat",        OrderId = o4.Id, DueDate = now.AddDays(-5),  CreatedAt = now.AddDays(-18) },
            new CreditTransaction { StoreId = s3.Id, WholesalerId = w3.Id, Type = CreditTransactionType.ManualDebit,  Amount = 250.00m,        Description = "Nakliye farkı — ek borç",                         CreatedAt = now.AddDays(-18) }
        );

        // Köşe ↔ Karadeniz
        db.CreditTransactions.Add(
            new CreditTransaction { StoreId = s4.Id, WholesalerId = w1.Id, Type = CreditTransactionType.OrderDebit,   Amount = o5.TotalAmount, Description = "Sipariş borcu — onaylandı",       OrderId = o5.Id, DueDate = now.AddDays(20),  CreatedAt = now.AddDays(-4) }
        );

        await db.SaveChangesAsync();
    }
}
