using IsntGwent.Scripts.Cards;
using IsntGwent.Scripts.Cards.Services;
using IsntGwent.Scripts.Lobby;
using IsntGwent.Scripts.Lobby.Network;
using IsntGwent.Scripts.Lobby.UI;
using IsntGwent.Scripts.Match;
using IsntGwent.Scripts.Server;
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
                .BindInterfacesAndSelfTo<CardPreviewService>()
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
            Container.Bind<DeckSelectService>().AsSingle();
            Container.BindInterfacesAndSelfTo<LobbyManager>().AsSingle();
            Container.BindInterfacesAndSelfTo<LobbyViewModel>().AsSingle();
            Container.BindInterfacesAndSelfTo<LobbySceneController>().AsSingle().NonLazy();
            
            Container.BindInterfacesAndSelfTo<ServerHandler>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<LobbyClientHandler>().AsSingle();
            Container.BindInterfacesAndSelfTo<MathServerHandler>().AsSingle().NonLazy();
        }
    }
}