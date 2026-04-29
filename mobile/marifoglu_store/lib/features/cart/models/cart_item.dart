import '../../products/models/product.dart';

class CartItem {
  final String productId;
  final String productName;
  final String? productImageUrl;
  final String unitConfigId;
  final String unitType;
  final int contentQty;
  final double unitPrice;
  final int vatRate;
  int quantity;

  CartItem({
    required this.productId,
    required this.productName,
    this.productImageUrl,
    required this.unitConfigId,
    required this.unitType,
    required this.contentQty,
    required this.unitPrice,
    required this.vatRate,
    required this.quantity,
  });

  double get totalPrice => unitPrice * quantity;

  CartItem copyWith({int? quantity}) => CartItem(
        productId: productId,
        productName: productName,
        productImageUrl: productImageUrl,
        unitConfigId: unitConfigId,
        unitType: unitType,
        contentQty: contentQty,
        unitPrice: unitPrice,
        vatRate: vatRate,
        quantity: quantity ?? this.quantity,
      );

  Map<String, dynamic> toJson() => {
        'productId': productId,
        'productName': productName,
        'productImageUrl': productImageUrl,
        'unitConfigId': unitConfigId,
        'unitType': unitType,
        'contentQty': contentQty,
        'unitPrice': unitPrice,
        'vatRate': vatRate,
        'quantity': quantity,
      };

  factory CartItem.fromJson(Map<String, dynamic> json) => CartItem(
        productId: json['productId'] as String,
        productName: json['productName'] as String,
        productImageUrl: json['productImageUrl'] as String?,
        unitConfigId: json['unitConfigId'] as String,
        unitType: json['unitType'] as String,
        contentQty: json['contentQty'] as int,
        unitPrice: (json['unitPrice'] as num).toDouble(),
        vatRate: json['vatRate'] as int,
        quantity: json['quantity'] as int,
      );

  /// Sepete eklenirken Product + UnitConfig'den oluştur
  factory CartItem.fromProduct({
    required Product product,
    required ProductUnitConfig config,
    required int quantity,
  }) =>
      CartItem(
        productId: product.id,
        productName: product.name,
        productImageUrl: product.mainImageUrl,
        unitConfigId: config.id,
        unitType: config.unitType,
        contentQty: config.contentQty,
        unitPrice: config.price,
        vatRate: product.vatRate,
        quantity: quantity,
      );
}
