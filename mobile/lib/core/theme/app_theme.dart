import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

// iOS system colors (light & dark), see Apple's Human Interface Guidelines.
class _IosColors {
  static const blue = Color(0xFF007AFF);
  static const lightBackground = Color(0xFFF2F2F7);
  static const lightGroupedCard = Color(0xFFFFFFFF);
  static const lightSeparator = Color(0xFFE5E5EA);
  static const lightLabelSecondary = Color(0xFF8E8E93);

  static const darkBackground = Color(0xFF000000);
  static const darkSecondaryBackground = Color(0xFF1C1C1E);
  static const darkGroupedCard = Color(0xFF1C1C1E);
  static const darkSeparator = Color(0xFF38383A);
  static const darkLabelSecondary = Color(0xFF8E8E93);
}

class AppTheme {
  AppTheme._();

  static ThemeData light() => _build(Brightness.light);
  static ThemeData dark() => _build(Brightness.dark);

  static ThemeData _build(Brightness brightness) {
    final isDark = brightness == Brightness.dark;
    final background = isDark ? _IosColors.darkBackground : _IosColors.lightBackground;
    final cardColor = isDark ? _IosColors.darkGroupedCard : _IosColors.lightGroupedCard;
    final separator = isDark ? _IosColors.darkSeparator : _IosColors.lightSeparator;
    final secondaryLabel =
        isDark ? _IosColors.darkLabelSecondary : _IosColors.lightLabelSecondary;

    final colorScheme = ColorScheme.fromSeed(
      seedColor: _IosColors.blue,
      brightness: brightness,
      primary: _IosColors.blue,
      surface: cardColor,
    );

    final baseTextTheme = GoogleFonts.interTextTheme(
      isDark ? ThemeData.dark().textTheme : ThemeData.light().textTheme,
    );

    return ThemeData(
      useMaterial3: true,
      brightness: brightness,
      colorScheme: colorScheme,
      scaffoldBackgroundColor: background,
      textTheme: baseTextTheme.copyWith(
        headlineSmall: baseTextTheme.headlineSmall?.copyWith(
          fontWeight: FontWeight.w700,
          letterSpacing: -0.4,
        ),
        titleLarge: baseTextTheme.titleLarge?.copyWith(
          fontWeight: FontWeight.w600,
          letterSpacing: -0.2,
        ),
        bodyMedium: baseTextTheme.bodyMedium?.copyWith(color: secondaryLabel),
      ),
      appBarTheme: AppBarTheme(
        backgroundColor: background,
        surfaceTintColor: Colors.transparent,
        elevation: 0,
        scrolledUnderElevation: 0,
        centerTitle: true,
        titleTextStyle: baseTextTheme.titleLarge?.copyWith(
          fontWeight: FontWeight.w600,
          color: isDark ? Colors.white : Colors.black,
        ),
        iconTheme: IconThemeData(color: colorScheme.primary),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: cardColor,
        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(14),
          borderSide: BorderSide(color: separator),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(14),
          borderSide: BorderSide(color: separator),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(14),
          borderSide: BorderSide(color: colorScheme.primary, width: 1.5),
        ),
        errorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(14),
          borderSide: BorderSide(color: colorScheme.error),
        ),
        labelStyle: TextStyle(color: secondaryLabel),
      ),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          backgroundColor: colorScheme.primary,
          foregroundColor: Colors.white,
          minimumSize: const Size.fromHeight(52),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
          textStyle: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
          elevation: 0,
        ),
      ),
      textButtonTheme: TextButtonThemeData(
        style: TextButton.styleFrom(
          foregroundColor: colorScheme.primary,
          textStyle: const TextStyle(fontWeight: FontWeight.w500),
        ),
      ),
      segmentedButtonTheme: SegmentedButtonThemeData(
        style: SegmentedButton.styleFrom(
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
          selectedBackgroundColor: colorScheme.primary,
          selectedForegroundColor: Colors.white,
        ),
      ),
      snackBarTheme: SnackBarThemeData(
        backgroundColor: isDark ? _IosColors.darkSecondaryBackground : Colors.black87,
        behavior: SnackBarBehavior.floating,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        contentTextStyle: const TextStyle(color: Colors.white, fontSize: 14),
      ),
      cardTheme: CardThemeData(
        color: cardColor,
        elevation: 0,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        margin: EdgeInsets.zero,
      ),
      dividerTheme: DividerThemeData(color: separator, thickness: 0.6, space: 0.6),
      splashFactory: NoSplash.splashFactory,
      highlightColor: Colors.transparent,
    );
  }
}
