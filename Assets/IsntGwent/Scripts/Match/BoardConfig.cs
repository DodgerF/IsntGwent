namespace IsntGwent.Scripts.Match
{
    public static class BoardConfig
    {
        public const int SlotsPerRow = 5;

        public static bool IsValidSlot(int index) => index >= 0 && index < SlotsPerRow;
    }
}
