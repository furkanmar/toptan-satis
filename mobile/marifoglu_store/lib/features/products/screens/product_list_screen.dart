import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/widgets/error_view.dart';
import '../../../core/widgets/shimmer_loading.dart';
import '../../cart/providers/cart_provider.dart';
import '../providers/product_provider.dart';
import '../widgets/product_card.dart';

class ProductListScreen extends ConsumerStatefulWidget {
  final String wholesalerId;
  final String wholesalerName;

  const ProductListScreen({
    super.key,
    required this.wholesalerId,
    required this.wholesalerName,
  });

  @override
  ConsumerState<ProductListScreen> createState() => _ProductListScreenState();
}

class _ProductListScreenState extends ConsumerState<ProductListScreen> {
  final _searchCtrl = TextEditingController();
  String _query = '';
  String? _selectedCategoryId;

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final params = (
      wholesalerId: widget.wholesalerId,
      query: _query,
      categoryId: _selectedCategoryId,
    );
    final filteredAsync = ref.watch(filteredProductsProvider(params));
    final categoriesAsync = ref.watch(categoriesProvider(widget.wholesalerId));
    final cartCount = ref.watch(
      cartProvider(widget.wholesalerId).select((c) => c.items.length),
    );

    return Scaffold(
      appBar: AppBar(
        title: Text(widget.wholesalerName),
        actions: [
          Stack(
            children: [
              IconButton(
                icon: const Icon(Icons.shopping_cart_outlined),
                onPressed: () => context.goNamed(
                  'cart',
                  pathParameters: {'wholesalerId': widget.wholesalerId},
                  queryParameters: {'name': widget.wholesalerName},
                ),
              ),
              if (cartCount > 0)
                Positioned(
                  right: 6,
                  top: 6,
                  child: CircleAvatar(
                    radius: 8,
                    backgroundColor: Theme.of(context).colorScheme.error,
                    child: Text(
                      '$cartCount',
                      style:
                          const TextStyle(fontSize: 10, color: Colors.white),
                    ),
                  ),
                ),
            ],
          ),
          PopupMenuButton(
            icon: const Icon(Icons.more_vert),
            itemBuilder: (_) => [
              PopupMenuItem(
                child: const Text('Siparişlerim'),
                onTap: () => context.pushNamed('orders'),
              ),
              PopupMenuItem(
                child: const Text('Cari Hesap'),
                onTap: () => context.pushNamed('credit'),
              ),
              PopupMenuItem(
                child: const Text('Profil'),
                onTap: () => context.pushNamed('profile'),
              ),
            ],
          ),
        ],
      ),
      body: Column(
        children: [
          // Arama çubuğu
          Padding(
            padding: const EdgeInsets.fromLTRB(12, 12, 12, 6),
            child: SearchBar(
              controller: _searchCtrl,
              hintText: 'Ürün, marka ara...',
              leading: const Icon(Icons.search),
              trailing: [
                if (_query.isNotEmpty)
                  IconButton(
                    icon: const Icon(Icons.clear),
                    onPressed: () {
                      _searchCtrl.clear();
                      setState(() => _query = '');
                    },
                  ),
              ],
              onChanged: (v) => setState(() => _query = v),
            ),
          ),

          // Kategori chip'leri
          categoriesAsync.when(
            loading: () => const SizedBox.shrink(),
            error: (_, __) => const SizedBox.shrink(),
            data: (cats) => cats.isEmpty
                ? const SizedBox.shrink()
                : SizedBox(
                    height: 40,
                    child: ListView(
                      scrollDirection: Axis.horizontal,
                      padding: const EdgeInsets.symmetric(horizontal: 12),
                      children: [
                        Padding(
                          padding: const EdgeInsets.only(right: 6),
                          child: FilterChip(
                            label: const Text('Tümü'),
                            selected: _selectedCategoryId == null,
                            onSelected: (_) =>
                                setState(() => _selectedCategoryId = null),
                          ),
                        ),
                        ...cats.map((cat) => Padding(
                              padding: const EdgeInsets.only(right: 6),
                              child: FilterChip(
                                label: Text(cat.name),
                                selected: _selectedCategoryId == cat.id,
                                onSelected: (_) => setState(
                                  () => _selectedCategoryId =
                                      _selectedCategoryId == cat.id
                                          ? null
                                          : cat.id,
                                ),
                              ),
                            )),
                      ],
                    ),
                  ),
          ),
          const SizedBox(height: 4),

          // Ürün listesi
          Expanded(
            child: filteredAsync.when(
              loading: () => const ProductListShimmer(),
              error: (e, _) => ErrorView(
                message: e.toString(),
                onRetry: () =>
                    ref.invalidate(productsProvider(widget.wholesalerId)),
              ),
              data: (products) {
                if (products.isEmpty) {
                  return const Center(
                    child: Text('Ürün bulunamadı'),
                  );
                }
                return RefreshIndicator(
                  onRefresh: () async =>
                      ref.invalidate(productsProvider(widget.wholesalerId)),
                  child: ListView.builder(
                    padding: const EdgeInsets.symmetric(
                        horizontal: 12, vertical: 4),
                    itemCount: products.length,
                    itemBuilder: (_, i) => ProductCard(
                      product: products[i],
                      wholesalerId: widget.wholesalerId,
                      ref: ref,
                    ),
                  ),
                );
              },
            ),
          ),
        ],
      ),

      // Barkod FAB
      floatingActionButton: FloatingActionButton(
        onPressed: () => context.goNamed(
          'barcode',
          pathParameters: {'wholesalerId': widget.wholesalerId},
        ),
        tooltip: 'Barkod Tara',
        child: const Icon(Icons.qr_code_scanner),
      ),
    );
  }
}
