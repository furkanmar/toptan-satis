import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../../core/widgets/error_view.dart';
import '../../auth/providers/auth_provider.dart';
import '../models/order.dart';
import '../providers/order_provider.dart';

final _dateFmt = DateFormat('dd.MM.yyyy HH:mm', 'tr_TR');

class IncomingOrdersScreen extends ConsumerWidget {
  const IncomingOrdersScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final ordersAsync = ref.watch(incomingOrdersProvider);
    final authState = ref.watch(authProvider);

    return Scaffold(
      appBar: AppBar(
        title: Text(authState.displayName ?? 'Siparişler'),
        actions: [
          IconButton(
            icon: const Icon(Icons.person_outline),
            onPressed: () => context.pushNamed('profile'),
            tooltip: 'Profil',
          ),
        ],
      ),
      body: ordersAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => ErrorView(
          message: e.toString(),
          onRetry: () => ref.invalidate(incomingOrdersProvider),
        ),
        data: (orders) {
          if (orders.isEmpty) {
            return const Center(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Icon(Icons.inbox_outlined, size: 64, color: Colors.grey),
                  SizedBox(height: 12),
                  Text('Bekleyen sipariş yok'),
                ],
              ),
            );
          }
          return RefreshIndicator(
            onRefresh: () async => ref.invalidate(incomingOrdersProvider),
            child: ListView.builder(
              padding: const EdgeInsets.all(12),
              itemCount: orders.length,
              itemBuilder: (_, i) => _IncomingOrderTile(order: orders[i]),
            ),
          );
        },
      ),
    );
  }
}

class _IncomingOrderTile extends StatelessWidget {
  final Order order;
  const _IncomingOrderTile({required this.order});

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;

    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: InkWell(
        borderRadius: BorderRadius.circular(12),
        onTap: () => context.pushNamed(
          'incoming-order-detail',
          pathParameters: {'orderId': order.id},
        ),
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Expanded(
                    child: Text(
                      order.storeName,
                      style: const TextStyle(fontWeight: FontWeight.bold),
                    ),
                  ),
                  _StatusBadge(status: order.status),
                ],
              ),
              const SizedBox(height: 6),
              Row(
                children: [
                  Icon(Icons.calendar_today_outlined,
                      size: 13, color: cs.onSurfaceVariant),
                  const SizedBox(width: 4),
                  Text(
                    _dateFmt.format(order.createdAt.toLocal()),
                    style:
                        TextStyle(fontSize: 12, color: cs.onSurfaceVariant),
                  ),
                  const Spacer(),
                  Text(
                    '${order.items.length} kalem',
                    style:
                        TextStyle(fontSize: 12, color: cs.onSurfaceVariant),
                  ),
                  const SizedBox(width: 12),
                  Text(
                    '${order.totalAmount.toStringAsFixed(2)} ₺',
                    style: TextStyle(
                        fontWeight: FontWeight.bold, color: cs.primary),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _StatusBadge extends StatelessWidget {
  final String status;
  const _StatusBadge({required this.status});

  @override
  Widget build(BuildContext context) => Container(
        padding:
            const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
        decoration: BoxDecoration(
          color:
              Color(status.statusColor).withValues(alpha: 0.15),
          borderRadius: BorderRadius.circular(20),
          border: Border.all(color: Color(status.statusColor)),
        ),
        child: Text(
          status.statusLabel,
          style: TextStyle(
            fontSize: 12,
            color: Color(status.statusColor),
            fontWeight: FontWeight.w600,
          ),
        ),
      );
}
