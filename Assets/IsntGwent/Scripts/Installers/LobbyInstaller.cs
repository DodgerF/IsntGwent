using IsntGwent.Scripts.Audio;
using IsntGwent.Scripts.Cards.Server;
using IsntGwent.Scripts.Cards.Client;
using IsntGwent.Scripts.Cards.UI;
using IsntGwent.Scripts.Accounts.Server;
using IsntGwent.Scripts.Lobby.Client;
using IsntGwent.Scripts.Lobby.UI;
using IsntGwent.Scripts.Core;
using IsntGwent.Scripts.Decks.UI;
using IsntGwent.Scripts.Match.Server;
using IsntGwent.Scripts.Match.Server.Bot;
using IsntGwent.Scripts.Tutorial.Server;
using IsntGwent.Scripts.Tutorial.UI;
using IsntGwent.Scripts.Match.Server.Journal;
using IsntGwent.Scripts.Match.Server.Stats;
using IsntGwent.Scripts.Lobby.Server;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zenject;

namespace IsntGwent.Scripts.Installers
{
    public class LobbyInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container
                .Bind<EffectRegistry>()
                .AsSingle();
            Container
                .Bind<CardResolver>()
                .AsSingle();
            Container
                .Bind<TriggeredEffectDispatcher>()
                .AsSingle();
            Container
                .Bind<CardPreviewService>()
                .AsSingle();
            Container
                .Bind<CardTooltipView>()
                .FromComponentInHierarchy()
                .AsSingle()
                .Lazy();
            Container
                .Bind<CardsViewLoader>()
                .FromComponentInHierarchy()
                .AsSingle()
                .Lazy();
            Container
                .BindInterfacesAndSelfTo<CardInfoPresenter>()
                .AsSingle()
                .NonLazy();
            Container
                .Bind<GraphicRaycaster>()
                .FromComponentInHierarchy()
                .AsSingle();
            Container
                .Bind<EventSystem>()
                .FromComponentInHierarchy()
                .AsSingle();
            Container
                .BindInterfacesAndSelfTo<InputRouter>()
                .AsSingle()
                .NonLazy();
            
            Container.BindInterfacesAndSelfTo<FileAccountStore>().AsSingle().NonLazy();
            Container.Bind<AccountRegistry>().AsSingle();
            Container.Bind<Leaderboard>().AsSingle();
            Container.BindInterfacesAndSelfTo<AccountServerHandler>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<AccountController>().AsSingle().NonLazy();

            Container.Bind<SeatRegistry>().AsSingle();
            Container.Bind<MatchmakingQueue>().AsSingle();
            Container.Bind<MatchStatsRecorder>().AsSingle();
            Container.Bind<MatchJournalRecorder>().AsSingle();
            Container.Bind<MatchJournalFactory>().AsSingle();
            Container.BindInterfacesAndSelfTo<CardStatsStore>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<LobbyManager>().AsSingle();
            Container.BindInterfacesAndSelfTo<LobbyViewModel>().AsSingle();
            Container.BindInterfacesAndSelfTo<LobbySceneController>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<LobbyAudioBinder>().AsSingle().NonLazy();
            
            Container.BindInterfacesAndSelfTo<ServerHandler>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<MatchServerHandler>().AsSingle().NonLazy();
            Container.Bind<MatchServerNotifier>().AsSingle();

            Container.Bind<DeckService>().AsSingle();
            Container.Bind<BoardSyncService>().AsSingle();
            Container.Bind<RedrawService>().AsSingle();
            Container.Bind<RoundService>().AsSingle();
            Container.Bind<TurnService>().AsSingle();
            Container.Bind<CardPlayService>().AsSingle();
            Container.Bind<MatchIntentService>().AsSingle();
            Container.Bind<WeatherService>().AsSingle();

            Container.BindInterfacesAndSelfTo<GameControllerServer>().AsSingle().NonLazy();

            Container.BindInterfacesAndSelfTo<BotProfileProvider>().AsSingle().NonLazy();
            Container.Bind<BotMoveEnumerator>().AsSingle();
            Container.Bind<BotForecast>().AsSingle();
            Container.Bind<BotEvaluator>().AsSingle();
            Container.Bind<BotRoundPolicy>().AsSingle();
            Container.Bind<BotRedrawPolicy>().AsSingle();
            Container.Bind<IBotBrain>().To<BotBrain>().AsSingle();
            Container.BindInterfacesAndSelfTo<BotDirector>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<TutorialDirector>().AsSingle().NonLazy();
            Container
                .Bind<TutorialMenuPresenter>()
                .FromNewComponentOnNewGameObject()
                .AsSingle()
                .NonLazy();
        }
    }
}