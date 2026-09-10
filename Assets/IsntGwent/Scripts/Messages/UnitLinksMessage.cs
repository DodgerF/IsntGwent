using IsntGwent.Scripts.Cards.Definitions;
using Mirror;

namespace IsntGwent.Scripts.Messages
{
    public enum UnitLinkKind : byte
    {
        Devour = 0,
        Lure = 1
    }

    public struct UnitLinksMessage : NetworkMessage
    {
        public UnitLinkData[] Links;
    }

    public struct UnitLinkData
    {
        public string SourceInstanceId;
        public string TargetInstanceId;
        public UnitLinkKind Kind;
        public RowType TargetRow;
        public int TargetSlot;
    }
}
