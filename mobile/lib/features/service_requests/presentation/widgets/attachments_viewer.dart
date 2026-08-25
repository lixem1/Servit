import 'dart:io';

import 'package:audioplayers/audioplayers.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:path_provider/path_provider.dart';
import 'package:servit_app/features/service_requests/domain/attachment.dart';
import 'package:servit_app/features/service_requests/presentation/service_requests_controller.dart';
import 'package:video_player/video_player.dart';

class AttachmentsViewer extends ConsumerWidget {
  const AttachmentsViewer({super.key, required this.requestId, required this.attachments});

  final String requestId;
  final List<Attachment> attachments;

  Future<File> _resolveFile(WidgetRef ref, Attachment attachment) async {
    final directory = await getTemporaryDirectory();
    final file = File('${directory.path}/${attachment.id}_${attachment.fileName}');
    if (await file.exists()) return file;

    final bytes = await ref.read(serviceRequestsRepositoryProvider).downloadAttachment(
          requestId: requestId,
          attachmentId: attachment.id,
        );
    await file.writeAsBytes(bytes);
    return file;
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    if (attachments.isEmpty) return const SizedBox.shrink();

    return SizedBox(
      height: 88,
      child: ListView.separated(
        scrollDirection: Axis.horizontal,
        itemCount: attachments.length,
        separatorBuilder: (_, _) => const SizedBox(width: 8),
        itemBuilder: (context, index) {
          final attachment = attachments[index];
          return FutureBuilder<File>(
            future: _resolveFile(ref, attachment),
            builder: (context, snapshot) {
              if (!snapshot.hasData) {
                return const SizedBox(
                  width: 72,
                  height: 72,
                  child: Center(child: CircularProgressIndicator(strokeWidth: 2)),
                );
              }
              return _AttachmentTile(attachment: attachment, file: snapshot.data!);
            },
          );
        },
      ),
    );
  }
}

class _AttachmentTile extends StatelessWidget {
  const _AttachmentTile({required this.attachment, required this.file});

  final Attachment attachment;
  final File file;

  @override
  Widget build(BuildContext context) {
    switch (attachment.type) {
      case 'Photo':
        return GestureDetector(
          onTap: () => showDialog(
            context: context,
            builder: (_) => Dialog(child: Image.file(file)),
          ),
          child: ClipRRect(
            borderRadius: BorderRadius.circular(8),
            child: Image.file(file, width: 72, height: 72, fit: BoxFit.cover),
          ),
        );
      case 'Video':
        return GestureDetector(
          onTap: () => Navigator.of(context).push(
            MaterialPageRoute(builder: (_) => _VideoPlayerPage(file: file)),
          ),
          child: Container(
            width: 72,
            height: 72,
            decoration: BoxDecoration(
              color: Theme.of(context).colorScheme.surfaceContainerHighest,
              borderRadius: BorderRadius.circular(8),
            ),
            child: const Icon(Icons.play_circle_outline, size: 32),
          ),
        );
      case 'Audio':
        return _AudioTile(file: file);
      default:
        return const SizedBox.shrink();
    }
  }
}

class _VideoPlayerPage extends StatefulWidget {
  const _VideoPlayerPage({required this.file});

  final File file;

  @override
  State<_VideoPlayerPage> createState() => _VideoPlayerPageState();
}

class _VideoPlayerPageState extends State<_VideoPlayerPage> {
  late final VideoPlayerController _controller;

  @override
  void initState() {
    super.initState();
    _controller = VideoPlayerController.file(widget.file)
      ..initialize().then((_) {
        if (mounted) setState(() {});
        _controller.play();
      });
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.black,
      appBar: AppBar(backgroundColor: Colors.black),
      body: Center(
        child: _controller.value.isInitialized
            ? AspectRatio(
                aspectRatio: _controller.value.aspectRatio,
                child: VideoPlayer(_controller),
              )
            : const CircularProgressIndicator(),
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: () => setState(() {
          _controller.value.isPlaying ? _controller.pause() : _controller.play();
        }),
        child: Icon(_controller.value.isPlaying ? Icons.pause : Icons.play_arrow),
      ),
    );
  }
}

class _AudioTile extends StatefulWidget {
  const _AudioTile({required this.file});

  final File file;

  @override
  State<_AudioTile> createState() => _AudioTileState();
}

class _AudioTileState extends State<_AudioTile> {
  final _player = AudioPlayer();
  bool _isPlaying = false;

  @override
  void initState() {
    super.initState();
    _player.onPlayerComplete.listen((_) {
      if (mounted) setState(() => _isPlaying = false);
    });
  }

  @override
  void dispose() {
    _player.dispose();
    super.dispose();
  }

  Future<void> _toggle() async {
    if (_isPlaying) {
      await _player.pause();
    } else {
      await _player.play(DeviceFileSource(widget.file.path));
    }
    setState(() => _isPlaying = !_isPlaying);
  }

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: _toggle,
      child: Container(
        width: 72,
        height: 72,
        decoration: BoxDecoration(
          color: Theme.of(context).colorScheme.surfaceContainerHighest,
          borderRadius: BorderRadius.circular(8),
        ),
        child: Icon(_isPlaying ? Icons.pause_circle_outline : Icons.play_circle_outline, size: 32),
      ),
    );
  }
}
