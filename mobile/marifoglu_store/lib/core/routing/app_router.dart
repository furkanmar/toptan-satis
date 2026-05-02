import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/auth/providers/auth_provider.dart';
import '../../features/auth/screens/login_screen.dart';
import '../../features/wholesalers/screens/wholesaler_selection_screen.dart';
import '../../features/products/screens/product_list_screen.dart';
import '../../features/products/screens/barcode_scanner_screen.dart';
import '../../features/cart/screens/cart_screen.dart';
import '../../features/orders/screens/order_history_screen.dart';
import '../../features/orders/screens/order_detail_screen.dart';
import '../../features/orders/screens/incoming_orders_screen.dart';
import '../../features/credit/screens/credit_screen.dart';
import '../../features/profile/screens/profile_screen.dart';

final routerProvider = Provider<GoRouter>((ref) {
  final authState = ref.watch(authProvider);

  return GoRouter(
    initialLocation: '/login',
    redirect: (context, state) {
      final isLoggedIn = authState.isAuthenticated;
      final isLoginRoute = state.matchedLocation == '/login';

      if (!isLoggedIn && !isLoginRoute) return '/login';
      if (isLoggedIn && isLoginRoute) {
        return authState.isWholesaler ? '/wholesaler/dashboard' : '/wholesalers';
      }
      return null;
    },
    routes: [
      GoRoute(
        path: '/login',
        name: 'login',
        builder: (_, __) => const LoginScreen(),
      ),

      // ── Mağaza (Store) rotaları ──────────────────────────────────────────
      GoRoute(
        path: '/wholesalers',
        name: 'wholesalers',
        builder: (_, __) => const WholesalerSelectionScreen(),
      ),
      GoRoute(
        path: '/products/:wholesalerId',
        name: 'products',
        builder: (_, state) => ProductListScreen(
          wholesalerId: state.pathParameters['wholesalerId']!,
          wholesalerName: state.uri.queryParameters['name'] ?? '',
        ),
        routes: [
          GoRoute(
            path: 'barcode',
            name: 'barcode',
            builder: (_, state) => BarcodeScannerScreen(
              wholesalerId: state.pathParameters['wholesalerId']!,
            ),
          ),
          GoRoute(
            path: 'cart',
            name: 'cart',
            builder: (_, state) => CartScreen(
              wholesalerId: state.pathParameters['wholesalerId']!,
              wholesalerName: state.uri.queryParameters['name'] ?? '',
            ),
          ),
        ],
      ),
      GoRoute(
        path: '/orders',
        name: 'orders',
        builder: (_, __) => const OrderHistoryScreen(),
        routes: [
          GoRoute(
            path: ':orderId',
            name: 'order-detail',
            builder: (_, state) => OrderDetailScreen(
              orderId: state.pathParameters['orderId']!,
            ),
          ),
        ],
      ),
      GoRoute(
        path: '/credit',
        name: 'credit',
        builder: (_, state) => CreditScreen(
          wholesalerId: state.uri.queryParameters['wholesalerId'] ?? '',
          storeId: state.uri.queryParameters['storeId'] ?? '',
        ),
      ),

      // ── Toptancı (Wholesaler) rotaları ───────────────────────────────────
      GoRoute(
        path: '/wholesaler/dashboard',
        name: 'wholesaler-dashboard',
        builder: (_, __) => const IncomingOrdersScreen(),
        routes: [
          GoRoute(
            path: 'order/:orderId',
            name: 'incoming-order-detail',
            builder: (_, state) => OrderDetailScreen(
              orderId: state.pathParameters['orderId']!,
            ),
          ),
        ],
      ),

      // ── Ortak ────────────────────────────────────────────────────────────
      GoRoute(
        path: '/profile',
        name: 'profile',
        builder: (_, __) => const ProfileScreen(),
      ),
    ],
    errorBuilder: (_, state) => Scaffold(
      body: Center(child: Text('Sayfa bulunamadı: ${state.error}')),
    ),
  );
});
