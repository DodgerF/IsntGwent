using IsntGwent.Scripts.Cards;
using IsntGwent.Scripts.Decks;
using IsntGwent.Scripts.Decks.Validation;
using IsntGwent.Scripts.Core;
using IsntGwent.Scripts.Audio;
using IsntGwent.Scripts.Lobby.Client;
using IsntGwent.Scripts.Network;
using IsntGwent.Scripts.Vfx;
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
                .BindInterfacesAndSelfTo<KeywordDatabase>()
                .AsSingle()
                .NonLazy();
            Container
                .BindInterfacesAndSelfTo<DeckDatabase>()
                .AsSingle()
                .NonLazy();
            Container
                .BindInterfacesAndSelfTo<DeckRulesProvider>()
                .AsSingle()
                .NonLazy();
            Container
                .Bind<DeckValidator>()
                .AsSingle();
            Container
                .BindInterfacesAndSelfTo<UserDeckStore>()
                .AsSingle()
                .NonLazy();
            Container
                .Bind<DeckSelectService>()
                .AsSingle();
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
            Container
                .BindInterfacesAndSelfTo<VfxDatabase>()
                .AsSingle()
                .NonLazy();
        }
    }
}