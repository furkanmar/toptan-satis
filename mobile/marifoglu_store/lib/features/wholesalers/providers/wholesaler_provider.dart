import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/api/api_endpoints.dart';
import '../../../core/api/dio_client.dart';
import '../../auth/providers/auth_provider.dart';
import '../models/wholesaler.dart';

final wholesalersProvider = FutureProvider<List<Wholesaler>>((ref) async {
  final dio = ref.watch(dioProvider);
  final response = await dio.get(ApiEndpoints.myWholesalers);
  final list = response.data as List<dynamic>;
  return list
      .map((e) => Wholesaler.fromJson(e as Map<String, dynamic>))
      .toList();
});
