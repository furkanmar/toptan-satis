import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../cart/providers/cart_provider.dart';
import '../models/product.dart';

void showAddToCartModal(
  BuildContext context,
  WidgetRef ref,
  Product product,
  String wholesalerId,
) {
  showModalBottomSheet(
    context: context,
    isScrollControlled: true,
    shape: const RoundedRectangleBorder(
      borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
    ),
    builder: (_) => _AddToCartSheet(
      product: product,
      wholesalerId: wholesalerId,
      ref: ref,
    ),
  );
}

class _AddToCartSheet extends StatefulWidget {
  final Product product;
  final String wholesalerId;
  final WidgetRef ref;

  const _AddToCartSheet({
    required this.product,
    required this.wholesalerId,
    required this.ref,
  });

  @override
  State<_AddToCartSheet> createState() => _AddToCartSheetState();
}

class _AddToCartSheetState extends State<_AddToCartSheet> {
  late ProductUnitConfig _selectedConfig;
  int _quantity = 1;

  @override
  void initState() {
    super.initState();
    _selectedConfig = widget.product.defaultUnitConfig ??
        widget.product.unitConfigs.first;
    _quantity = widget.product.minOrderQty;
  }

  double get _total => _selectedConfig.price * _quantity;

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final product = widget.product;

    return Padding(
      padding: EdgeInsets.only(
        left: 16,
        right: 16,
        top: 20,
        bottom: MediaQuery.of(context).viewInsets.bottom + 20,
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          // Handle
          Center(
            child: Container(
              width: 40,
              height: 4,
              margin: const EdgeInsets.only(bottom: 16),
              decoration: BoxDecoration(
                color: cs.outlineVariant,
                borderRadius: BorderRadius.circular(2),
              ),
            ),
          ),

          Text(
            product.name,
            style: Theme.of(context)
                .textTheme
                .titleLarge
                ?.copyWith(fontWeight: FontWeight.bold),
          ),
          if (product.brand != null)
            Text(product.brand!,
                style: TextStyle(color: cs.onSurfaceVariant)),
          const SizedBox(height: 16),

          // Birim seçimi
          if (product.unitConfigs.length > 1) ...[
            Text('Birim Tipi',
                style: Theme.of(context).textTheme.labelLarge),
            const SizedBox(height: 8),
            Wrap(
              spacing: 8,
              children: product.unitConfigs.map((config) {
                final selected = config.id == _selectedConfig.id;
                return ChoiceChip(
                  label: Text(
                      '${config.label} - ${config.price.toStringAsFixed(2)}₺'),
                  selected: selected,
                  onSelected: (_) =>
                      setState(() => _selectedConfig = config),
                );
              }).toList(),
            ),
            const SizedBox(height: 16),
          ],

          // Miktar
          Row(
            children: [
              Text('Adet', style: Theme.of(context).textTheme.labelLarge),
              const Spacer(),
              IconButton(
                onPressed: _quantity > product.minOrderQty
                    ? () => setState(() => _quantity--)
                    : null,
                icon: const Icon(Icons.remove_circle_outline),
              ),
              SizedBox(
                width: 48,
                child: Text(
                  '$_quantity',
                  textAlign: TextAlign.center,
                  style: Theme.of(context).textTheme.titleMedium,
                ),
              ),
              IconButton(
                onPressed: () => setState(() => _quantity++),
                icon: const Icon(Icons.add_circle_outline),
              ),
            ],
          ),

          // KDV + toplam
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Chip(
                label:
                    Text('KDV %${product.vatRate}', style: const TextStyle(fontSize: 12)),
                padding: EdgeInsets.zero,
              ),
              Text(
                'Toplam: ${_total.toStringAsFixed(2)} ₺',
                style: Theme.of(context)
                    .textTheme
                    .titleMedium
                    ?.copyWith(fontWeight: FontWeight.bold),
              ),
            ],
          ),
          const SizedBox(height: 16),

          FilledButton.icon(
            onPressed: () {
              widget.ref.read(cartProvider(widget.wholesalerId).notifier).add(
                    product: product,
                    unitConfig: _selectedConfig,
                    quantity: _quantity,
                  );
              Navigator.of(context).pop();
              ScaffoldMessenger.of(context).showSnackBar(
                SnackBar(
                  content: Text('${product.name} sepete eklendi'),
                  duration: const Duration(seconds: 2),
                ),
              );
            },
            icon: const Icon(Icons.shopping_cart_outlined),
            label: const Text('Sepete Ekle'),
          ),
        ],
      ),
    );
  }
}
