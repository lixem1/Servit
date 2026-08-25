import 'package:flutter/material.dart';

String requestStatusLabel(String status, {int responseCount = 0}) {
  switch (status) {
    case 'Pending':
      return responseCount > 0 ? 'Cotizada' : 'Buscando proveedor';
    case 'Assigned':
      return 'Asignada';
    case 'Completed':
      return 'Completada';
    case 'Cancelled':
      return 'Cancelada';
    default:
      return status;
  }
}

String responseStatusLabel(String status) {
  switch (status) {
    case 'Pending':
      return 'Pendiente';
    case 'Accepted':
      return 'Aceptada';
    case 'Declined':
      return 'Rechazada';
    default:
      return status;
  }
}

Color requestStatusColor(BuildContext context, String status, {int responseCount = 0}) {
  final scheme = Theme.of(context).colorScheme;
  switch (status) {
    case 'Assigned':
    case 'Accepted':
      return Colors.green;
    case 'Cancelled':
    case 'Declined':
      return scheme.error;
    case 'Pending':
      return responseCount > 0 ? Colors.amber.shade800 : scheme.primary;
    default:
      return scheme.primary;
  }
}

String timeAgo(DateTime dateTime) {
  final diff = DateTime.now().difference(dateTime);
  if (diff.inMinutes < 1) return 'ahora';
  if (diff.inMinutes < 60) return 'hace ${diff.inMinutes} min';
  if (diff.inHours < 24) return 'hace ${diff.inHours} h';
  return 'hace ${diff.inDays} d';
}

String formatDistance(double? meters) {
  if (meters == null) return '';
  if (meters < 1000) return '${meters.round()} m';
  return '${(meters / 1000).toStringAsFixed(1)} km';
}
