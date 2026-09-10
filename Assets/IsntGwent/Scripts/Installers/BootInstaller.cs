using IsntGwent.Scripts.Network;
using Zenject;

namespace IsntGwent.Scripts.Installers
{
    public class BootInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container
                .BindInterfacesAndSelfTo<BootController>()
                .AsSingle()
                .NonLazy();
        }
    }
}
