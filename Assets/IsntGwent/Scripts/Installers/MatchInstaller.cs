using IsntGwent.Scripts.Cards;
using IsntGwent.Scripts.Cards.Services;
using IsntGwent.Scripts.Match;
using UnityEngine.EventSystems;
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
                .BindInterfacesAndSelfTo<CardPreviewService>()
                .AsSingle()
                .NonLazy();
            Container
                .BindInterfacesAndSelfTo<CardSelectionService>()
                .AsSingle()
                .NonLazy();
            Container
                .Bind<MatchState>()
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
                .BindInterfacesAndSelfTo<MatchViewModel>()
                .AsSingle()
                .NonLazy();
        }
    }
}