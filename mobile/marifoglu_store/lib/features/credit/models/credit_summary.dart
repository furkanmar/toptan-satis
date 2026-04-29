class CreditTransaction {
  final String id;
  final String type;
  final double amount;
  final double allocatedAmount;
  final double remainingAmount;
  final bool isFullyAllocated;
  final String description;
  final DateTime? dueDate;
  final String? orderId;
  final DateTime createdAt;

  const CreditTransaction({
    required this.id,
    required this.type,
    required this.amount,
    required this.allocatedAmount,
    required this.remainingAmount,
    required this.isFullyAllocated,
    required this.description,
    this.dueDate,
    this.orderId,
    required this.createdAt,
  });

  factory CreditTransaction.fromJson(Map<String, dynamic> json) =>
      CreditTransaction(
        id: json['id'].toString(),
        type: json['type'] as String,
        amount: (json['amount'] as num).toDouble(),
        allocatedAmount: (json['allocatedAmount'] as num).toDouble(),
        remainingAmount: (json['remainingAmount'] as num).toDouble(),
        isFullyAllocated: json['isFullyAllocated'] as bool,
        description: json['description'] as String,
        dueDate: json['dueDate'] != null
            ? DateTime.parse(json['dueDate'] as String)
            : null,
        orderId: json['orderId']?.toString(),
        createdAt: DateTime.parse(json['createdAt'] as String),
      );

  bool get isOverdue =>
      dueDate != null &&
      dueDate!.isBefore(DateTime.now()) &&
      !isFullyAllocated;
}

class CreditSummary {
  final String storeId;
  final String storeName;
  final double totalDebt;
  final double totalPayment;
  final double balance;
  final int openDebtCount;
  final double overdueAmount;
  final DateTime? oldestOverdueDate;
  final double unallocatedPaymentAmount;
  final List<CreditTransaction> transactions;

  const CreditSummary({
    required this.storeId,
    required this.storeName,
    required this.totalDebt,
    required this.totalPayment,
    required this.balance,
    required this.openDebtCount,
    required this.overdueAmount,
    this.oldestOverdueDate,
    required this.unallocatedPaymentAmount,
    required this.transactions,
  });

  factory CreditSummary.fromJson(Map<String, dynamic> json) => CreditSummary(
        storeId: json['storeId'].toString(),
        storeName: json['storeName'] as String,
        totalDebt: (json['totalDebt'] as num).toDouble(),
        totalPayment: (json['totalPayment'] as num).toDouble(),
        balance: (json['balance'] as num).toDouble(),
        openDebtCount: json['openDebtCount'] as int,
        overdueAmount: (json['overdueAmount'] as num).toDouble(),
        oldestOverdueDate: json['oldestOverdueDate'] != null
            ? DateTime.parse(json['oldestOverdueDate'] as String)
            : null,
        unallocatedPaymentAmount:
            (json['unallocatedPaymentAmount'] as num).toDouble(),
        transactions: (json['transactions'] as List<dynamic>)
            .map((e) =>
                CreditTransaction.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

class CreditStatementEntry {
  final DateTime date;
  final String type;
  final double amount;
  final double? remainingAmount;
  final double runningBalance;
  final String description;
  final String? orderId;

  const CreditStatementEntry({
    required this.date,
    required this.type,
    required this.amount,
    this.remainingAmount,
    required this.runningBalance,
    required this.description,
    this.orderId,
  });

  factory CreditStatementEntry.fromJson(Map<String, dynamic> json) =>
      CreditStatementEntry(
        date: DateTime.parse(json['date'] as String),
        type: json['type'] as String,
        amount: (json['amount'] as num).toDouble(),
        remainingAmount: json['remainingAmount'] != null
            ? (json['remainingAmount'] as num).toDouble()
            : null,
        runningBalance: (json['runningBalance'] as num).toDouble(),
        description: json['description'] as String,
        orderId: json['orderId']?.toString(),
      );

  bool get isDebt => type.contains('Debit') || type.contains('debit');
}

class CreditStatement {
  final String storeId;
  final String wholesalerId;
  final DateTime from;
  final DateTime to;
  final double openingBalance;
  final double closingBalance;
  final List<CreditStatementEntry> entries;

  const CreditStatement({
    required this.storeId,
    required this.wholesalerId,
    required this.from,
    required this.to,
    required this.openingBalance,
    required this.closingBalance,
    required this.entries,
  });

  factory CreditStatement.fromJson(Map<String, dynamic> json) =>
      CreditStatement(
        storeId: json['storeId'].toString(),
        wholesalerId: json['wholesalerId'].toString(),
        from: DateTime.parse(json['from'] as String),
        to: DateTime.parse(json['to'] as String),
        openingBalance: (json['openingBalance'] as num).toDouble(),
        closingBalance: (json['closingBalance'] as num).toDouble(),
        entries: (json['entries'] as List<dynamic>)
            .map((e) =>
                CreditStatementEntry.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}
