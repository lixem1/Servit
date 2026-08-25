import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:servit_app/main.dart';

void main() {
  testWidgets('App boots to the login screen', (WidgetTester tester) async {
    await tester.pumpWidget(const ProviderScope(child: ServitApp()));
    await tester.pump(const Duration(milliseconds: 500));

    expect(find.text('Bienvenido'), findsWidgets);
  });
}
