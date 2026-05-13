using IsntGwent.Scripts.Lobby.Network;
using IsntGwent.Scripts.Lobby.Services;
using IsntGwent.Scripts.Lobby.UI;
using Zenject;

namespace IsntGwent.Scripts.Installers
{
    public class LobbyInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<LobbyService>().AsSingle();
            Container.Bind<LobbyViewModel>().AsSingle();
            Container.Bind<LobbyNetworkHub>().FromComponentInHierarchy().AsSingle();
            Container.Bind<LobbyManager>().AsSingle();
            
            Container.BindInterfacesAndSelfTo<LobbyServerHandler>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<LobbyClientHandler>().AsSingle().NonLazy();
        }
    }
}