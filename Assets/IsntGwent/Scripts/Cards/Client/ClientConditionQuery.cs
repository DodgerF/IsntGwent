using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Match.Client;
using Zenject;

namespace IsntGwent.Scripts.Cards.Client
{
    public class ClientConditionQuery
    {
        [Inject] private readonly MatchState _state;

        public bool IsMet(EffectDefinition definition)
        {
            var condition = definition.Condition;
            if (condition == null) return true;

            if (!string.IsNullOrEmpty(condition.AllyOnBoard))
            {
                if (HasOnBoard(condition.AllyOnBoard) == condition.AllyAbsent) return false;
            }

            if (!string.IsNullOrEmpty(condition.Environment))
            {
                if (HasEnvironment(condition.Environment) == condition.EnvironmentAbsent) return false;
            }

            if (condition.Leadership != LeadershipMode.Any)
            {
                var held = MaxPower(OwnUnits()) >= MaxPower(EnemyUnits());
                if (held != (condition.Leadership == LeadershipMode.Held)) return false;
            }

            return true;
        }

        private bool HasEnvironment(string cardId)
        {
            return HasWeather(_state.EnemyMeleeWeather, cardId) || HasWeather(_state.EnemyRangedWeather, cardId);
        }

        private static bool HasWeather(RowWeatherState weather, string cardId)
        {
            return weather.CardId.Value == cardId;
        }

        private bool HasOnBoard(string definitionId)
        {
            foreach (var card in OwnUnits())
            {
                if (card.Definition.Id == definitionId) return true;
            }

            return false;
        }

        private IEnumerable<CardInstance> OwnUnits()
        {
            foreach (var card in _state.OwnMeleeRow.Units) yield return card;
            foreach (var card in _state.OwnRangedRow.Units) yield return card;
        }

        private IEnumerable<CardInstance> EnemyUnits()
        {
            foreach (var card in _state.EnemyMeleeRow.Units) yield return card;
            foreach (var card in _state.EnemyRangedRow.Units) yield return card;
        }

        private static int MaxPower(IEnumerable<CardInstance> cards)
        {
            var max = 0;

            foreach (var card in cards)
            {
                if (card is UnitInstance unit && unit.CurrentPower.Value > max)
                    max = unit.CurrentPower.Value;
            }

            return max;
        }
    }
}
