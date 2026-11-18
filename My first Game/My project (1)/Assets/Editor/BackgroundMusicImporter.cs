using UnityEditor;
using UnityEngine;

public class BackgroundMusicImporter : AssetPostprocessor
{
    private void OnPreprocessAudio()
    {
        if (assetPath != "Assets/Resources/Audio/Music/CitadelAmbient.mp3") return;
        AudioImporter importer = (AudioImporter)assetImporter;
        AudioImporterSampleSettings settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.Streaming;
        importer.defaultSampleSettings = settings;
    }
}
