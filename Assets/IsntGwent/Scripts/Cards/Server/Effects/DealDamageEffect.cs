using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;

namespace IsntGwent.Scripts.Cards.Server.Effects
{
    public class DealDamageEffect : CardEffectBase
    {
        public override void Execute(EffectContext context)
        {
            var definition = (DealDamageEffectDefinition)context.Definition;

            if (definition.Self)
            {
                Hit(context, context.Source as UnitInstance, definition.Amount);
                return;
            }

            foreach (var target in context.Targets)
                Hit(context, target, definition.Amount);

            var missed = MissedTargets(definition, context.Targets.Count);
            if (missed <= 0) return;

            Hit(context, context.Source as UnitInstance, definition.Amount * missed);
        }

        private static int MissedTargets(DealDamageEffectDefinition definition, int hitCount)
        {
            if (definition.SelfPerMissingTarget)
                return definition.ExpectedTargets - hitCount;

            return definition.SelfIfNoTargets && hitCount == 0 ? 1 : 0;
        }

        private static void Hit(EffectContext context, UnitInstance target, int amount)
        {
            if (target == null || amount <= 0) return;

            context.Game.RecordDamage(context.Source, target, amount, context.DamageKind);
            target.GetDamage(amount);
        }
    }

    public class DealDamageEffectDefinition : EffectDefinition
    {
        public int Amount;
        public bool Self;
        public bool SelfIfNoTargets;
        public bool SelfPerMissingTarget;
        public int ExpectedTargets;
    }
}
