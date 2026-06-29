using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace IsntGwent.Scripts
{
    public static class StreamingAssetsLoader
    {
        [System.Serializable]
        private class Manifest
        {
            public string[] files;
        }
        
        public static IEnumerator LoadAllJson(
            string folder,
            System.Action<List<string>> onComplete,
            System.Action<string> onError = null)
        {
            var manifestUrl = GetUrl(folder + "/_manifest.json");
            using var manifestReq = UnityWebRequest.Get(manifestUrl);
            yield return manifestReq.SendWebRequest();

            if (manifestReq.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"Failed to load manifest: {manifestReq.error}");
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