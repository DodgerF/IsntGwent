using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace IsntGwent.Scripts.Core
{
    public static class StreamingAssetsLoader
    {
        [System.Serializable]
        private class Manifest { public string[] files; }

        private static bool RequiresWebRequest
        {
            get
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.Android:
                    case RuntimePlatform.WebGLPlayer:
                        return true;

                    case RuntimePlatform.WindowsPlayer:
                    case RuntimePlatform.WindowsEditor:
                    case RuntimePlatform.LinuxPlayer:
                    case RuntimePlatform.LinuxEditor:
                    case RuntimePlatform.OSXPlayer:
                    case RuntimePlatform.OSXEditor:
                        return false;

                    default:
                        return false;
                }
            }
        }

        public static IEnumerator LoadAllJson(
            string folder,
            System.Action<List<string>> onComplete,
            System.Action<string> onError = null)
        {
            if (RequiresWebRequest)
                yield return LoadAllJsonWeb(folder, onComplete, onError);
            else
                LoadAllJsonDirect(folder, onComplete, onError);
        }
        
        private static void LoadAllJsonDirect(
            string folder,
            System.Action<List<string>> onComplete,
            System.Action<string> onError)
        {
            var dirPath = Path.Combine(Application.streamingAssetsPath, folder);

            if (!Directory.Exists(dirPath))
            {
                onError?.Invoke($"Directory not found: {dirPath}");
                onComplete(new List<string>());
                return;
            }

            var results = new List<string>();
            var files = Directory.GetFiles(dirPath, "*.json");

            foreach (var file in files)
            {
                if (Path.GetFileName(file) == "_manifest.json") continue;
                results.Add(File.ReadAllText(file));
            }

            onComplete(results);
        }
        
        private static IEnumerator LoadAllJsonWeb(
            string folder,
            System.Action<List<string>> onComplete,
            System.Action<string> onError)
        {
            var manifestUrl = GetUrl(folder + "/_manifest.json");
            using var manifestReq = UnityWebRequest.Get(manifestUrl);
            yield return manifestReq.SendWebRequest();

            if (manifestReq.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"Failed to load manifest: {manifestReq.error}");
                onComplete(new List<string>());
                yield break;
            }

            var manifest = JsonUtility.FromJson<Manifest>(manifestReq.downloadHandler.text);
            var results = new List<string>();

            foreach (var fileName in manifest.files)
            {
                if (fileName == "_manifest.json") continue;

                var url = GetUrl(folder + "/" + fileName);
                using var req = UnityWebRequest.Get(url);
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    onError?.Invoke($"Failed to load {fileName}: {req.error}");
                    continue;
                }

                results.Add(req.downloadHandler.text);
            }

            onComplete(results);
        }

        private static string GetUrl(string relativePath)
        {
            return Path.Combine(Application.streamingAssetsPath, relativePath);
        }
    }
}