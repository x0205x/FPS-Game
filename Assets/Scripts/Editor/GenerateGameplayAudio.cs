using System.IO;
using Game.Audio;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// Bakes procedural aircraft and space ambience clips into Resources for cleaner import.
    /// </summary>
    public static class GenerateGameplayAudio
    {
        private const string AircraftResourcesFolder = "Assets/Resources/Audio/Aircraft";
        private const string SpaceResourcesFolder = "Assets/Resources/Audio/Space";

        [MenuItem("Tools/Game/Generate Aircraft && Space Audio")]
        public static void GenerateMenu()
        {
            EnsureAssets(force: true);
            Debug.Log("[GenerateGameplayAudio] Wrote aircraft and space ambience clips to Resources/Audio.");
        }

        public static void EnsureAssets(bool force = false)
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/Audio");
            EnsureFolder(AircraftResourcesFolder);
            EnsureFolder(SpaceResourcesFolder);

            WriteClip(AircraftResourcesFolder + "/osprey_engine_loop.wav",
                ProceduralAudioUtility.CreateOspreyEngineLoop(), force);
            WriteClip(AircraftResourcesFolder + "/osprey_boost_layer.wav",
                ProceduralAudioUtility.CreateOspreyBoostLayer(), force);
            WriteClip(AircraftResourcesFolder + "/thruster_puff.wav",
                ProceduralAudioUtility.CreateThrusterPuff(), force);
            WriteClip(AircraftResourcesFolder + "/air_rush_loop.wav",
                ProceduralAudioUtility.CreateSpaceWindLoop(), force);

            WriteClip(SpaceResourcesFolder + "/space_hum_loop.wav",
                ProceduralAudioUtility.CreateSpaceHumLoop(), force);
            WriteClip(SpaceResourcesFolder + "/space_wind_loop.wav",
                ProceduralAudioUtility.CreateSpaceWindLoop(), force);
            WriteClip(SpaceResourcesFolder + "/space_chime.wav",
                ProceduralAudioUtility.CreateSpaceChime(), force);

            AssetDatabase.Refresh();
        }

        private static void WriteClip(string assetPath, AudioClip clip, bool force)
        {
            if (!force && AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath) != null)
                return;

            string fullPath = Path.GetFullPath(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            int sampleCount = clip.samples * clip.channels;
            var samples = new float[sampleCount];
            if (!clip.GetData(samples, 0))
            {
                Debug.LogWarning($"[GenerateGameplayAudio] Could not read samples for {assetPath}");
                return;
            }

            WriteWav(fullPath, samples, clip.frequency);
            AssetDatabase.ImportAsset(assetPath);

            var importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
            if (importer == null) return;

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.CompressedInMemory;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;
            importer.SaveAndReimport();
        }

        private static void WriteWav(string path, float[] samples, int sampleRate)
        {
            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
            using var writer = new BinaryWriter(stream);

            int channels = 1;
            int bitsPerSample = 16;
            int byteRate = sampleRate * channels * bitsPerSample / 8;
            short blockAlign = (short)(channels * bitsPerSample / 8);
            int dataSize = samples.Length * blockAlign;

            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataSize);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write(blockAlign);
            writer.Write((short)bitsPerSample);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(dataSize);

            foreach (float sample in samples)
            {
                float clamped = Mathf.Clamp(sample, -1f, 1f);
                short pcm = (short)(clamped * short.MaxValue);
                writer.Write(pcm);
            }
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath)) return;
            string parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            string leaf = Path.GetFileName(assetPath);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(leaf))
                AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
