class OrderItem {
  final String id;
  final String productId;
  final String productName;
  final int quantity;
  final double unitPrice;
  final double total;
  final String unitType;
  final int contentQty;
  final int vatRate;

  const OrderItem({
    required this.id,
    required this.productId,
    required this.productName,
    required this.quantity,
    required this.unitPrice,
    required this.total,
    required this.unitType,
    required this.contentQty,
    required this.vatRate,
  });

  factory OrderItem.fromJson(Map<String, dynamic> json) => OrderItem(
        id: json['id'].toString(),
        productId: json['productId'].toString(),
        productName: json['productName'] as String,
        quantity: json['quantity'] as int,
        unitPrice: (json['unitPrice'] as num).toDouble(),
        total: (json['total'] as num).toDouble(),
        unitType: json['unitType'] as String? ?? 'Adet',
        contentQty: json['contentQty'] as int? ?? 1,
        vatRate: json['vatRate'] as int? ?? 18,
      );
}

class Order {
  final String id;
  final String storeId;
  final String storeName;
  final String wholesalerId;
  final String wholesalerName;
  final String status;
  final double totalAmount;
  final String? note;
  final String? wholesalerNote;
  final DateTime? dueDate;
  final DateTime createdAt;
  final DateTime? updatedAt;
  final List<OrderItem> items;

  const Order({
    required this.id,
    required this.storeId,
    required this.storeName,
    required this.wholesalerId,
    required this.wholesalerName,
    required this.status,
    required this.totalAmount,
    this.note,
    this.wholesalerNote,
    this.dueDate,
    required this.createdAt,
    this.updatedAt,
    required this.items,
  });

  factory Order.fromJson(Map<String, dynamic> json) => Order(
        id: json['id'].toString(),
        storeId: json['storeId'].toString(),
        storeName: json['storeName'] as String,
        wholesalerId: json['wholesalerId'].toString(),
        wholesalerName: json['wholesalerName'] as String,
        status: json['status'] as String,
        totalAmount: (json['totalAmount'] as num).toDouble(),
        note: json['note'] as String?,
        wholesalerNote: json['wholesalerNote'] as String?,
        dueDate: json['dueDate'] != null
            ? DateTime.parse(json['dueDate'] as String)
            : null,
        createdAt: DateTime.parse(json['createdAt'] as String),
        updatedAt: json['updatedAt'] != null
            ? DateTime.parse(json['updatedAt'] as String)
            : null,
        items: (json['items'] as List<dynamic>)
            .map((e) => OrderItem.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

/// Durum → Türkçe etiket + renk
extension OrderStatusExt on String {
  String get statusLabel => switch (this) {
        'Pending' => 'Beklemede',
        'Confirmed' => 'Onaylandı',
        'Processing' => 'Hazırlanıyor',
        'Shipped' => 'Kargoda',
        'Delivered' => 'Teslim Edildi',
        'Cancelled' => 'İptal',
        _ => this,
      };

  // ignore: prefer_const_constructors
  int get statusColor => switch (this) {
        'Pending' => 0xFFFF9800,       // turuncu
        'Confirmed' => 0xFF2196F3,     // mavi
        'Processing' => 0xFF9C27B0,    // mor
        'Shipped' => 0xFF00BCD4,       // camgöbeği
        'Delivered' => 0xFF4CAF50,     // yeşil
        'Cancelled' => 0xFFF44336,     // kırmızı
        _ => 0xFF9E9E9E,
      };
}
