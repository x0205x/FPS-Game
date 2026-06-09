using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// Procedural lunar heightmap with deep impact craters, saved as TerrainData for the arena.
    /// Height stamp algorithm inspired by LRO-style lunar surface variation (procedural, not NASA DEM).
    /// </summary>
    public static class LunarTerrainHeightmapBuilder
    {
        public const string HeightmapAssetPath = "Assets/Art/Environment/Textures/LunarHeightmap.png";
        public const string TerrainDataAssetPath = "Assets/Art/Environment/Terrain/LunarArenaTerrainData.asset";

        private const int HeightmapResolution = 513;
        private const float MaxTerrainHeight = 42f;

        public static Terrain CreateTerrain(Transform parent, Material terrainMaterial, float arenaHalfExtent)
        {
            EnsureFolder("Assets/Art/Environment/Terrain");
            float terrainSize = arenaHalfExtent * 2f;
            TerrainData data = EnsureTerrainData(terrainSize, arenaHalfExtent);

            var terrainGo = Terrain.CreateTerrainGameObject(data);
            terrainGo.name = "LunarTerrain";
            terrainGo.transform.SetParent(parent, worldPositionStays: false);
            terrainGo.transform.position = new Vector3(-arenaHalfExtent, 0f, -arenaHalfExtent);
            terrainGo.isStatic = true;

            Terrain terrain = terrainGo.GetComponent<Terrain>();
            terrain.materialTemplate = terrainMaterial;
            terrain.drawInstanced = true;
            terrain.heightmapPixelError = 5f;
            terrain.basemapDistance = 1200f;
            terrain.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return terrain;
        }

        public static float SampleHeight(Terrain terrain, Vector3 worldPosition)
        {
            if (terrain == null) return 0f;
            return terrain.SampleHeight(worldPosition);
        }

        private static TerrainData EnsureTerrainData(float terrainSize, float arenaHalfExtent)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataAssetPath);
            float[,] heights = GenerateHeightmap(arenaHalfExtent);

            if (existing == null)
            {
                existing = new TerrainData
                {
                    heightmapResolution = HeightmapResolution,
                    size = new Vector3(terrainSize, MaxTerrainHeight, terrainSize),
                };
                AssetDatabase.CreateAsset(existing, TerrainDataAssetPath);
            }
            else
            {
                existing.heightmapResolution = HeightmapResolution;
                existing.size = new Vector3(terrainSize, MaxTerrainHeight, terrainSize);
            }

            existing.SetHeights(0, 0, heights);
            SaveHeightmapPreview(heights);
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssets();
            return existing;
        }

        private static float[,] GenerateHeightmap(float arenaHalfExtent)
        {
            int res = HeightmapResolution;
            var heights = new float[res, res];
            var rng = new System.Random(88031);

            for (int z = 0; z < res; z++)
            {
                for (int x = 0; x < res; x++)
                {
                    float nx = x / (float)(res - 1);
                    float nz = z / (float)(res - 1);
                    float n1 = Mathf.PerlinNoise(nx * 6f, nz * 6f);
                    float n2 = Mathf.PerlinNoise(nx * 18f + 2.3f, nz * 18f + 1.1f) * 0.35f;
                    float n3 = Mathf.PerlinNoise(nx * 42f + 7.7f, nz * 42f + 4.2f) * 0.12f;
                    heights[z, x] = Mathf.Clamp01(0.52f + (n1 - 0.5f) * 0.08f + (n2 - 0.5f) * 0.05f + n3 * 0.03f);
                }
            }

            int craterCount = Mathf.Clamp(Mathf.RoundToInt(arenaHalfExtent / 18f), 24, 64);
            for (int i = 0; i < craterCount; i++)
            {
                float cx = (float)rng.NextDouble();
                float cz = (float)rng.NextDouble();
                float radiusNorm = Mathf.Lerp(0.015f, 0.08f, (float)rng.NextDouble());
                float depthNorm = Mathf.Lerp(0.04f, 0.18f, (float)rng.NextDouble());
                StampCrater(heights, res, cx, cz, radiusNorm, depthNorm, rimBoost: 0.012f);
            }

            int microCount = Mathf.Clamp(craterCount * 2, 40, 120);
            for (int i = 0; i < microCount; i++)
            {
                float cx = (float)rng.NextDouble();
                float cz = (float)rng.NextDouble();
                StampCrater(heights, res, cx, cz, radiusNorm: 0.006f, depthNorm: 0.025f, rimBoost: 0.004f);
            }

            return heights;
        }

        private static void StampCrater(
            float[,] heights, int res, float centerX, float centerZ,
            float radiusNorm, float depthNorm, float rimBoost)
        {
            int cx = Mathf.RoundToInt(centerX * (res - 1));
            int cz = Mathf.RoundToInt(centerZ * (res - 1));
            int radiusPx = Mathf.Max(2, Mathf.RoundToInt(radiusNorm * res));

            for (int z = -radiusPx; z <= radiusPx; z++)
            {
                for (int x = -radiusPx; x <= radiusPx; x++)
                {
                    int px = cx + x;
                    int pz = cz + z;
                    if (px < 0 || pz < 0 || px >= res || pz >= res) continue;

                    float dist = Mathf.Sqrt(x * x + z * z) / radiusPx;
                    if (dist > 1.35f) continue;

                    float bowl = Mathf.SmoothStep(1f, 0f, dist);
                    bowl *= bowl;
                    float rim = dist > 0.82f && dist < 1.15f ? Mathf.SmoothStep(0f, 1f, 1f - Mathf.Abs(dist - 0.98f) / 0.16f) : 0f;
                    heights[pz, px] = Mathf.Clamp01(heights[pz, px] - depthNorm * bowl + rimBoost * rim);
                }
            }
        }

        private static void SaveHeightmapPreview(float[,] heights)
        {
            int res = heights.GetLength(0);
            var tex = new Texture2D(res, res, TextureFormat.RGB24, false);
            for (int z = 0; z < res; z++)
            {
                for (int x = 0; x < res; x++)
                {
                    float h = heights[z, x];
                    tex.SetPixel(x, z, new Color(h, h, h));
                }
            }

            tex.Apply();
            EnsureFolder("Assets/Art/Environment/Textures");
            File.WriteAllBytes(Path.GetFullPath(HeightmapAssetPath), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(HeightmapAssetPath);
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
