namespace IsntGwent.Scripts.Cards.Server.Effects
{
    public class WeakestTargetingEffect : PowerTargetingEffect
    {
        protected override bool PickHighest => false;
    }

    public class WeakestTargetingDefinition : PowerTargetingDefinition
    {
    }
}
