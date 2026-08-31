using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Cards.Server;
using UniRx;
using Zenject;

namespace IsntGwent.Scripts.Match.Server
{
    public class WeatherService
    {
        [Inject] private readonly CardResolver _cardResolver;
        [Inject] private readonly BoardSyncService _boardSync;

        public void Attach(GameContext context)
        {
            context.AddDisposable(context.Events.Stream.Subscribe(gameEvent =>
            {
                switch (gameEvent)
                {
                    case CardPlayed played:
                        Enter(context, played.Card as UnitInstance, played.Owner, gameEvent);
                        break;

                    case UnitSummoned summoned:
                        Enter(context, summoned.Unit, summoned.Owner, gameEvent);
                        break;

                    case UnitMoved moved when moved.FromRow != moved.ToRow:
                        Enter(context, moved.Unit, moved.Owner, gameEvent);
                        break;
                }
            }));
        }

        public static void Clear(Player player)
        {
            player.Weather.Clear();
        }

        private void Enter(GameContext context, UnitInstance unit, Player owner, IGameEvent gameEvent)
        {
            if (unit == null || owner == null) return;

            var weather = owner.GetWeather(unit.RowType);
            if (weather?.Source == null) return;
            if (owner.GetRow(unit.RowType).SlotOf(unit) == null) return;

            if (context.FlushBoardDirty())
                _boardSync.SyncBoard(context);

            _cardResolver.RunEffects(context, owner, weather.Source,
                EffectTrigger.OnWeatherEnter, gameEvent, null, unit.RowType);

            _boardSync.Sync(context);

            if (context.FlushBoardDirty())
                _boardSync.SyncBoard(context);
        }
    }
}
