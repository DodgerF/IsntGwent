using IsntGwent.Scripts.Messages;
using UniRx;

namespace IsntGwent.Scripts.Match.Client
{
    public class RowWeatherState
    {
        public readonly ReactiveProperty<string> CardId = new(string.Empty);
        public readonly ReactiveProperty<int> TurnsLeft = new(0);

        public void Apply(RowStatusData data)
        {
            CardId.Value = data.IsEmpty ? string.Empty : data.CardId;
            TurnsLeft.Value = data.IsEmpty ? 0 : data.TurnsLeft;
        }

        public void Clear()
        {
            CardId.Value = string.Empty;
            TurnsLeft.Value = 0;
        }
    }
}
