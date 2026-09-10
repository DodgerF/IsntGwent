using System.Collections.Generic;

namespace IsntGwent.Scripts.Match.Server.Bot
{
    public interface IBotBrain
    {
        BotMove Decide(GameContext context, Player me);

        List<string> ChooseAim(GameContext context, Player me, IReadOnlyList<string> pool);

        string ChooseRedraw(GameContext context, Player me);
    }
}
