import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../cart/providers/cart_provider.dart';
import '../models/product.dart';

void showAddToCartModal(
  BuildContext context,
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
    ),
  );
}

class _AddToCartSheet extends ConsumerStatefulWidget {
  final Product product;
  final String wholesalerId;

  const _AddToCartSheet({
    required this.product,
    required this.wholesalerId,
  });

  @override
  ConsumerState<_AddToCartSheet> createState() => _AddToCartSheetState();
}

class _AddToCartSheetState extends ConsumerState<_AddToCartSheet> {
  late ProductUnitConfig _selectedConfig;
  late int _quantity;
  late TextEditingController _qtyCtrl;

  int get _step => _selectedConfig.contentQty.clamp(1, 9999);

  @override
  void initState() {
    super.initState();
    _selectedConfig = widget.product.defaultUnitConfig ??
        widget.product.unitConfigs.first;
    _quantity = _step;
    _qtyCtrl = TextEditingController(text: '$_quantity');
  }

  @override
  void dispose() {
    _qtyCtrl.dispose();
    super.dispose();
  }

  void _selectConfig(ProductUnitConfig config) {
    setState(() {
      _selectedConfig = config;
      _quantity = _step;
      _qtyCtrl.text = '$_quantity';
    });
  }

  void _setQty(int qty) {
    final clamped = qty < _step ? _step : qty;
    setState(() => _quantity = clamped);
    _qtyCtrl.text = '$clamped';
    _qtyCtrl.selection = TextSelection.collapsed(offset: _qtyCtrl.text.length);
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
                  onSelected: (_) => _selectConfig(config),
                );
              }).toList(),
            ),
            const SizedBox(height: 16),
          ],

          // Miktar — adım = seçili config'in contentQty'si
          Row(
            children: [
              Text('Adet', style: Theme.of(context).textTheme.labelLarge),
              if (_step > 1)
                Padding(
                  padding: const EdgeInsets.only(left: 6),
                  child: Text(
                    '($_step\'er)',
                    style: TextStyle(
                        fontSize: 12, color: cs.onSurfaceVariant),
                  ),
                ),
              const Spacer(),
              IconButton(
                onPressed: _quantity > _step
                    ? () => _setQty(_quantity - _step)
                    : null,
                icon: const Icon(Icons.remove_circle_outline),
              ),
              SizedBox(
                width: 56,
                child: TextField(
                  controller: _qtyCtrl,
                  keyboardType: TextInputType.number,
                  textAlign: TextAlign.center,
                  style: Theme.of(context).textTheme.titleMedium,
                  decoration: const InputDecoration(
                    isDense: true,
                    border: OutlineInputBorder(),
                    contentPadding:
                        EdgeInsets.symmetric(vertical: 6, horizontal: 4),
                  ),
                  onSubmitted: (v) {
                    final parsed = int.tryParse(v) ?? _step;
                    // Round up to nearest step
                    final rounded =
                        ((parsed + _step - 1) ~/ _step) * _step;
                    _setQty(rounded.clamp(_step, 99999));
                  },
                ),
              ),
              IconButton(
                onPressed: () => _setQty(_quantity + _step),
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
              ref.read(cartProvider(widget.wholesalerId).notifier).add(
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
