using IsntGwent.Scripts.Cards;
using IsntGwent.Scripts.Decks;
using Zenject;

namespace IsntGwent.Scripts.Installers
{
    public class ProjectInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container
                .BindInterfacesAndSelfTo<SceneService>()
                .AsSingle()
                .NonLazy();
            
            Container.BindInterfacesAndSelfTo<CardDatabase>()
                .AsSingle()
                .NonLazy();
            Container.BindInterfacesAndSelfTo<DeckDatabase>()
                .AsSingle() 
                .NonLazy();
        }
    }
}