import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/api/api_endpoints.dart';
import '../../auth/providers/auth_provider.dart';
import '../models/order.dart';

final ordersProvider = FutureProvider<List<Order>>((ref) async {
  final dio = ref.watch(dioProvider);
  final response = await dio.get(ApiEndpoints.orders);
  final list = response.data as List<dynamic>;
  return list
      .map((e) => Order.fromJson(e as Map<String, dynamic>))
      .toList()
    ..sort((a, b) => b.createdAt.compareTo(a.createdAt));
});

final orderDetailProvider =
    FutureProvider.family<Order, String>((ref, orderId) async {
  final dio = ref.watch(dioProvider);
  final response = await dio.get(ApiEndpoints.orderById(orderId));
  return Order.fromJson(response.data as Map<String, dynamic>);
});
