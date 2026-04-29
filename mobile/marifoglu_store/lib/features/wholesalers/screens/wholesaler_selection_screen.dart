import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../providers/wholesaler_provider.dart';
import '../../../core/widgets/error_view.dart';
import '../../auth/providers/auth_provider.dart';

class WholesalerSelectionScreen extends ConsumerWidget {
  const WholesalerSelectionScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final wholesalersAsync = ref.watch(wholesalersProvider);
    final authState = ref.watch(authProvider);
    final cs = Theme.of(context).colorScheme;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Toptancı Seç'),
        actions: [
          IconButton(
            icon: const Icon(Icons.person_outline),
            onPressed: () => context.pushNamed('profile'),
            tooltip: 'Profil',
          ),
        ],
      ),
      body: Column(
        children: [
          // Karşılama bandı
          Container(
            width: double.infinity,
            color: cs.primaryContainer,
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            child: Text(
              'Hoş geldin, ${authState.displayName ?? ''}',
              style: TextStyle(
                color: cs.onPrimaryContainer,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),

          Expanded(
            child: wholesalersAsync.when(
              loading: () => const Center(child: CircularProgressIndicator()),
              error: (e, _) => ErrorView(
                message: e.toString(),
                onRetry: () => ref.invalidate(wholesalersProvider),
              ),
              data: (wholesalers) {
                if (wholesalers.isEmpty) {
                  return const Center(
                    child: Text(
                      'Henüz bir toptancıya bağlı değilsiniz.\nYöneticinizle iletişime geçin.',
                      textAlign: TextAlign.center,
                    ),
                  );
                }
                return RefreshIndicator(
                  onRefresh: () async => ref.invalidate(wholesalersProvider),
                  child: GridView.builder(
                    padding: const EdgeInsets.all(16),
                    gridDelegate:
                        const SliverGridDelegateWithFixedCrossAxisCount(
                      crossAxisCount: 2,
                      crossAxisSpacing: 12,
                      mainAxisSpacing: 12,
                      childAspectRatio: 1.1,
                    ),
                    itemCount: wholesalers.length,
                    itemBuilder: (_, i) {
                      final w = wholesalers[i];
                      return Card(
                        child: InkWell(
                          borderRadius: BorderRadius.circular(12),
                          onTap: () => context.goNamed(
                            'products',
                            pathParameters: {'wholesalerId': w.id},
                            queryParameters: {'name': w.companyName},
                          ),
                          child: Padding(
                            padding: const EdgeInsets.all(16),
                            child: Column(
                              mainAxisAlignment: MainAxisAlignment.center,
                              children: [
                                CircleAvatar(
                                  radius: 28,
                                  backgroundColor: cs.primaryContainer,
                                  child: Text(
                                    w.initials,
                                    style: TextStyle(
                                      fontSize: 20,
                                      fontWeight: FontWeight.bold,
                                      color: cs.onPrimaryContainer,
                                    ),
                                  ),
                                ),
                                const SizedBox(height: 10),
                                Text(
                                  w.companyName,
                                  textAlign: TextAlign.center,
                                  maxLines: 2,
                                  overflow: TextOverflow.ellipsis,
                                  style: const TextStyle(
                                      fontWeight: FontWeight.w600),
                                ),
                                if (w.phone != null) ...[
                                  const SizedBox(height: 4),
                                  Text(
                                    w.phone!,
                                    style: Theme.of(context)
                                        .textTheme
                                        .bodySmall
                                        ?.copyWith(
                                            color: cs.onSurfaceVariant),
                                  ),
                                ],
                              ],
                            ),
                          ),
                        ),
                      );
                    },
                  ),
                );
              },
            ),
          ),
        ],
      ),
    );
  }
}
