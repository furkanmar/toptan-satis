import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../../core/widgets/error_view.dart';
import '../models/credit_summary.dart';
import '../providers/credit_provider.dart';

final _dateFmt = DateFormat('dd.MM.yyyy', 'tr_TR');
final _numFmt = NumberFormat('#,##0.00', 'tr_TR');

class CreditScreen extends ConsumerStatefulWidget {
  const CreditScreen({super.key});

  @override
  ConsumerState<CreditScreen> createState() => _CreditScreenState();
}

class _CreditScreenState extends ConsumerState<CreditScreen>
    with SingleTickerProviderStateMixin {
  late TabController _tabCtrl;
  DateTime _from = DateTime.now().subtract(const Duration(days: 30));
  DateTime _to = DateTime.now();

  @override
  void initState() {
    super.initState();
    _tabCtrl = TabController(length: 2, vsync: this);
  }

  @override
  void dispose() {
    _tabCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Cari Hesap'),
        bottom: TabBar(
          controller: _tabCtrl,
          tabs: const [
            Tab(text: 'Özet'),
            Tab(text: 'Ekstre'),
          ],
        ),
      ),
      body: TabBarView(
        controller: _tabCtrl,
        children: [
          _SummaryTab(),
          _StatementTab(
            from: _from,
            to: _to,
            onDateRangeChanged: (f, t) =>
                setState(() {
                  _from = f;
                  _to = t;
                }),
          ),
        ],
      ),
    );
  }
}

class _SummaryTab extends ConsumerWidget {
  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final summaryAsync = ref.watch(creditSummaryProvider);
    final cs = Theme.of(context).colorScheme;

    return summaryAsync.when(
      loading: () => const Center(child: CircularProgressIndicator()),
      error: (e, _) => ErrorView(
        message: e.toString(),
        onRetry: () => ref.invalidate(creditSummaryProvider),
      ),
      data: (summary) => RefreshIndicator(
        onRefresh: () async => ref.invalidate(creditSummaryProvider),
        child: SingleChildScrollView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.all(16),
          child: Column(
            children: [
              // Bakiye kartı
              Card(
                color: summary.balance > 0 ? cs.errorContainer : cs.primaryContainer,
                child: Padding(
                  padding: const EdgeInsets.all(20),
                  child: Column(
                    children: [
                      Text(
                        'Net Bakiye (Borç)',
                        style: TextStyle(
                          color: summary.balance > 0
                              ? cs.onErrorContainer
                              : cs.onPrimaryContainer,
                        ),
                      ),
                      const SizedBox(height: 8),
                      Text(
                        '${_numFmt.format(summary.balance)} ₺',
                        style: TextStyle(
                          fontSize: 28,
                          fontWeight: FontWeight.bold,
                          color: summary.balance > 0
                              ? cs.onErrorContainer
                              : cs.onPrimaryContainer,
                        ),
                      ),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 12),

              // Detay satırları
              _InfoRow('Toplam Borç', '${_numFmt.format(summary.totalDebt)} ₺'),
              _InfoRow('Toplam Ödeme', '${_numFmt.format(summary.totalPayment)} ₺'),
              _InfoRow('Açık Borç Sayısı', '${summary.openDebtCount} adet'),
              if (summary.overdueAmount > 0)
                _InfoRow(
                  'Vadesi Geçen',
                  '${_numFmt.format(summary.overdueAmount)} ₺',
                  valueColor: cs.error,
                ),
              if (summary.oldestOverdueDate != null)
                _InfoRow(
                  'En Eski Vade',
                  _dateFmt.format(summary.oldestOverdueDate!.toLocal()),
                  valueColor: cs.error,
                ),
              if (summary.unallocatedPaymentAmount > 0)
                _InfoRow(
                  'Avans Bakiyesi',
                  '${_numFmt.format(summary.unallocatedPaymentAmount)} ₺',
                  valueColor: Colors.green,
                ),
            ],
          ),
        ),
      ),
    );
  }
}

class _InfoRow extends StatelessWidget {
  final String label;
  final String value;
  final Color? valueColor;

  const _InfoRow(this.label, this.value, {this.valueColor});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label),
          Text(
            value,
            style: TextStyle(
              fontWeight: FontWeight.bold,
              color: valueColor,
            ),
          ),
        ],
      ),
    );
  }
}

class _StatementTab extends ConsumerWidget {
  final DateTime from;
  final DateTime to;
  final void Function(DateTime, DateTime) onDateRangeChanged;

  const _StatementTab({
    required this.from,
    required this.to,
    required this.onDateRangeChanged,
  });

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final statementAsync =
        ref.watch(creditStatementProvider((from: from, to: to)));
    final cs = Theme.of(context).colorScheme;

    return Column(
      children: [
        // Tarih aralığı seçici
        Padding(
          padding: const EdgeInsets.all(12),
          child: Row(
            children: [
              Expanded(
                child: OutlinedButton.icon(
                  icon: const Icon(Icons.date_range, size: 16),
                  label: Text(
                    '${_dateFmt.format(from)} – ${_dateFmt.format(to)}',
                    style: const TextStyle(fontSize: 12),
                  ),
                  onPressed: () async {
                    final range = await showDateRangePicker(
                      context: context,
                      firstDate: DateTime(2020),
                      lastDate: DateTime.now(),
                      initialDateRange:
                          DateTimeRange(start: from, end: to),
                    );
                    if (range != null) {
                      onDateRangeChanged(range.start, range.end);
                    }
                  },
                ),
              ),
            ],
          ),
        ),

        Expanded(
          child: statementAsync.when(
            loading: () =>
                const Center(child: CircularProgressIndicator()),
            error: (e, _) => ErrorView(message: e.toString()),
            data: (statement) {
              if (statement.entries.isEmpty) {
                return const Center(
                    child: Text('Bu dönemde hareket yok'));
              }
              return ListView.builder(
                padding: const EdgeInsets.symmetric(horizontal: 12),
                itemCount: statement.entries.length + 1, // +1 for header
                itemBuilder: (_, i) {
                  if (i == 0) {
                    return Padding(
                      padding: const EdgeInsets.only(bottom: 8),
                      child: Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          Text(
                              'Açılış: ${_numFmt.format(statement.openingBalance)} ₺'),
                          Text(
                              'Kapanış: ${_numFmt.format(statement.closingBalance)} ₺'),
                        ],
                      ),
                    );
                  }
                  final entry = statement.entries[i - 1];
                  final isOverdue = entry.isDebt &&
                      entry.remainingAmount != null &&
                      entry.remainingAmount! > 0;

                  return Card(
                    margin: const EdgeInsets.only(bottom: 6),
                    color: isOverdue
                        ? cs.errorContainer.withOpacity(0.3)
                        : null,
                    child: Padding(
                      padding: const EdgeInsets.symmetric(
                          horizontal: 12, vertical: 8),
                      child: Row(
                        children: [
                          Expanded(
                            child: Column(
                              crossAxisAlignment:
                                  CrossAxisAlignment.start,
                              children: [
                                Text(
                                  entry.description,
                                  style: const TextStyle(
                                      fontWeight: FontWeight.w500),
                                  maxLines: 2,
                                ),
                                Text(
                                  _dateFmt
                                      .format(entry.date.toLocal()),
                                  style: TextStyle(
                                      fontSize: 11,
                                      color: cs.onSurfaceVariant),
                                ),
                              ],
                            ),
                          ),
                          Column(
                            crossAxisAlignment:
                                CrossAxisAlignment.end,
                            children: [
                              Text(
                                '${entry.isDebt ? '+' : '-'}${_numFmt.format(entry.amount)} ₺',
                                style: TextStyle(
                                  fontWeight: FontWeight.bold,
                                  color: entry.isDebt
                                      ? cs.error
                                      : Colors.green,
                                ),
                              ),
                              Text(
                                'Bakiye: ${_numFmt.format(entry.runningBalance)} ₺',
                                style: TextStyle(
                                    fontSize: 11,
                                    color: cs.onSurfaceVariant),
                              ),
                            ],
                          ),
                        ],
                      ),
                    ),
                  );
                },
              );
            },
          ),
        ),
      ],
    );
  }
}
