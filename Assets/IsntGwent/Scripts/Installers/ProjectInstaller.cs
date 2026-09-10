using IsntGwent.Scripts.Cards;
using IsntGwent.Scripts.Decks;
using IsntGwent.Scripts.Decks.Validation;
using IsntGwent.Scripts.Core;
using IsntGwent.Scripts.Audio;
using IsntGwent.Scripts.Diagnostics;
using IsntGwent.Scripts.Accounts.Client;
using IsntGwent.Scripts.Lobby.Client;
using IsntGwent.Scripts.Localization;
using IsntGwent.Scripts.Network;
using IsntGwent.Scripts.Tutorial;
using IsntGwent.Scripts.Tutorial.Client;
using IsntGwent.Scripts.Tutorial.UI;
using IsntGwent.Scripts.Vfx;
using Zenject;

namespace IsntGwent.Scripts.Installers
{
    public class ProjectInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container
                .BindInterfacesAndSelfTo<LogService>()
                .AsSingle()
                .NonLazy();

            Container.Bind<PlaySession>().AsSingle();
            Container
                .BindInterfacesAndSelfTo<LobbyClientHandler>()
                .AsSingle()
                .NonLazy();
            Container
                .BindInterfacesAndSelfTo<AccountClientHandler>()
                .AsSingle()
                .NonLazy();
            Container
                .BindInterfacesAndSelfTo<PlayerAccount>()
                .AsSingle()
                .NonLazy();
            Container
                .BindInterfacesAndSelfTo<TutorialScriptProvider>()
                .AsSingle()
                .NonLazy();
            Container
                .BindInterfacesAndSelfTo<TutorialService>()
                .AsSingle()
                .NonLazy();

            
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
            Container
                .Bind<TutorialDebugHud>()
                .FromNewComponentOnNewGameObject()
                .AsSingle()
                .NonLazy();
#endif
            Container
                .BindInterfacesAndSelfTo<LocalizationService>()
                .AsSingle()
                .NonLazy();
            Container
                .Bind<LocalizationBinder>()
                .FromNewComponentOnNewGameObject()
                .AsSingle()
                .NonLazy();
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