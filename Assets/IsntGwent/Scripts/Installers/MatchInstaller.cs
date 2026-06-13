using IsntGwent.Scripts.Match;
using Zenject;

namespace IsntGwent.Scripts.Installers
{
    public class MatchInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
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