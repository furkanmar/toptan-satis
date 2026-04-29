import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/api/api_endpoints.dart';
import '../../auth/providers/auth_provider.dart';
import '../models/credit_summary.dart';

final creditSummaryProvider = FutureProvider<CreditSummary>((ref) async {
  final dio = ref.watch(dioProvider);
  final response = await dio.get(ApiEndpoints.creditSummary);
  return CreditSummary.fromJson(response.data as Map<String, dynamic>);
});

/// Tarih aralığı ile ekstre
final creditStatementProvider =
    FutureProvider.family<CreditStatement, ({DateTime from, DateTime to})>(
        (ref, params) async {
  final dio = ref.watch(dioProvider);
  final response = await dio.get(
    ApiEndpoints.creditStatement,
    queryParameters: {
      'from': params.from.toIso8601String().substring(0, 10),
      'to': params.to.toIso8601String().substring(0, 10),
    },
  );
  return CreditStatement.fromJson(response.data as Map<String, dynamic>);
});
