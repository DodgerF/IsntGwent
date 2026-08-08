using IsntGwent.Scripts.Core;
using IsntGwent.Scripts.Decks;
using IsntGwent.Scripts.Decks.UI;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zenject;

namespace IsntGwent.Scripts.Installers
{
    public class DeckBuilderInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<GraphicRaycaster>().FromComponentInHierarchy().AsSingle();
            Container.Bind<EventSystem>().FromComponentInHierarchy().AsSingle();
            Container.BindInterfacesAndSelfTo<InputRouter>().AsSingle().NonLazy();

            Container.Bind<ConfirmWindow>().FromComponentInHierarchy().AsSingle();
            Container.Bind<CardTooltipView>().FromComponentInHierarchy().AsSingle();
            Container.Bind<DeckDraft>().AsSingle();
            Container.BindInterfacesAndSelfTo<DeckBuilderSceneController>().AsSingle().NonLazy();
        }
    }
}
