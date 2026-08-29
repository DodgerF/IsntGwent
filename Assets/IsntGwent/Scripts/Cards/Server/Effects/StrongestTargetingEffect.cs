namespace IsntGwent.Scripts.Cards.Server.Effects
{
    public class StrongestTargetingEffect : PowerTargetingEffect
    {
        protected override bool PickHighest => true;
    }

    public class StrongestTargetingDefinition : PowerTargetingDefinition
    {
    }
}
