using System.Collections.Generic;
using System.Linq;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Match.Server;
using IsntGwent.Scripts.Messages;

namespace IsntGwent.Scripts.Cards.Server.Effects
{
    public class DevourEffect : CardEffectBase
    {
        public override void Execute(EffectContext context)
        {
            var definition = (DevourDefinition)context.Definition;

            var consumer = definition.ConsumerIsManualTarget
                ? context.ManualTargets.FirstOrDefault()
                : context.Source as UnitInstance;

            if (consumer == null) return;

            foreach (var target in ResolveMeals(context, definition, consumer))
            {
                var power = target.CurrentPower.Value;

                if (definition.TakeSlot)
                    context.Game.RequestSlotTakeover(consumer, context.Game.FindSlot(target));

                if (definition.AsArmor)
                    consumer.Armor.Value += power;
                else
                    consumer.CurrentPower.Value += power;

                context.Game.RecordLink(consumer, target, UnitLinkKind.Devour);
                context.Game.Publish(new UnitDevoured(target, consumer, context.Owner));

                context.Game.RecordKill(context.Source, target);
                target.Kill();
            }
        }

        private static IEnumerable<UnitInstance> ResolveMeals(EffectContext context,
            DevourDefinition definition, UnitInstance consumer)
        {
            IEnumerable<UnitInstance> meals;

            if (definition.RowOfConsumer)
            {
                meals = context.Owner.MeleeRow.Concat(context.Owner.RangedRow)
                    .Where(u => u != consumer && u.RowType == consumer.RowType);
            }
            else
            {
                meals = context.Targets.Where(u => u != consumer);
            }

            if (definition.MaxTargetPower > 0)
                meals = meals.Where(u => u.CurrentPower.Value <= definition.MaxTargetPower);

            return meals.ToList();
        }
    }

    public class DevourDefinition : EffectDefinition
    {
        public int MaxTargetPower;
        public bool ConsumerIsManualTarget;
        public bool RowOfConsumer;
        public bool AsArmor;
        public bool TakeSlot;
    }
}
