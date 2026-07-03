namespace Editor
{
#if UNITY_EDITOR
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;

    public static class StreamingAssetsManifestBuilder
    {
        [MenuItem("Tools/Rebuild StreamingAssets Manifest")]
        public static void Rebuild()
        {
            BuildManifestFor("Cards");
            BuildManifestFor("Decks");
            AssetDatabase.Refresh();
            Debug.Log("StreamingAssets manifests rebuilt.");
        }

        [InitializeOnLoadMethod]
        static void RegisterBuildCallback()
        {
            BuildPlayerWindow.RegisterBuildPlayerHandler(buildOptions =>
            {
                Rebuild();
                BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(buildOptions);
            });
        }

        private static void BuildManifestFor(string folder)
        {
            var dirPath = Path.Combine(Application.streamingAssetsPath, folder);
            if (!Directory.Exists(dirPath)) return;

            var files = Directory.GetFiles(dirPath, "*.json")
                .Select(Path.GetFileName)
                .ToArray();

            var manifestPath = Path.Combine(dirPath, "_manifest.json");
            File.WriteAllText(manifestPath, JsonUtility.ToJson(new Manifest { files = files }));
        }

        [System.Serializable]
        private class Manifest { public string[] files; }
    }
#endif
}