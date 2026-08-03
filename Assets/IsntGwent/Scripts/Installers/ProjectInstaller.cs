using IsntGwent.Scripts.Cards;
using IsntGwent.Scripts.Decks;
using IsntGwent.Scripts.Core;
using IsntGwent.Scripts.Audio;
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
                .BindInterfacesAndSelfTo<MatchReconnectService>()
                .AsSingle()
                .NonLazy();
            Container
                .Bind<CoroutineRunner>()
                .FromNewComponentOnNewGameObject()
                .AsSingle();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Container
                .Bind<NetDebugHud>()
                .FromNewComponentOnNewGameObject()
                .AsSingle()
                .NonLazy();
#endif
            Container
                .BindInterfacesAndSelfTo<CardDatabase>()
                .AsSingle()
                .NonLazy();
            Container
                .BindInterfacesAndSelfTo<DeckDatabase>()
                .AsSingle()
                .NonLazy();
            Container
                .Bind<AudioPlayer>()
                .FromNewComponentOnNewGameObject()
                .AsSingle();
            Container
                .BindInterfacesAndSelfTo<SoundDatabase>()
                .AsSingle()
                .NonLazy();
            Container
                .BindInterfacesAndSelfTo<SettingsService>()
                .AsSingle()
                .NonLazy();
            Container
                .BindInterfacesAndSelfTo<AudioService>()
                .AsSingle()
                .NonLazy();
        }
    }
}