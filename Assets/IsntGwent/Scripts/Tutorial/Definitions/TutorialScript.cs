using System.Collections.Generic;

namespace IsntGwent.Scripts.Tutorial.Definitions
{
    public class TutorialScript
    {
        public string Id = "tutorial";

        public List<string> PlayerDeck = new();
        public List<string> BotDeck = new();

        public int StartingHand = 3;
        public string PassWarning = "Passing now gives the round away. Play your cards first.";
        public bool PlayerFirst;

        public List<TutorialBotMove> BotMoves = new();
        public List<TutorialStep> IntroSteps = new();
        public List<TutorialStep> Steps = new();
        public List<TutorialStep> MenuSteps = new();
    }
}
