import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../../core/api/api_endpoints.dart';
import '../../../core/errors/app_exception.dart';
import '../../auth/providers/auth_provider.dart';

class ProfileScreen extends ConsumerStatefulWidget {
  const ProfileScreen({super.key});

  @override
  ConsumerState<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends ConsumerState<ProfileScreen> {
  final _telegramCtrl = TextEditingController();
  bool _isSaving = false;
  String? _telegramStatus;

  @override
  void dispose() {
    _telegramCtrl.dispose();
    super.dispose();
  }

  Future<void> _saveTelegramChatId() async {
    final chatId = _telegramCtrl.text.trim();
    if (chatId.isEmpty) return;

    setState(() {
      _isSaving = true;
      _telegramStatus = null;
    });

    try {
      final dio = ref.read(dioProvider);
      await dio.post(
        ApiEndpoints.usersTelegramChatId,
        data: {'chatId': chatId},
      );
      setState(() => _telegramStatus = '✓ Telegram bağlandı');
      _telegramCtrl.clear();
    } on DioException catch (e) {
      setState(() =>
          _telegramStatus = (e.error as AppException?)?.message ??
              'Kaydetme başarısız');
    } finally {
      if (mounted) setState(() => _isSaving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final authState = ref.watch(authProvider);
    final cs = Theme.of(context).colorScheme;

    return Scaffold(
      appBar: AppBar(title: const Text('Profil')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // Kullanıcı bilgileri
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Row(
                  children: [
                    CircleAvatar(
                      radius: 30,
                      backgroundColor: cs.primaryContainer,
                      child: Text(
                        (authState.displayName ?? '?')
                            .substring(0, 1)
                            .toUpperCase(),
                        style: TextStyle(
                          fontSize: 24,
                          color: cs.onPrimaryContainer,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ),
                    const SizedBox(width: 14),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            authState.displayName ?? '-',
                            style: const TextStyle(
                              fontWeight: FontWeight.bold,
                              fontSize: 16,
                            ),
                          ),
                          Text(
                            authState.role ?? '',
                            style: TextStyle(color: cs.onSurfaceVariant),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
            ),

            const SizedBox(height: 16),

            // Telegram Bağlantısı
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text(
                      'Telegram Bildirimleri',
                      style: TextStyle(
                          fontWeight: FontWeight.bold, fontSize: 15),
                    ),
                    const SizedBox(height: 8),
                    Text(
                      'Sipariş güncellemelerini Telegram\'dan almak için bot\'umuza mesaj gönderin ve Chat ID\'nizi girin.',
                      style:
                          TextStyle(color: cs.onSurfaceVariant, fontSize: 13),
                    ),
                    const SizedBox(height: 10),
                    FilledButton.tonal(
                      onPressed: () async {
                        const botUrl =
                            'https://t.me/marifoglubot'; // Bot adresinizi güncelleyin
                        final uri = Uri.parse(botUrl);
                        if (await canLaunchUrl(uri)) {
                          await launchUrl(uri,
                              mode: LaunchMode.externalApplication);
                        }
                      },
                      child: const Text('Telegram Bot\'u Aç'),
                    ),
                    const SizedBox(height: 12),
                    Row(
                      children: [
                        Expanded(
                          child: TextField(
                            controller: _telegramCtrl,
                            keyboardType: TextInputType.number,
                            decoration: const InputDecoration(
                              labelText: 'Chat ID',
                              hintText: '123456789',
                              prefixIcon:
                                  Icon(Icons.telegram, color: Color(0xFF2AABEE)),
                            ),
                          ),
                        ),
                        const SizedBox(width: 8),
                        FilledButton(
                          onPressed: _isSaving ? null : _saveTelegramChatId,
                          child: _isSaving
                              ? const SizedBox(
                                  width: 18,
                                  height: 18,
                                  child: CircularProgressIndicator(
                                      strokeWidth: 2),
                                )
                              : const Text('Kaydet'),
                        ),
                      ],
                    ),
                    if (_telegramStatus != null) ...[
                      const SizedBox(height: 8),
                      Text(
                        _telegramStatus!,
                        style: TextStyle(
                          color: _telegramStatus!.startsWith('✓')
                              ? Colors.green
                              : cs.error,
                        ),
                      ),
                    ],
                  ],
                ),
              ),
            ),

            const SizedBox(height: 16),

            // Menü seçenekleri
            Card(
              child: Column(
                children: [
                  ListTile(
                    leading: const Icon(Icons.receipt_long_outlined),
                    title: const Text('Siparişlerim'),
                    trailing: const Icon(Icons.chevron_right),
                    onTap: () => context.pushNamed('orders'),
                  ),
                  const Divider(height: 1),
                  ListTile(
                    leading: const Icon(Icons.account_balance_outlined),
                    title: const Text('Cari Hesap'),
                    trailing: const Icon(Icons.chevron_right),
                    onTap: () => context.pushNamed('credit'),
                  ),
                ],
              ),
            ),

            const SizedBox(height: 16),

            // Çıkış
            OutlinedButton.icon(
              onPressed: () async {
                final confirmed = await showDialog<bool>(
                  context: context,
                  builder: (_) => AlertDialog(
                    title: const Text('Çıkış Yap'),
                    content:
                        const Text('Hesabınızdan çıkış yapmak istiyor musunuz?'),
                    actions: [
                      TextButton(
                        onPressed: () => Navigator.pop(context, false),
                        child: const Text('İptal'),
                      ),
                      FilledButton(
                        onPressed: () => Navigator.pop(context, true),
                        child: const Text('Çıkış Yap'),
                      ),
                    ],
                  ),
                );
                if (confirmed == true) {
                  await ref.read(authProvider.notifier).logout();
                }
              },
              icon: const Icon(Icons.logout, color: Colors.red),
              label: const Text('Çıkış Yap',
                  style: TextStyle(color: Colors.red)),
              style: OutlinedButton.styleFrom(
                side: const BorderSide(color: Colors.red),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
