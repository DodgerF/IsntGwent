using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Match.Server;

namespace IsntGwent.Scripts.Cards.Server.Effects
{
    public class EffectContext
    {
        public GameContext Game;
        
        public CardInstance Source;
        public Player Owner;
        
        public IGameEvent Event;

        public EffectDefinition Definition;
        
        public List<UnitInstance> ManualTargets = new();
        
        public List<UnitInstance> Targets = new();
    }
}
