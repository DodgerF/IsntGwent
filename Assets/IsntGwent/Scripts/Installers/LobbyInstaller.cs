using IsntGwent.Scripts.Audio;
using IsntGwent.Scripts.Cards.Server;
using IsntGwent.Scripts.Cards.Client;
using IsntGwent.Scripts.Cards.UI;
using IsntGwent.Scripts.Content;
using IsntGwent.Scripts.Network.Http;
using IsntGwent.Scripts.Lobby.Client;
using IsntGwent.Scripts.Lobby.Network;
using IsntGwent.Scripts.Lobby.UI;
using IsntGwent.Scripts.Core;
using IsntGwent.Scripts.Decks.UI;
using IsntGwent.Scripts.Match.Server;
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
                .Bind<ContentService>()
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
            
            Container.Bind<LobbyNetworkHub>().FromComponentInHierarchy().AsSingle();
            Container.Bind<SeatRegistry>().AsSingle();
            Container.BindInterfacesAndSelfTo<LobbyManager>().AsSingle();
            Container.BindInterfacesAndSelfTo<LobbyViewModel>().AsSingle();
            Container.BindInterfacesAndSelfTo<LobbySceneController>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<LobbyAudioBinder>().AsSingle().NonLazy();
            
            Container.BindInterfacesAndSelfTo<ServerHandler>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<LobbyClientHandler>().AsSingle();
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

            Container
                .Bind<HttpGateway>()
                .FromNewComponentOnNewGameObject()
                .AsSingle()
                .NonLazy();
        }
    }
}