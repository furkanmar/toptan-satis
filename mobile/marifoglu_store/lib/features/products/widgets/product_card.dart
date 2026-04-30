import 'package:flutter/material.dart';

import '../models/product.dart';
import 'add_to_cart_modal.dart';

class ProductCard extends StatelessWidget {
  final Product product;
  final String wholesalerId;

  const ProductCard({
    super.key,
    required this.product,
    required this.wholesalerId,
  });

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final imageUrl = product.mainImageUrl;

    return Card(
      child: InkWell(
        borderRadius: BorderRadius.circular(12),
        onTap: () => showAddToCartModal(context, product, wholesalerId),
        child: Padding(
          padding: const EdgeInsets.all(10),
          child: Row(
            children: [
              // Ürün görseli
              ClipRRect(
                borderRadius: BorderRadius.circular(8),
                child: imageUrl != null
                    ? Image.network(
                        imageUrl,
                        width: 70,
                        height: 70,
                        fit: BoxFit.cover,
                        errorBuilder: (_, __, ___) => _placeholder(cs),
                      )
                    : _placeholder(cs),
              ),
              const SizedBox(width: 12),

              // Bilgiler
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      product.name,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(fontWeight: FontWeight.w600),
                    ),
                    if (product.brand != null)
                      Text(
                        product.brand!,
                        style: Theme.of(context)
                            .textTheme
                            .bodySmall
                            ?.copyWith(color: cs.onSurfaceVariant),
                      ),
                    const SizedBox(height: 4),
                    Row(
                      children: [
                        Text(
                          '${product.displayPrice.toStringAsFixed(2)} ₺',
                          style: TextStyle(
                            color: cs.primary,
                            fontWeight: FontWeight.bold,
                            fontSize: 15,
                          ),
                        ),
                        const SizedBox(width: 6),
                        Container(
                          padding: const EdgeInsets.symmetric(
                              horizontal: 5, vertical: 2),
                          decoration: BoxDecoration(
                            color: cs.secondaryContainer,
                            borderRadius: BorderRadius.circular(4),
                          ),
                          child: Text(
                            'KDV %${product.vatRate}',
                            style: TextStyle(
                                fontSize: 10, color: cs.onSecondaryContainer),
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),

              // Sepete ekle
              IconButton(
                icon: const Icon(Icons.add_shopping_cart),
                onPressed: () =>
                    showAddToCartModal(context, product, wholesalerId),
                tooltip: 'Sepete ekle',
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _placeholder(ColorScheme cs) => Container(
        width: 70,
        height: 70,
        color: cs.surfaceContainerHighest,
        child: Icon(Icons.image_not_supported_outlined,
            color: cs.onSurfaceVariant),
      );
}
