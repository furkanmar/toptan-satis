// ignore_for_file: constant_identifier_names
class ApiEndpoints {
  ApiEndpoints._();

  static const String login = '/auth/login';
  static const String myWholesalers = '/store-wholesalers/my';
  static const String products = '/products';
  static const String orders = '/orders';
  static const String creditSummary = '/credit/summary';
  static const String creditStatement = '/credit/statement';
  static const String usersTelegramChatId = '/users/me/telegram-chat-id';

  static String productById(String id) => '/products/$id';
  static String orderById(String id) => '/orders/$id';
  static String deliveryNotePdf(String id) => '/delivery-notes/$id/pdf';
}
