class Attachment {
  const Attachment({
    required this.id,
    required this.type,
    required this.fileName,
  });

  final String id;
  final String type;
  final String fileName;

  factory Attachment.fromJson(Map<String, dynamic> json) => Attachment(
        id: json['id'] as String,
        type: json['type'] as String,
        fileName: json['fileName'] as String,
      );
}
