using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Cards.Server;
using IsntGwent.Scripts.Match;
using Zenject;

namespace IsntGwent.Scripts.Match.Server.Bot
{
    public class BotMoveEnumerator
    {
        [Inject] private readonly CardResolver _cardResolver;

        private static readonly RowType[] Rows = { RowType.Melee, RowType.Ranged };
        private static readonly List<string> NoTargets = new();

        public List<BotMove> Enumerate(GameContext context, Player me)
        {
            var result = new List<BotMove>();
            if (context == null || me == null) return result;

            foreach (var card in Playable(me))
            {
                var boardOwner = CardPlayService.IsTraitor(card) ? context.GetOpponent(me) : me;

                if (card is UnitInstance)
                    AddUnitMoves(context, me, boardOwner, card, result);
                else
                    AddSpellMoves(context, me, boardOwner, card, result);
            }

            return result;
        }

        public static IReadOnlyList<CardInstance> Playable(Player me)
        {
            if (me.PendingPlays.Count > 0)
                return new List<CardInstance> { me.NextPendingPlay };

            return me.Hand;
        }

        private void AddUnitMoves(GameContext context, Player me, Player boardOwner, CardInstance card,
            List<BotMove> result)
        {
            foreach (var row in Rows)
            {
                var slots = boardOwner.GetRow(row).Slots;

                for (var i = 0; i < slots.Length; i++)
                {
                    if (!slots[i].IsEmpty) continue;
                    if (!_cardResolver.CanPlay(context, boardOwner, card, NoTargets, row, i)) continue;

                    result.Add(BotMove.Play(card, row, i, false, null));
                }
            }
        }

        private void AddSpellMoves(GameContext context, Player me, Player boardOwner, CardInstance card,
            List<BotMove> result)
        {
            if (!_cardResolver.TargetsAnyRow(card.Definition))
            {
                if (_cardResolver.CanPlay(context, boardOwner, card, NoTargets, RowType.Melee, -1))
                    result.Add(BotMove.Play(card, RowType.Melee, -1, false, null));

                return;
            }

            foreach (var row in Rows)
            {
                if (!_cardResolver.CanPlay(context, boardOwner, card, NoTargets, row, -1)) continue;

                result.Add(BotMove.Play(card, row, -1, false, null));
                result.Add(BotMove.Play(card, row, -1, true, null));
            }
        }
    }
}
