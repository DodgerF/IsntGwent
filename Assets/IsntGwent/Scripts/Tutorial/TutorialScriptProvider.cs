using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IsntGwent.Scripts.Core;
using IsntGwent.Scripts.Decks.Definitions;
using IsntGwent.Scripts.Diagnostics;
using IsntGwent.Scripts.Tutorial.Definitions;
using Newtonsoft.Json;
using UniRx;
using Zenject;

namespace IsntGwent.Scripts.Tutorial
{
    public class TutorialScriptProvider : IInitializable
    {
        public const string PlayerDeckId = "tutorial_player";
        public const string BotDeckId = "tutorial_bot";

        private readonly CoroutineRunner _runner;

        public TutorialScript Script { get; private set; }

        public readonly ReactiveProperty<bool> OnLoaded = new(false);

        public TutorialScriptProvider(CoroutineRunner runner)
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
                folder: "Tutorial",
                onComplete: jsonList =>
                {
                    foreach (var json in jsonList)
                    {
                        var script = JsonConvert.DeserializeObject<TutorialScript>(json);
                        if (script == null || string.IsNullOrEmpty(script.Id)) continue;

                        Script = script;
                        break;
                    }

                    if (Script == null)
                    {
                        Log.Warn(LogTag.Data, "tutorial script not found");
                        return;
                    }

                    OnLoaded.Value = true;
                },
                onError: err => Log.Warn(LogTag.Data, err)
            );
        }

        public DeckDefinition PlayerDeck() => BuildDeck(PlayerDeckId, Script?.PlayerDeck);

        public DeckDefinition BotDeck() => BuildDeck(BotDeckId, Script?.BotDeck);

        public IReadOnlyList<TutorialStep> IntroSteps => Script?.IntroSteps ?? new List<TutorialStep>();

        public IReadOnlyList<TutorialStep> Steps => Script?.Steps ?? new List<TutorialStep>();

        public IReadOnlyList<TutorialStep> MenuSteps => Script?.MenuSteps ?? new List<TutorialStep>();

        private static DeckDefinition BuildDeck(string id, IReadOnlyList<string> cardIds)
        {
            return new DeckDefinition
            {
                Id = id,
                Name = id,
                Cards = (cardIds ?? new List<string>())
                    .Select(cardId => new DeckCardEntry { CardId = cardId, Count = 1 })
                    .ToArray(),
            };
        }
    }
}
