import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:uuid/uuid.dart';

import '../../../core/api/api_endpoints.dart';
import '../../../core/errors/app_exception.dart';
import '../../auth/providers/auth_provider.dart';
import '../providers/cart_provider.dart';
import '../models/cart_item.dart';

class CartScreen extends ConsumerStatefulWidget {
  final String wholesalerId;
  final String wholesalerName;

  const CartScreen({
    super.key,
    required this.wholesalerId,
    required this.wholesalerName,
  });

  @override
  ConsumerState<CartScreen> createState() => _CartScreenState();
}

class _CartScreenState extends ConsumerState<CartScreen> {
  final _noteCtrl = TextEditingController();
  bool _isSubmitting = false;

  @override
  void dispose() {
    _noteCtrl.dispose();
    super.dispose();
  }

  Future<void> _placeOrder() async {
    final cart = ref.read(cartProvider(widget.wholesalerId));
    if (cart.items.isEmpty) return;

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (_) => AlertDialog(
        title: const Text('Siparişi Onayla'),
        content: Text(
          '${cart.items.length} kalem ürün, '
          'toplam ${cart.subtotal.toStringAsFixed(2)} ₺\n\n'
          'Siparişi vermek istediğinize emin misiniz?',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('İptal'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Onayla'),
          ),
        ],
      ),
    );

    if (confirmed != true) return;

    setState(() => _isSubmitting = true);
    try {
      final dio = ref.read(dioProvider);
      await dio.post(
        ApiEndpoints.orders,
        data: {
          'wholesalerId': widget.wholesalerId,
          'note': _noteCtrl.text.trim().isEmpty ? null : _noteCtrl.text.trim(),
          'items': cart.items
              .map((item) => {
                    'productId': item.productId,
                    'unitConfigId': item.unitConfigId,
                    'quantity': item.quantity,
                  })
              .toList(),
        },
        // Idempotency key interceptor tarafından otomatik eklenir
      );

      await ref.read(cartProvider(widget.wholesalerId).notifier).clear();

      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Sipariş başarıyla oluşturuldu!'),
          backgroundColor: Colors.green,
          duration: Duration(seconds: 3),
        ),
      );
      context.goNamed('orders');
    } on DioException catch (e) {
      final msg = (e.error as AppException?)?.message ?? 'Sipariş gönderilemedi';
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(msg), backgroundColor: Colors.red),
      );
    } finally {
      if (mounted) setState(() => _isSubmitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final cart = ref.watch(cartProvider(widget.wholesalerId));
    final cs = Theme.of(context).colorScheme;

    return Scaffold(
      appBar: AppBar(
        title: Text('Sepet — ${widget.wholesalerName}'),
        actions: [
          if (cart.items.isNotEmpty)
            TextButton(
              onPressed: () => showDialog(
                context: context,
                builder: (_) => AlertDialog(
                  title: const Text('Sepeti Temizle'),
                  content:
                      const Text('Tüm ürünler sepetten kaldırılsın mı?'),
                  actions: [
                    TextButton(
                      onPressed: () => Navigator.pop(context),
                      child: const Text('İptal'),
                    ),
                    FilledButton(
                      onPressed: () {
                        Navigator.pop(context);
                        ref
                            .read(cartProvider(widget.wholesalerId).notifier)
                            .clear();
                      },
                      child: const Text('Temizle'),
                    ),
                  ],
                ),
              ),
              child: const Text('Temizle'),
            ),
        ],
      ),
      body: cart.items.isEmpty
          ? const Center(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Icon(Icons.shopping_cart_outlined, size: 64),
                  SizedBox(height: 12),
                  Text('Sepetiniz boş'),
                ],
              ),
            )
          : Column(
              children: [
                Expanded(
                  child: ListView.builder(
                    padding: const EdgeInsets.all(12),
                    itemCount: cart.items.length,
                    itemBuilder: (_, i) =>
                        _CartItemTile(item: cart.items[i], wholesalerId: widget.wholesalerId),
                  ),
                ),

                // Sipariş notu
                Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 12),
                  child: TextField(
                    controller: _noteCtrl,
                    decoration: const InputDecoration(
                      labelText: 'Sipariş notu (opsiyonel)',
                      prefixIcon: Icon(Icons.note_outlined),
                    ),
                    maxLines: 2,
                  ),
                ),
                const SizedBox(height: 8),

                // Özet + sipariş butonu
                Container(
                  padding: const EdgeInsets.all(16),
                  decoration: BoxDecoration(
                    color: cs.surfaceContainerHighest,
                    borderRadius: const BorderRadius.vertical(
                        top: Radius.circular(16)),
                  ),
                  child: Column(
                    children: [
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          const Text('Toplam (KDV dahil):'),
                          Text(
                            '${cart.subtotal.toStringAsFixed(2)} ₺',
                            style: TextStyle(
                              fontSize: 18,
                              fontWeight: FontWeight.bold,
                              color: cs.primary,
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 12),
                      FilledButton.icon(
                        onPressed: _isSubmitting ? null : _placeOrder,
                        icon: _isSubmitting
                            ? const SizedBox(
                                width: 18,
                                height: 18,
                                child:
                                    CircularProgressIndicator(strokeWidth: 2),
                              )
                            : const Icon(Icons.send),
                        label: Text(
                          _isSubmitting ? 'Gönderiliyor...' : 'Sipariş Ver',
                          style: const TextStyle(fontSize: 16),
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
    );
  }
}

class _CartItemTile extends ConsumerWidget {
  final CartItem item;
  final String wholesalerId;

  const _CartItemTile({
    required this.item,
    required this.wholesalerId,
  });

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final cs = Theme.of(context).colorScheme;

    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: Padding(
        padding: const EdgeInsets.all(10),
        child: Row(
          children: [
            // Görsel
            ClipRRect(
              borderRadius: BorderRadius.circular(8),
              child: item.productImageUrl != null
                  ? Image.network(
                      item.productImageUrl!,
                      width: 56,
                      height: 56,
                      fit: BoxFit.cover,
                      errorBuilder: (_, __, ___) => Container(
                        width: 56,
                        height: 56,
                        color: cs.surfaceContainerHighest,
                        child: const Icon(Icons.image_not_supported_outlined),
                      ),
                    )
                  : Container(
                      width: 56,
                      height: 56,
                      color: cs.surfaceContainerHighest,
                      child: const Icon(Icons.inventory_2_outlined),
                    ),
            ),
            const SizedBox(width: 10),

            // Ad + birim
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(item.productName,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                      style:
                          const TextStyle(fontWeight: FontWeight.w600)),
                  Text(
                    '${item.unitType} × ${item.unitPrice.toStringAsFixed(2)} ₺',
                    style: TextStyle(
                        fontSize: 12, color: cs.onSurfaceVariant),
                  ),
                ],
              ),
            ),

            // Miktar kontrolü
            Row(
              children: [
                IconButton(
                  icon: const Icon(Icons.remove_circle_outline, size: 20),
                  onPressed: () {
                    if (item.quantity > 1) {
                      ref
                          .read(cartProvider(wholesalerId).notifier)
                          .updateQuantity(
                              item.productId, item.unitConfigId, item.quantity - 1);
                    } else {
                      ref
                          .read(cartProvider(wholesalerId).notifier)
                          .remove(item.productId, item.unitConfigId);
                    }
                  },
                ),
                Text('${item.quantity}',
                    style: const TextStyle(fontWeight: FontWeight.bold)),
                IconButton(
                  icon: const Icon(Icons.add_circle_outline, size: 20),
                  onPressed: () => ref
                      .read(cartProvider(wholesalerId).notifier)
                      .updateQuantity(
                          item.productId, item.unitConfigId, item.quantity + 1),
                ),
              ],
            ),

            // Toplam fiyat
            SizedBox(
              width: 70,
              child: Text(
                '${item.totalPrice.toStringAsFixed(2)} ₺',
                textAlign: TextAlign.end,
                style: TextStyle(
                    fontWeight: FontWeight.bold, color: cs.primary),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
