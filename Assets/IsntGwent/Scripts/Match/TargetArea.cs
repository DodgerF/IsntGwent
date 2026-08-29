using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Server.Effects;

namespace IsntGwent.Scripts.Match
{
    public readonly struct TargetArea
    {
        public readonly int MinRange;
        public readonly int MaxRange;
        public readonly int Width;
        public readonly RowType Row;
        public readonly bool ExcludeAnchor;
        public readonly bool SameLine;

        public bool IsUnlimited => MinRange <= 0 && MaxRange <= 0 && Width <= 0 && !ExcludeAnchor && !SameLine;

        public bool NeedsAnchor => !IsUnlimited;

        public TargetArea(int minRange, int maxRange, int width, RowType row, bool excludeAnchor, bool sameLine)
        {
            MinRange = minRange;
            MaxRange = maxRange;
            Width = width;
            Row = row;
            ExcludeAnchor = excludeAnchor;
            SameLine = sameLine;
        }

        public static TargetArea From(TargetingEffectDefinition definition)
            => new(definition.MinRange, definition.MaxRange, definition.Width, definition.TargetRow,
                definition.ExcludeAnchor, definition.SameLine);

        public bool MatchesRow(RowType row) => Row == RowType.None || Row == row;

        public bool Contains(int anchorColumn, int anchorLine, int column, int line, RowType row)
        {
            if (!MatchesRow(row)) return false;
            if (IsUnlimited) return true;

            var spread = BoardGeometry.Spread(anchorColumn, column);
            var depth = BoardGeometry.Depth(anchorLine, line);

            if (ExcludeAnchor && spread == 0 && depth == 0) return false;
            if (SameLine && depth != 0) return false;
            if (Width > 0 && spread > Width) return false;
            if (depth < MinRange) return false;

            return MaxRange <= 0 || depth <= MaxRange;
        }
    }
}
