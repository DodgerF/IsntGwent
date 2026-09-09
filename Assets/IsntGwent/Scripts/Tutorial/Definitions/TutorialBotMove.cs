using System.Collections.Generic;

namespace IsntGwent.Scripts.Tutorial.Definitions
{
    public class TutorialBotMove
    {
        public string Card;
        public string Row = "Melee";
        public int Slot = -1;
        public bool EnemyRow;
        public bool Pass;

        public List<string> Targets;
    }
}
