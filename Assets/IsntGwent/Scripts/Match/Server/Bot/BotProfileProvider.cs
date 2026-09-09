using System.Collections;
using System.Collections.Generic;
using IsntGwent.Scripts.Core;
using IsntGwent.Scripts.Diagnostics;
using Newtonsoft.Json;
using Zenject;

namespace IsntGwent.Scripts.Match.Server.Bot
{
    public class BotProfileProvider : IInitializable
    {
        private readonly CoroutineRunner _runner;
        private readonly Dictionary<string, BotProfile> _profiles = new();

        public BotProfile Default { get; private set; } = new();

        public BotProfileProvider(CoroutineRunner runner)
        {
            _runner = runner;
        }

        public void Initialize()
        {
            _runner.StartCoroutine(LoadCoroutine());
        }

        private IEnumerator LoadCoroutine()
        {
            yield return StreamingAssetsLoader.LoadAllJson(
                folder: "Bots",
                onComplete: jsonList =>
                {
                    foreach (var json in jsonList)
                    {
                        var profile = JsonConvert.DeserializeObject<BotProfile>(json);
                        if (profile == null || string.IsNullOrEmpty(profile.Id)) continue;

                        _profiles[profile.Id] = profile;
                    }

                    if (_profiles.TryGetValue(Default.Id, out var starter))
                        Default = starter;
                },
                onError: err => Log.Warn(LogTag.Data, err)
            );
        }

        public BotProfile Get(string id)
        {
            if (!string.IsNullOrEmpty(id) && _profiles.TryGetValue(id, out var profile))
                return profile;

            return Default;
        }
    }
}
