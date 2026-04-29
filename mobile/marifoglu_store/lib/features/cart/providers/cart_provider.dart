import 'dart:convert';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../../../core/auth/secure_token_storage.dart';
import '../../products/models/product.dart';
import '../models/cart_item.dart';

class CartState {
  final List<CartItem> items;
  final bool isLoading;

  const CartState({this.items = const [], this.isLoading = false});

  double get subtotal =>
      items.fold(0, (sum, item) => sum + item.totalPrice);

  int get totalItemCount => items.fold(0, (sum, item) => sum + item.quantity);

  CartState copyWith({List<CartItem>? items, bool? isLoading}) => CartState(
        items: items ?? this.items,
        isLoading: isLoading ?? this.isLoading,
      );
}

/// Her toptancı için ayrı sepet — scoped by wholesalerId
final cartProvider =
    StateNotifierProvider.family<CartNotifier, CartState, String>(
        (ref, wholesalerId) => CartNotifier(wholesalerId));

class CartNotifier extends StateNotifier<CartState> {
  final String _wholesalerId;
  static const _storage = FlutterSecureStorage(
    aOptions: AndroidOptions(encryptedSharedPreferences: true),
  );

  CartNotifier(this._wholesalerId) : super(const CartState()) {
    _load();
  }

  String get _storageKey => SecureTokenStorage.cartKey(_wholesalerId);

  Future<void> _load() async {
    state = state.copyWith(isLoading: true);
    try {
      final raw = await _storage.read(key: _storageKey);
      if (raw != null) {
        final list = (jsonDecode(raw) as List<dynamic>)
            .map((e) => CartItem.fromJson(e as Map<String, dynamic>))
            .toList();
        state = CartState(items: list);
      } else {
        state = const CartState();
      }
    } catch (_) {
      state = const CartState();
    }
  }

  Future<void> _persist() async {
    final json = jsonEncode(state.items.map((i) => i.toJson()).toList());
    await _storage.write(key: _storageKey, value: json);
  }

  void add({
    required Product product,
    required ProductUnitConfig unitConfig,
    required int quantity,
  }) {
    final existing = state.items.indexWhere(
      (i) => i.productId == product.id && i.unitConfigId == unitConfig.id,
    );

    final items = [...state.items];
    if (existing >= 0) {
      items[existing] =
          items[existing].copyWith(quantity: items[existing].quantity + quantity);
    } else {
      items.add(CartItem.fromProduct(
          product: product, config: unitConfig, quantity: quantity));
    }

    state = state.copyWith(items: items);
    _persist();
  }

  void updateQuantity(String productId, String unitConfigId, int quantity) {
    final items = state.items
        .map((i) =>
            i.productId == productId && i.unitConfigId == unitConfigId
                ? i.copyWith(quantity: quantity)
                : i)
        .where((i) => i.quantity > 0)
        .toList();
    state = state.copyWith(items: items);
    _persist();
  }

  void remove(String productId, String unitConfigId) {
    final items = state.items
        .where((i) =>
            !(i.productId == productId && i.unitConfigId == unitConfigId))
        .toList();
    state = state.copyWith(items: items);
    _persist();
  }

  Future<void> clear() async {
    state = const CartState();
    await _storage.delete(key: _storageKey);
  }
}
