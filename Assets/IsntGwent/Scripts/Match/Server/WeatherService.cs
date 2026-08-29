using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Server;
using UniRx;
using Zenject;

namespace IsntGwent.Scripts.Match.Server
{
    public class WeatherService
    {
        [Inject] private readonly CardResolver _cardResolver;
        [Inject] private readonly BoardSyncService _boardSync;

        private static readonly RowType[] Rows = { RowType.Melee, RowType.Ranged };

        public void Attach(GameContext context)
        {
            context.AddDisposable(context.Events.Stream.Subscribe(gameEvent =>
            {
                if (gameEvent is TurnEnded turnEnded)
                    Tick(context, turnEnded.Player);
            }));
        }

        public static void Clear(Player player)
        {
            player.Weather.Clear();
        }

        private void Tick(GameContext context, Player player)
        {
            if (player == null || player.Weather.Count == 0) return;

            var ticked = false;

            foreach (var row in Rows)
            {
                var weather = player.GetWeather(row);
                if (weather == null || weather.TurnsLeft <= 0) continue;

                _cardResolver.RunEffects(context, player, weather.Source,
                    EffectTrigger.OnWeatherTick, null, null, row);

                weather.TurnsLeft--;
                if (weather.TurnsLeft <= 0) player.Weather.Remove(row);

                ticked = true;
                _boardSync.Sync(context);
            }

            if (ticked) _boardSync.SyncBoard(context);
        }
    }
}
