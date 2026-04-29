import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/api/api_endpoints.dart';
import '../../auth/providers/auth_provider.dart';
import '../models/product.dart';

/// Seçilen toptancının tüm ürünleri — client-side filtre + barkod arama
final productsProvider =
    FutureProvider.family<List<Product>, String>((ref, wholesalerId) async {
  final dio = ref.watch(dioProvider);
  final response = await dio.get(
    ApiEndpoints.products,
    queryParameters: {
      'wholesalerId': wholesalerId,
      'includeInactive': false,
    },
  );
  final list = response.data as List<dynamic>;
  return list
      .map((e) => Product.fromJson(e as Map<String, dynamic>))
      .toList();
});

/// Client-side arama + kategori filtresi
final filteredProductsProvider = Provider.family<
    AsyncValue<List<Product>>,
    ({String wholesalerId, String query, String? categoryId})>((ref, params) {
  final productsAsync = ref.watch(productsProvider(params.wholesalerId));
  return productsAsync.whenData((products) {
    var result = products;

    if (params.categoryId != null && params.categoryId!.isNotEmpty) {
      result = result
          .where((p) => p.categoryId == params.categoryId)
          .toList();
    }

    final q = params.query.trim().toLowerCase();
    if (q.isNotEmpty) {
      result = result.where((p) {
        return p.name.toLowerCase().contains(q) ||
            (p.brand?.toLowerCase().contains(q) ?? false) ||
            p.categoryName.toLowerCase().contains(q);
      }).toList();
    }

    return result;
  });
});

/// Barkod ile ürün bulma (client-side)
Product? findByBarcode(List<Product> products, String barcode) {
  for (final product in products) {
    for (final config in product.unitConfigs) {
      if (config.barcodes.any((b) => b.barcode == barcode)) {
        return product;
      }
    }
  }
  return null;
}

/// Ürünlerdeki kategorileri çıkar
final categoriesProvider =
    Provider.family<AsyncValue<List<({String id, String name})>>, String>(
        (ref, wholesalerId) {
  final productsAsync = ref.watch(productsProvider(wholesalerId));
  return productsAsync.whenData((products) {
    final seen = <String>{};
    final cats = <({String id, String name})>[];
    for (final p in products) {
      if (seen.add(p.categoryId)) {
        cats.add((id: p.categoryId, name: p.categoryName));
      }
    }
    cats.sort((a, b) => a.name.compareTo(b.name));
    return cats;
  });
});
