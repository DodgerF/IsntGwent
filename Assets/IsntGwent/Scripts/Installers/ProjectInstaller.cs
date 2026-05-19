using Zenject;

namespace IsntGwent.Scripts.Installers
{
    public class ProjectInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container
                .Bind<SessionService>()
                .AsSingle()
                .NonLazy();
            Container
                .BindInterfacesAndSelfTo<SceneController>()
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