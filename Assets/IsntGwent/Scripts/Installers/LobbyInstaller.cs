using IsntGwent.Scripts.Cards.Services;
using IsntGwent.Scripts.Lobby;
using IsntGwent.Scripts.Lobby.Network;
using IsntGwent.Scripts.Lobby.UI;
using IsntGwent.Scripts.Server;
using Zenject;

namespace IsntGwent.Scripts.Installers
{
    public class LobbyInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<CardPreviewService>().AsSingle();
            
            Container.Bind<LobbyStore>().AsSingle();
            Container.Bind<DeckSelectService>().AsSingle();
            Container.Bind<LobbyNetworkHub>().FromComponentInHierarchy().AsSingle();
            Container.Bind<LobbyManager>().AsSingle();
            Container.BindInterfacesAndSelfTo<LobbyViewModel>().AsSingle();
            Container.BindInterfacesAndSelfTo<LobbySceneController>().AsSingle().NonLazy();
            
            Container.BindInterfacesAndSelfTo<ServerHandler>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<LobbyClientHandler>().AsSingle();
        }
    }
}