using DDS3ModelLibrary.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DDS3ModelLibrary.Materials
{
    public static class MaterialPresetStore
    {
        private static Dictionary<int, int> sMaterialHashToId;
        private static HashSet<int> sValidPresetIds;

        static MaterialPresetStore()
        {
            LoadIndex();
        }

        private static void LoadIndex()
        {
            sValidPresetIds = new HashSet<int>();
            var hashToIdJsonPath = GetPath("index.json");
            if (File.Exists(hashToIdJsonPath))
            {
                sMaterialHashToId = JsonSerializer.Deserialize<Dictionary<int, int>>(File.ReadAllText(hashToIdJsonPath));
                foreach (int value in sMaterialHashToId.Values)
                    sValidPresetIds.Add(value);
            }
            else
            {
                sMaterialHashToId = new Dictionary<int, int>();
            }
        }

        private static string GetPath(string path) => ResourceStore.GetPath("material_presets\\" + path);

        public static bool IsPreset(Material material) => sMaterialHashToId.ContainsKey(material.GetPresetHashCode());

        public static int GetPresetId(Material material) => sMaterialHashToId[material.GetPresetHashCode()];

        public static bool TryGetPresetId(Material material, out int presetId)
        {
            return sMaterialHashToId.TryGetValue(material.GetPresetHashCode(), out presetId);
        }

        public static bool IsValidPresetId(int id) => sValidPresetIds.Contains(id);

        public static Material GetPreset(int id, bool hasTexture, bool hasOverlay)
        {
            var path = GetMaterialPresetPath(id, hasTexture, hasOverlay);
            if (!File.Exists(path))
                throw new ArgumentOutOfRangeException(nameof(id), $"Material preset {id} does not exist");

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Material>(json);
        }

        private static string GetMaterialPresetPath(int id, bool hasTexture, bool hasOverlay)
        {
            var name = id.ToString();
            if (hasTexture)
                name += "_d";

            if (hasOverlay)
                name += "_o";

            return GetPath(name + ".json");
        }
    }
}
