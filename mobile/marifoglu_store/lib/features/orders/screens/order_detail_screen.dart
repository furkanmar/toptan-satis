import 'dart:io';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import 'package:path_provider/path_provider.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../../core/api/api_endpoints.dart';
import '../../../core/widgets/error_view.dart';
import '../../auth/providers/auth_provider.dart';
import '../models/order.dart';
import '../providers/order_provider.dart';

// ignore_for_file: use_build_context_synchronously

final _dateFmt = DateFormat('dd.MM.yyyy HH:mm', 'tr_TR');
final _shortFmt = DateFormat('dd.MM.yyyy', 'tr_TR');

class OrderDetailScreen extends ConsumerWidget {
  final String orderId;

  const OrderDetailScreen({super.key, required this.orderId});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final orderAsync = ref.watch(orderDetailProvider(orderId));
    final cs = Theme.of(context).colorScheme;

    return Scaffold(
      appBar: AppBar(title: const Text('Sipariş Detayı')),
      body: orderAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => ErrorView(
          message: e.toString(),
          onRetry: () => ref.invalidate(orderDetailProvider(orderId)),
        ),
        data: (order) => SingleChildScrollView(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Durum kartı
              _SectionCard(
                child: Row(
                  children: [
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text('Toptancı',
                              style: Theme.of(context).textTheme.labelSmall),
                          Text(order.wholesalerName,
                              style: const TextStyle(
                                  fontWeight: FontWeight.bold)),
                          const SizedBox(height: 8),
                          Text('Tarih',
                              style: Theme.of(context).textTheme.labelSmall),
                          Text(_dateFmt.format(order.createdAt.toLocal())),
                          if (order.dueDate != null) ...[
                            const SizedBox(height: 8),
                            Text('Vade',
                                style:
                                    Theme.of(context).textTheme.labelSmall),
                            Text(
                              _shortFmt.format(order.dueDate!.toLocal()),
                              style: TextStyle(
                                color: order.dueDate!
                                        .isBefore(DateTime.now())
                                    ? cs.error
                                    : null,
                                fontWeight: FontWeight.w600,
                              ),
                            ),
                          ],
                        ],
                      ),
                    ),
                    Container(
                      padding: const EdgeInsets.symmetric(
                          horizontal: 12, vertical: 6),
                      decoration: BoxDecoration(
                        color: Color(order.status.statusColor)
                            .withValues(alpha: 0.15),
                        borderRadius: BorderRadius.circular(20),
                        border: Border.all(
                            color: Color(order.status.statusColor)),
                      ),
                      child: Text(
                        order.status.statusLabel,
                        style: TextStyle(
                          color: Color(order.status.statusColor),
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ),
                  ],
                ),
              ),

              // Notlar
              if (order.note != null || order.wholesalerNote != null) ...[
                const SizedBox(height: 12),
                _SectionCard(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      if (order.note != null) ...[
                        Text('Sipariş Notu',
                            style: Theme.of(context).textTheme.labelSmall),
                        Text(order.note!),
                        const SizedBox(height: 8),
                      ],
                      if (order.wholesalerNote != null) ...[
                        Text('Toptancı Notu',
                            style: Theme.of(context).textTheme.labelSmall),
                        Text(order.wholesalerNote!),
                      ],
                    ],
                  ),
                ),
              ],

              const SizedBox(height: 12),

              // Ürünler
              Text('Ürünler (${order.items.length})',
                  style: Theme.of(context).textTheme.titleMedium),
              const SizedBox(height: 6),
              ...order.items.map((item) => _OrderItemTile(item: item)),

              const SizedBox(height: 12),

              // Toplam
              _SectionCard(
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    const Text('Toplam Tutar',
                        style: TextStyle(fontWeight: FontWeight.bold)),
                    Text(
                      '${order.totalAmount.toStringAsFixed(2)} ₺',
                      style: TextStyle(
                        fontSize: 18,
                        fontWeight: FontWeight.bold,
                        color: cs.primary,
                      ),
                    ),
                  ],
                ),
              ),

              // İrsaliye PDF (Shipped veya Delivered durumunda göster)
              if (order.status == 'Delivered' || order.status == 'Shipped') ...[
                const SizedBox(height: 12),
                _PdfDownloadButton(orderId: order.id),
              ],

              const SizedBox(height: 24),
            ],
          ),
        ),
      ),
    );
  }
}

/// PDF'i Dio üzerinden (token ile) indirir → geçici dosyaya yazar → açar.
class _PdfDownloadButton extends ConsumerStatefulWidget {
  final String orderId;

  const _PdfDownloadButton({required this.orderId});

  @override
  ConsumerState<_PdfDownloadButton> createState() => _PdfDownloadButtonState();
}

class _PdfDownloadButtonState extends ConsumerState<_PdfDownloadButton> {
  bool _loading = false;

  Future<void> _download() async {
    setState(() => _loading = true);
    try {
      final dio = ref.read(dioProvider);
      final response = await dio.get<List<int>>(
        ApiEndpoints.deliveryNotePdf(widget.orderId),
        options: Options(responseType: ResponseType.bytes),
      );

      final tmpDir = await getTemporaryDirectory();
      final file = File('${tmpDir.path}/irsaliye_${widget.orderId}.pdf');
      await file.writeAsBytes(response.data!);

      final uri = Uri.file(file.path);
      if (await canLaunchUrl(uri)) {
        await launchUrl(uri, mode: LaunchMode.externalApplication);
      } else {
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('PDF açılamadı — PDF görüntüleyici bulunamadı')),
          );
        }
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('PDF indirilemedi: $e')),
        );
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return FilledButton.tonalIcon(
      onPressed: _loading ? null : _download,
      icon: _loading
          ? const SizedBox(
              width: 18, height: 18,
              child: CircularProgressIndicator(strokeWidth: 2),
            )
          : const Icon(Icons.picture_as_pdf),
      label: Text(_loading ? 'İndiriliyor...' : 'İrsaliye PDF İndir'),
    );
  }
}

class _SectionCard extends StatelessWidget {
  final Widget child;

  const _SectionCard({required this.child});

  @override
  Widget build(BuildContext context) => Card(
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: SizedBox(width: double.infinity, child: child),
        ),
      );
}

class _OrderItemTile extends StatelessWidget {
  final OrderItem item;

  const _OrderItemTile({required this.item});

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return Card(
      margin: const EdgeInsets.only(bottom: 6),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
        child: Row(
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(item.productName,
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
            Text('${item.quantity} adet',
                style: const TextStyle(fontWeight: FontWeight.w500)),
            const SizedBox(width: 12),
            Text(
              '${item.total.toStringAsFixed(2)} ₺',
              style: TextStyle(
                  fontWeight: FontWeight.bold, color: cs.primary),
            ),
          ],
        ),
      ),
    );
  }
}
