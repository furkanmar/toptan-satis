class ProductBarcode {
  final String id;
  final String barcode;
  final String? note;

  const ProductBarcode({required this.id, required this.barcode, this.note});

  factory ProductBarcode.fromJson(Map<String, dynamic> json) => ProductBarcode(
        id: json['id'].toString(),
        barcode: json['barcode'] as String,
        note: json['note'] as String?,
      );
}

class ProductUnitConfig {
  final String id;
  final String unitType;
  final int contentQty;
  final double price;
  final int sortOrder;
  final bool isActive;
  final List<ProductBarcode> barcodes;

  const ProductUnitConfig({
    required this.id,
    required this.unitType,
    required this.contentQty,
    required this.price,
    required this.sortOrder,
    required this.isActive,
    required this.barcodes,
  });

  factory ProductUnitConfig.fromJson(Map<String, dynamic> json) =>
      ProductUnitConfig(
        id: json['id'].toString(),
        unitType: json['unitType'] as String,
        contentQty: json['contentQty'] as int,
        price: (json['price'] as num).toDouble(),
        sortOrder: json['sortOrder'] as int,
        isActive: json['isActive'] as bool,
        barcodes: (json['barcodes'] as List<dynamic>)
            .map((e) => ProductBarcode.fromJson(e as Map<String, dynamic>))
            .toList(),
      );

  String get label =>
      contentQty > 1 ? '$unitType ($contentQty adet)' : unitType;
}

class ProductImage {
  final String id;
  final String url;
  final bool isMain;

  const ProductImage({required this.id, required this.url, required this.isMain});

  factory ProductImage.fromJson(Map<String, dynamic> json) => ProductImage(
        id: json['id'].toString(),
        url: json['url'] as String,
        isMain: json['isMain'] as bool,
      );
}

class Product {
  final String id;
  final String wholesalerId;
  final String wholesalerName;
  final String categoryId;
  final String categoryName;
  final String name;
  final String? description;
  final String? brand;
  final double price;
  final int minOrderQty;
  final int stock;
  final bool isActive;
  final int vatRate;
  final List<ProductImage> images;
  final List<ProductUnitConfig> unitConfigs;

  const Product({
    required this.id,
    required this.wholesalerId,
    required this.wholesalerName,
    required this.categoryId,
    required this.categoryName,
    required this.name,
    this.description,
    this.brand,
    required this.price,
    required this.minOrderQty,
    required this.stock,
    required this.isActive,
    required this.vatRate,
    required this.images,
    required this.unitConfigs,
  });

  factory Product.fromJson(Map<String, dynamic> json) => Product(
        id: json['id'].toString(),
        wholesalerId: json['wholesalerId'].toString(),
        wholesalerName: json['wholesalerName'] as String,
        categoryId: json['categoryId'].toString(),
        categoryName: json['categoryName'] as String,
        name: json['name'] as String,
        description: json['description'] as String?,
        brand: json['brand'] as String?,
        price: (json['price'] as num).toDouble(),
        minOrderQty: json['minOrderQty'] as int,
        stock: json['stock'] as int,
        isActive: json['isActive'] as bool,
        vatRate: json['vatRate'] as int,
        images: (json['images'] as List<dynamic>)
            .map((e) => ProductImage.fromJson(e as Map<String, dynamic>))
            .toList(),
        unitConfigs: (json['unitConfigs'] as List<dynamic>)
            .map((e) => ProductUnitConfig.fromJson(e as Map<String, dynamic>))
            .where((u) => u.isActive)
            .toList()
          ..sort((a, b) => a.sortOrder.compareTo(b.sortOrder)),
      );

  String? get mainImageUrl {
    if (images.isEmpty) return null;
    return images.firstWhere((i) => i.isMain, orElse: () => images.first).url;
  }

  /// Varsayılan/ilk aktif unit config fiyatı
  double get displayPrice =>
      unitConfigs.isNotEmpty ? unitConfigs.first.price : price;

  ProductUnitConfig? get defaultUnitConfig =>
      unitConfigs.isNotEmpty ? unitConfigs.first : null;
}
