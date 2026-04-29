class Wholesaler {
  final String id;
  final String companyName;
  final String? phone;
  final String? address;
  final String? description;
  final DateTime assignedAt;

  const Wholesaler({
    required this.id,
    required this.companyName,
    this.phone,
    this.address,
    this.description,
    required this.assignedAt,
  });

  factory Wholesaler.fromJson(Map<String, dynamic> json) => Wholesaler(
        id: json['wholesalerId'].toString(),
        companyName: json['companyName'] as String,
        phone: json['phone'] as String?,
        address: json['address'] as String?,
        description: json['description'] as String?,
        assignedAt: DateTime.parse(json['assignedAt'] as String),
      );

  /// İlk iki harf avatar için
  String get initials {
    final parts = companyName.split(' ');
    if (parts.length >= 2) {
      return '${parts[0][0]}${parts[1][0]}'.toUpperCase();
    }
    return companyName.substring(0, companyName.length.clamp(0, 2)).toUpperCase();
  }
}
