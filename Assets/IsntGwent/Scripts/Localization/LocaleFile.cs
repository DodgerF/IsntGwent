using System.Collections.Generic;
using IsntGwent.Scripts.Cards;

namespace IsntGwent.Scripts.Localization
{
    public class LocaleCardText
    {
        public string name;
        public string description;
    }

    public class LocaleFile
    {
        public string code;
        public string name;
        public int order;
        public string inherits;
        public Dictionary<string, string> strings;
        public Dictionary<string, LocaleCardText> cards;
        public List<KeywordEntry> keywords;
    }
}
