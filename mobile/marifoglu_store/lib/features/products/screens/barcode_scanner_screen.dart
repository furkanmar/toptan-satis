import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

import '../providers/product_provider.dart';
import '../widgets/add_to_cart_modal.dart';
import '../models/product.dart';

class BarcodeScannerScreen extends ConsumerStatefulWidget {
  final String wholesalerId;

  const BarcodeScannerScreen({super.key, required this.wholesalerId});

  @override
  ConsumerState<BarcodeScannerScreen> createState() =>
      _BarcodeScannerScreenState();
}

class _BarcodeScannerScreenState extends ConsumerState<BarcodeScannerScreen> {
  final _controller = MobileScannerController();
  bool _isProcessing = false;
  String? _statusMessage;

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _onBarcode(BarcodeCapture capture) async {
    if (_isProcessing) return;
    final barcodes = capture.barcodes;
    if (barcodes.isEmpty) return;

    final raw = barcodes.first.rawValue;
    if (raw == null) return;

    setState(() {
      _isProcessing = true;
      _statusMessage = 'Aranıyor: $raw';
    });

    await _controller.stop();

    // Client-side arama
    final productsAsync = ref.read(productsProvider(widget.wholesalerId));
    final products = productsAsync.valueOrNull ?? [];
    final found = findByBarcode(products, raw);

    if (!mounted) return;

    if (found != null) {
      showAddToCartModal(context, ref, found, widget.wholesalerId);
      setState(() {
        _isProcessing = false;
        _statusMessage = null;
      });
    } else {
      setState(() {
        _statusMessage = 'Ürün bulunamadı: $raw';
        _isProcessing = false;
      });
      await Future.delayed(const Duration(seconds: 2));
      if (mounted) {
        await _controller.start();
        setState(() => _statusMessage = null);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Barkod Tara'),
        actions: [
          IconButton(
            icon: ValueListenableBuilder(
              valueListenable: _controller,
              builder: (_, state, __) => Icon(
                state.torchState == TorchState.on
                    ? Icons.flash_on
                    : Icons.flash_off,
              ),
            ),
            onPressed: _controller.toggleTorch,
            tooltip: 'Flaş',
          ),
        ],
      ),
      body: Stack(
        children: [
          MobileScanner(
            controller: _controller,
            onDetect: _onBarcode,
          ),

          // Hedef çerçeve
          Center(
            child: Container(
              width: 250,
              height: 150,
              decoration: BoxDecoration(
                border: Border.all(color: cs.primary, width: 3),
                borderRadius: BorderRadius.circular(12),
              ),
            ),
          ),

          // Talimat / durum mesajı
          Positioned(
            bottom: 40,
            left: 0,
            right: 0,
            child: Center(
              child: Container(
                padding:
                    const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
                decoration: BoxDecoration(
                  color: Colors.black54,
                  borderRadius: BorderRadius.circular(20),
                ),
                child: Text(
                  _statusMessage ?? 'Barkodu çerçeve içine getirin',
                  style: const TextStyle(color: Colors.white),
                ),
              ),
            ),
          ),

          if (_isProcessing)
            const Center(child: CircularProgressIndicator()),
        ],
      ),
    );
  }
}
