using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Diagnostics;
using IsntGwent.Scripts.Match.Server.Stats;
using IsntGwent.Scripts.Messages;
using Newtonsoft.Json;

namespace IsntGwent.Scripts.Match.Server.Journal
{
    public sealed class MatchJournal : IDisposable
    {
        private static readonly JsonSerializerSettings Settings = new()
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.None,
        };

        public readonly string MatchId;
        public readonly DateTime StartedAt = DateTime.Now;
        public readonly MatchCardTally Tally = new();

        public int Round = 1;
        public int Turn;

        private StreamWriter _json;
        private StreamWriter _text;
        private int _seq;

        private MatchJournal(string matchId, StreamWriter json, StreamWriter text)
        {
            MatchId = matchId;
            _json = json;
            _text = text;
        }

        public static MatchJournal Open(string matchId, string directory)
        {
            if (string.IsNullOrEmpty(directory))
                return new MatchJournal(matchId, null, null);

            try
            {
                Directory.CreateDirectory(directory);

                var json = Create(Path.Combine(directory, matchId + ".jsonl"));
                var text = Create(Path.Combine(directory, matchId + ".log"));

                return new MatchJournal(matchId, json, text);
            }
            catch (Exception e)
            {
                Log.Error(LogTag.Journal, $"failed to open journal {matchId}: {e.Message}");
                return new MatchJournal(matchId, null, null);
            }
        }

        private static StreamWriter Create(string path)
        {
            var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);

            return new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
        }

        public void Start(GameContext context, string lobbyId)
        {
            var p1 = context.Player1;
            var p2 = context.Player2;

            Tally.Deck(p1, p1.Deck.Select(c => c.Definition.Id));
            Tally.Deck(p2, p2.Deck.Select(c => c.Definition.Id));

            Write("match_start", null,
                $"{Kind(context)}: {Who(p1)} [{DeckName(p1)}] vs {Who(p2)} [{DeckName(p2)}]",
                "matchId", MatchId,
                "lobbyId", lobbyId,
                "ranked", context.IsRanked,
                "player1", Profile(p1),
                "player2", Profile(p2));
        }

        public void Deal(GameContext context)
        {
            Hand(context.Player1);
            Hand(context.Player2);

            Write("first_turn", Who(context.CurrentPlayer), $"первый ход: {Who(context.CurrentPlayer)}");
        }

        private void Hand(Player player)
        {
            foreach (var card in player.Hand)
                Tally.Drawn(card.Definition.Id);

            Write("hand", Who(player),
                $"{Who(player)}: стартовая рука {Cards(player.Hand)}",
                "cards", Ids(player.Hand));
        }

        public void Draw(Player player, IReadOnlyList<CardInstance> cards)
        {
            if (cards == null || cards.Count == 0) return;

            foreach (var card in cards)
                Tally.Drawn(card.Definition.Id);

            Write("draw", Who(player),
                $"{Who(player)} добрал {Cards(cards)} (в колоде {player.Deck.Count})",
                "cards", Ids(cards),
                "deckLeft", player.Deck.Count);
        }

        public void RedrawStart(GameContext context)
        {
            Write("redraw_start", null,
                $"мулиган: {Who(context.Player1)} {context.Player1.RedrawsLeft}, " +
                $"{Who(context.Player2)} {context.Player2.RedrawsLeft}");
        }

        public void Redraw(Player player, CardInstance dropped, CardInstance drawn)
        {
            Tally.Redrawn(Id(dropped));
            Tally.Drawn(Id(drawn));

            Write("redraw", Who(player),
                $"{Who(player)} меняет {Title(dropped)} на {Title(drawn)} (осталось {player.RedrawsLeft})",
                "dropped", Id(dropped),
                "drawn", Id(drawn),
                "left", player.RedrawsLeft);
        }

        public void RedrawReady(Player player)
        {
            Write("redraw_ready", Who(player), $"{Who(player)} готов");
        }

        public void RedrawEnd()
        {
            Write("redraw_end", null, "мулиган закончен");
        }

        public void TurnStart(Player player)
        {
            Turn++;

            Write("turn_start", Who(player), $"ход {Who(player)}");
        }

        public void TurnEnd(Player player)
        {
            Write("turn_end", Who(player), $"конец хода {Who(player)}");
        }

        public void Pass(Player player)
        {
            Write("pass", Who(player), $"{Who(player)} пасует");
        }

        public void Play(GameContext context, Player caster, Player boardOwner, CardInstance card,
            RowType row, int slot, IReadOnlyList<string> targets, bool fromPending)
        {
            var place = card is UnitInstance ? $" → {Who(boardOwner)} {Cell(row, slot)}" : string.Empty;
            var aimed = targets == null || targets.Count == 0
                ? string.Empty
                : " по целям " + Names(context, targets);
            var pending = fromPending ? " (отложенная)" : string.Empty;

            Tally.Played(caster, Id(card));

            Write("play", Who(caster),
                $"{Who(caster)} играет {Title(card)}{place}{aimed}{pending}",
                "card", Id(card),
                "instance", Instance(card),
                "owner", Who(boardOwner),
                "row", row.ToString(),
                "slot", slot,
                "targets", targets != null && targets.Count > 0 ? targets.ToArray() : null,
                "fromPending", fromPending ? (object)true : null);
        }

        public void AimRequest(GameContext context, Player caster, CardInstance card, IReadOnlyList<string> pool)
        {
            Write("aim_request", Who(caster),
                $"{Title(card)} просит цель, пул: {Names(context, pool)}",
                "card", Id(card),
                "pool", pool?.ToArray());
        }

        public void Aim(GameContext context, Player caster, CardInstance card, IReadOnlyList<string> targets)
        {
            Write("aim", Who(caster),
                $"{Who(caster)} выбрал {Names(context, targets)} для {Title(card)}",
                "card", Id(card),
                "targets", targets?.ToArray());
        }

        public void Damage(GameContext context, CardInstance source, UnitInstance target, int amount,
            DamageKind kind)
        {
            Tally.Damage(Id(source), amount);

            Write("damage", null,
                $"{Title(source)} бьёт {Title(target)} {Where(context, target)} на {amount} ({kind})" +
                $" → сила {target.CurrentPower.Value}, броня {target.Armor.Value}",
                "source", Id(source),
                "target", Id(target),
                "targetInstance", Instance(target),
                "amount", amount,
                "kind", kind.ToString(),
                "power", target.CurrentPower.Value);
        }

        public void Kill(GameContext context, CardInstance source, UnitInstance target)
        {
            Write("kill", null,
                $"{Title(source)} уничтожает {Title(target)} {Where(context, target)}",
                "source", Id(source),
                "target", Id(target),
                "targetInstance", Instance(target));
        }

        public void Link(GameContext context, CardInstance source, UnitInstance target, UnitLinkKind kind)
        {
            Write("link", null,
                $"{Title(source)}: {kind} на {Title(target)} {Where(context, target)}",
                "source", Id(source),
                "target", Id(target),
                "kind", kind.ToString());
        }

        public void Death(Player owner, UnitInstance unit, RowType row, int slot, CardInstance killer)
        {
            var by = killer == null ? string.Empty : $", убил {Title(killer)}";

            Tally.Died(Id(unit));
            Tally.Killed(Id(killer));

            Write("death", Who(owner),
                $"{Title(unit)} ({Who(owner)} {Cell(row, slot)}) погибает{by}",
                "unit", Id(unit),
                "instance", Instance(unit),
                "owner", Who(owner),
                "row", row.ToString(),
                "slot", slot,
                "killer", Id(killer));
        }

        public void Summon(GameContext context, Player owner, UnitInstance unit, bool fromDeck)
        {
            var from = fromDeck ? " из колоды" : string.Empty;

            Tally.Summoned(Id(unit));

            Write("summon", Who(owner),
                $"{Who(owner)} призывает {Title(unit)} {Where(context, unit)}{from}",
                "unit", Id(unit),
                "instance", Instance(unit),
                "owner", Who(owner),
                "fromDeck", fromDeck);
        }

        public void Move(Player owner, UnitInstance unit, RowType fromRow, int fromSlot, RowType toRow, int toSlot)
        {
            Write("move", Who(owner),
                $"{Title(unit)} едет {Cell(fromRow, fromSlot)} → {Cell(toRow, toSlot)} ({Who(owner)})",
                "unit", Id(unit),
                "instance", Instance(unit),
                "owner", Who(owner),
                "fromRow", fromRow.ToString(),
                "fromSlot", fromSlot,
                "toRow", toRow.ToString(),
                "toSlot", toSlot);
        }

        public void Devour(GameContext context, Player owner, UnitInstance consumer, UnitInstance consumed)
        {
            Write("devour", Who(owner),
                $"{Title(consumer)} жрёт {Title(consumed)} {Where(context, consumed)}",
                "consumer", Id(consumer),
                "consumed", Id(consumed),
                "owner", Who(owner));
        }

        public void Weather(Player owner, RowType row, CardInstance source)
        {
            Write("weather", Who(owner),
                $"погода {Title(source)} на ряду {row} игрока {Who(owner)}",
                "card", Id(source),
                "owner", Who(owner),
                "row", row.ToString());
        }

        public void WeatherCleared(Player owner)
        {
            Write("weather_clear", Who(owner), $"погода снята у {Who(owner)}");
        }

        public void States(GameContext context, IReadOnlyList<UnitInstance> changed)
        {
            if (changed == null || changed.Count == 0) return;

            var parts = changed
                .Select(u => $"{Title(u)} {Where(context, u)} → {u.CurrentPower.Value}/{u.BasePower.Value}" +
                             (u.Armor.Value > 0 ? $" +{u.Armor.Value}бр" : string.Empty))
                .ToArray();

            var units = changed
                .Select(u => new Dictionary<string, object>
                {
                    { "card", Id(u) },
                    { "instance", Instance(u) },
                    { "power", u.CurrentPower.Value },
                    { "base", u.BasePower.Value },
                    { "armor", u.Armor.Value },
                })
                .ToArray();

            Write("states", null, "изменились: " + string.Join(", ", parts), "units", units);
        }

        public void RoundEnd(GameContext context, Player winner, bool isTie, int power1, int power2)
        {
            var p1 = context.Player1;
            var p2 = context.Player2;
            var outcome = isTie ? "ничья" : $"побеждает {Who(winner)}";

            Write("round_end", Who(winner),
                $"раунд {Round}: {Who(p1)} {power1} : {power2} {Who(p2)} — {outcome}; hp {p1.Hp} : {p2.Hp}",
                "winner", Who(winner),
                "tie", isTie,
                "power1", power1,
                "power2", power2,
                "hp1", p1.Hp,
                "hp2", p2.Hp);

            Round++;
            Turn = 0;
        }

        public void Rejected(Player player, string intent, string error, string details)
        {
            var tail = string.IsNullOrEmpty(details) ? string.Empty : " — " + details;

            Write("rejected", Who(player),
                $"{intent} отклонён: {error}{tail}",
                "intent", intent,
                "error", error,
                "details", details);
        }

        public void Pause(Player player)
        {
            Write("pause", Who(player), $"{Who(player)} потерял связь, партия на паузе");
        }

        public void Resume(Player player)
        {
            Write("resume", Who(player), $"{Who(player)} вернулся");
        }

        public void End(GameContext context, string reason)
        {
            var p1 = context.Player1;
            var p2 = context.Player2;
            var outcome = context.IsTie ? "ничья" : $"победил {Who(context.Winner)}";
            var seconds = (DateTime.Now - StartedAt).TotalSeconds;

            Write("match_end", Who(context.Winner),
                $"партия окончена ({reason}): {outcome}; hp {p1.Hp} : {p2.Hp}; " +
                $"раундов {Round}; длилась {seconds:F0} c",
                "reason", reason,
                "winner", Who(context.Winner),
                "tie", context.IsTie,
                "hp1", p1.Hp,
                "hp2", p2.Hp,
                "rounds", Round,
                "seconds", (long)seconds,
                "points1", Points(p1),
                "points2", Points(p2));
        }

        private void Write(string type, string actor, string text, params object[] data)
        {
            if (_json == null && _text == null) return;

            var now = DateTime.Now;

            var entry = new MatchJournalEvent
            {
                Seq = ++_seq,
                Time = now.ToString("O"),
                Ms = (long)(now - StartedAt).TotalMilliseconds,
                Round = Round,
                Turn = Turn,
                Type = type,
                Actor = actor,
                Text = text,
                Data = Pack(data),
            };

            try
            {
                _json?.WriteLine(JsonConvert.SerializeObject(entry, Settings));
                _text?.WriteLine(Render(entry));
            }
            catch (Exception e)
            {
                Break(e);
            }
        }

        private static string Render(MatchJournalEvent entry)
        {
            var span = TimeSpan.FromMilliseconds(entry.Ms);

            return $"{span:mm\\:ss\\.fff} #{entry.Seq,-4} r{entry.Round}/t{entry.Turn} {entry.Type,-13} {entry.Text}";
        }

        private static Dictionary<string, object> Pack(object[] data)
        {
            if (data == null || data.Length < 2) return null;

            var result = new Dictionary<string, object>();

            for (var i = 0; i + 1 < data.Length; i += 2)
            {
                var key = data[i] as string;
                var value = data[i + 1];

                if (string.IsNullOrEmpty(key) || value == null) continue;

                result[key] = value;
            }

            return result.Count == 0 ? null : result;
        }

        private void Break(Exception exception)
        {
            Log.Error(LogTag.Journal, $"journal {MatchId} stopped: {exception.Message}");

            Dispose();
        }

        private static Dictionary<string, object> Profile(Player player)
        {
            return new Dictionary<string, object>
            {
                { "nickname", Who(player) },
                { "accountId", player?.Seat?.Account?.Id },
                { "seatId", player?.Seat?.Id },
                { "deck", DeckName(player) },
                { "cards", player?.Deck?.Select(c => c.Definition.Id).OrderBy(id => id).ToArray() },
            };
        }

        private static object Points(Player player)
        {
            var account = player?.Seat?.Account;

            return account?.Points;
        }

        private static string Kind(GameContext context) => context.IsRanked ? "рейтинг" : "приватная комната";

        private static string DeckName(Player player)
        {
            var deck = player?.Seat?.Deck;

            return string.IsNullOrEmpty(deck?.Name) ? "?" : deck.Name;
        }

        private static string Who(Player player)
        {
            var nickname = player?.Seat?.Account?.Nickname;

            return string.IsNullOrEmpty(nickname) ? null : nickname;
        }

        private static string Title(CardInstance card)
        {
            return card == null ? "?" : $"{card.Definition.Name} ({card.Definition.Id})";
        }

        private static string Id(CardInstance card) => card?.Definition.Id;

        private static string Instance(CardInstance card) => card?.Id.ToString();

        private static string Cell(RowType row, int slot) => slot < 0 ? row.ToString() : $"{row}[{slot}]";

        private static string Cards(IReadOnlyList<CardInstance> cards)
        {
            return cards == null || cards.Count == 0 ? "ничего" : string.Join(", ", cards.Select(Title));
        }

        private static string[] Ids(IReadOnlyList<CardInstance> cards)
        {
            return cards?.Select(c => c.Definition.Id).ToArray();
        }

        private static string Where(GameContext context, UnitInstance unit)
        {
            if (unit == null) return string.Empty;

            var slot = context?.FindSlot(unit);

            return slot == null ? "(вне стола)" : $"({Who(slot.Owner)} {Cell(slot.Row, slot.Index)})";
        }

        private static string Names(GameContext context, IReadOnlyList<string> ids)
        {
            if (ids == null || ids.Count == 0) return "нет";

            return string.Join(", ", ids.Select(id => Title(Find(context, id))));
        }

        private static CardInstance Find(GameContext context, string instanceId)
        {
            if (context == null || string.IsNullOrEmpty(instanceId)) return null;

            foreach (var player in new[] { context.Player1, context.Player2 })
            {
                foreach (var unit in player.MeleeRow.Concat(player.RangedRow))
                {
                    if (unit.Id.ToString() == instanceId) return unit;
                }

                foreach (var card in player.Hand.Concat(player.Graveyard))
                {
                    if (card.Id.ToString() == instanceId) return card;
                }
            }

            return null;
        }

        public void Dispose()
        {
            try
            {
                _json?.Dispose();
                _text?.Dispose();
            }
            catch (Exception)
            {
            }

            _json = null;
            _text = null;
        }
    }
}
