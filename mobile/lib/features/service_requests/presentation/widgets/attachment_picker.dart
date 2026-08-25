import 'dart:io';

import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import 'package:path_provider/path_provider.dart';
import 'package:record/record.dart';

class AttachmentSelection {
  const AttachmentSelection({
    this.photos = const [],
    this.video,
    this.audios = const [],
  });

  final List<File> photos;
  final File? video;
  final List<File> audios;
}

class AttachmentPicker extends StatefulWidget {
  const AttachmentPicker({super.key, required this.onChanged});

  final ValueChanged<AttachmentSelection> onChanged;

  static const maxPhotos = 10;
  static const maxAudios = 3;

  @override
  State<AttachmentPicker> createState() => _AttachmentPickerState();
}

class _AttachmentPickerState extends State<AttachmentPicker> {
  final _imagePicker = ImagePicker();
  final _recorder = AudioRecorder();

  final List<File> _photos = [];
  File? _video;
  final List<File> _audios = [];
  bool _isRecording = false;

  @override
  void dispose() {
    _recorder.dispose();
    super.dispose();
  }

  void _notify() {
    widget.onChanged(AttachmentSelection(photos: _photos, video: _video, audios: _audios));
  }

  Future<void> _addPhoto() async {
    if (_photos.length >= AttachmentPicker.maxPhotos) return;
    final picked = await _imagePicker.pickImage(source: ImageSource.gallery, imageQuality: 85);
    if (picked == null) return;
    setState(() => _photos.add(File(picked.path)));
    _notify();
  }

  void _removePhoto(int index) {
    setState(() => _photos.removeAt(index));
    _notify();
  }

  Future<void> _pickVideo() async {
    final picked = await _imagePicker.pickVideo(source: ImageSource.gallery);
    if (picked == null) return;
    setState(() => _video = File(picked.path));
    _notify();
  }

  void _removeVideo() {
    setState(() => _video = null);
    _notify();
  }

  Future<void> _toggleRecording() async {
    if (_isRecording) {
      final path = await _recorder.stop();
      setState(() => _isRecording = false);
      if (path != null) {
        setState(() => _audios.add(File(path)));
        _notify();
      }
      return;
    }

    if (_audios.length >= AttachmentPicker.maxAudios) return;
    if (!await _recorder.hasPermission()) return;

    final directory = await getTemporaryDirectory();
    final path = '${directory.path}/audio_${DateTime.now().microsecondsSinceEpoch}.m4a';
    await _recorder.start(const RecordConfig(), path: path);
    setState(() => _isRecording = true);
  }

  void _removeAudio(int index) {
    setState(() => _audios.removeAt(index));
    _notify();
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text('Adjuntos (opcional)', style: Theme.of(context).textTheme.titleMedium),
        const SizedBox(height: 4),
        Text(
          'Hasta ${AttachmentPicker.maxPhotos} fotos, 1 video y ${AttachmentPicker.maxAudios} audios de referencia.',
          style: Theme.of(context).textTheme.bodySmall,
        ),
        const SizedBox(height: 12),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: [
            for (var i = 0; i < _photos.length; i++) _PhotoThumbnail(file: _photos[i], onRemove: () => _removePhoto(i)),
            if (_photos.length < AttachmentPicker.maxPhotos)
              _AddTile(icon: Icons.add_photo_alternate_outlined, label: 'Foto', onTap: _addPhoto),
          ],
        ),
        const SizedBox(height: 12),
        if (_video != null)
          ListTile(
            contentPadding: EdgeInsets.zero,
            leading: const Icon(Icons.videocam_outlined),
            title: Text(_video!.path.split('/').last, overflow: TextOverflow.ellipsis),
            trailing: IconButton(icon: const Icon(Icons.close), onPressed: _removeVideo),
          )
        else
          OutlinedButton.icon(
            onPressed: _pickVideo,
            icon: const Icon(Icons.videocam_outlined),
            label: const Text('Agregar video'),
          ),
        const SizedBox(height: 12),
        OutlinedButton.icon(
          onPressed: _audios.length >= AttachmentPicker.maxAudios && !_isRecording ? null : _toggleRecording,
          icon: Icon(_isRecording ? Icons.stop_circle_outlined : Icons.mic_outlined),
          label: Text(_isRecording ? 'Detener grabación' : 'Grabar audio de referencia'),
        ),
        for (var i = 0; i < _audios.length; i++)
          ListTile(
            contentPadding: EdgeInsets.zero,
            leading: const Icon(Icons.audiotrack_outlined),
            title: Text('Audio ${i + 1}'),
            trailing: IconButton(icon: const Icon(Icons.close), onPressed: () => _removeAudio(i)),
          ),
      ],
    );
  }
}

class _PhotoThumbnail extends StatelessWidget {
  const _PhotoThumbnail({required this.file, required this.onRemove});

  final File file;
  final VoidCallback onRemove;

  @override
  Widget build(BuildContext context) {
    return Stack(
      children: [
        ClipRRect(
          borderRadius: BorderRadius.circular(8),
          child: Image.file(file, width: 72, height: 72, fit: BoxFit.cover),
        ),
        Positioned(
          top: -8,
          right: -8,
          child: IconButton(
            icon: const Icon(Icons.cancel, size: 20),
            onPressed: onRemove,
          ),
        ),
      ],
    );
  }
}

class _AddTile extends StatelessWidget {
  const _AddTile({required this.icon, required this.label, required this.onTap});

  final IconData icon;
  final String label;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(8),
      child: Container(
        width: 72,
        height: 72,
        decoration: BoxDecoration(
          border: Border.all(color: Theme.of(context).colorScheme.outlineVariant),
          borderRadius: BorderRadius.circular(8),
        ),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(icon, size: 20),
            Text(label, style: Theme.of(context).textTheme.labelSmall),
          ],
        ),
      ),
    );
  }
}
