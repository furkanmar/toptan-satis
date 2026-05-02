import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/api/api_endpoints.dart';
import '../../auth/providers/auth_provider.dart';
import '../models/credit_summary.dart';

/// Mağazanın belirli toptancıdaki cari özeti
/// GET /credit/wholesaler/{wholesalerId}
final creditSummaryProvider =
    FutureProvider.family<CreditSummary, String>((ref, wholesalerId) async {
  final dio = ref.watch(dioProvider);
  final response =
      await dio.get(ApiEndpoints.creditByWholesaler(wholesalerId));
  return CreditSummary.fromJson(response.data as Map<String, dynamic>);
});

/// Ekstre — storeId + wholesalerId + tarih aralığı
/// GET /credit/statement?storeId=&wholesalerId=&from=&to=
final creditStatementProvider = FutureProvider.family<CreditStatement,
    ({String storeId, String wholesalerId, DateTime from, DateTime to})>(
  (ref, params) async {
    final dio = ref.watch(dioProvider);
    final response = await dio.get(
      ApiEndpoints.creditStatement,
      queryParameters: {
        'storeId': params.storeId,
        'wholesalerId': params.wholesalerId,
        'from': params.from.toIso8601String().substring(0, 10),
        'to': params.to.toIso8601String().substring(0, 10),
      },
    );
    return CreditStatement.fromJson(response.data as Map<String, dynamic>);
  },
);
