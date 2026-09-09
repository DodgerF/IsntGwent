using System.Collections.Generic;

namespace IsntGwent.Scripts.Tutorial.Definitions
{
    public class TutorialStep
    {
        public string Id;
        public string Text;
        public string Action;
        public string Advance = "tap";

        public List<string> Highlight = new();
        public List<string> Panel = new();

        public bool Aura = true;

        public TutorialAllow Allow;
    }
}
