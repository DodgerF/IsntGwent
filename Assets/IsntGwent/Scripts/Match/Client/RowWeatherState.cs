using IsntGwent.Scripts.Messages;
using UniRx;

namespace IsntGwent.Scripts.Match.Client
{
    public class RowWeatherState
    {
        public readonly ReactiveProperty<string> CardId = new(string.Empty);

        public void Apply(RowStatusData data)
        {
            CardId.Value = data.IsEmpty ? string.Empty : data.CardId;
        }

        public void Clear()
        {
            CardId.Value = string.Empty;
        }
    }
}
