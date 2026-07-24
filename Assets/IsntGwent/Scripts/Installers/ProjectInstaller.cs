using IsntGwent.Scripts.Cards;
using IsntGwent.Scripts.Decks;
using IsntGwent.Scripts.Core;
using IsntGwent.Scripts.Lobby.Client;
using IsntGwent.Scripts.Network;
using Zenject;

namespace IsntGwent.Scripts.Installers
{
    public class ProjectInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<LobbyStore>().AsSingle();
            
            Container
                .BindInterfacesAndSelfTo<SceneService>()
                .AsSingle()
                .NonLazy();
            Container
                .BindInterfacesAndSelfTo<ConnectionService>()
                .AsSingle()
                .NonLazy();
            Container
                .Bind<CoroutineRunner>()
                .FromNewComponentOnNewGameObject()
                .AsSingle();
            Container
                .BindInterfacesAndSelfTo<CardDatabase>()
                .AsSingle()
                .NonLazy();
            Container
                .BindInterfacesAndSelfTo<DeckDatabase>()
                .AsSingle() 
                .NonLazy();
        }
    }
}