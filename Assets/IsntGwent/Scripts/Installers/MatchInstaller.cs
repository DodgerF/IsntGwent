using IsntGwent.Scripts.Cards.Server;
using IsntGwent.Scripts.Cards.Client;
using IsntGwent.Scripts.Decks.UI;
using IsntGwent.Scripts.Match.Client;
using UnityEngine.EventSystems;
using IsntGwent.Scripts.Core;
using IsntGwent.Scripts.Audio;
using IsntGwent.Scripts.Vfx;
using UnityEngine.UI;
using Zenject;

namespace IsntGwent.Scripts.Installers
{
    public class MatchInstaller : MonoInstaller
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
                .Bind<CardPreviewService>()
                .AsSingle();
            Container
                .Bind<BoardTargetQuery>()
                .AsSingle();
            Container
                .Bind<PlayPreviewQuery>()
                .AsSingle();
            Container
                .Bind<ClientConditionQuery>()
                .AsSingle();
            Container
                .BindInterfacesAndSelfTo<CardSelectionService>()
                .AsSingle()
                .NonLazy();
            Container
                .Bind<CardTooltipView>()
                .FromComponentInHierarchy()
                .AsSingle()
                .Lazy();
            Container
                .BindInterfacesAndSelfTo<CardInfoPresenter>()
                .AsSingle()
                .NonLazy();
            Container
                .Bind<CardViewRegistry>()
                .AsSingle();
            Container
                .Bind<CardInstanceRegistry>()
                .AsSingle();
            Container
                .BindInterfacesAndSelfTo<TargetHighlightPresenter>()
                .AsSingle()
                .NonLazy();
            Container
                .Bind<MatchState>()
                .AsSingle();
            Container
                .BindInterfacesAndSelfTo<AnimationCoordinator>()
                .AsSingle();
            Container
                .BindInterfacesAndSelfTo<GameControllerClient>()
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
            
            Container
                .BindInterfacesAndSelfTo<MatchClientHandler>()
                .AsSingle();

            Container
                .Bind<VfxLayer>()
                .FromComponentInHierarchy()
                .AsSingle();
            Container
                .Bind<VfxService>()
                .AsSingle();

            Container
                .BindInterfacesAndSelfTo<MatchAudioBinder>()
                .AsSingle()
                .NonLazy();
        }
    }
}