using System;
using IsntGwent.Scripts.Network;
using Zenject;

namespace IsntGwent.Scripts.Decks
{
    public class DeckBuilderSceneController : IInitializable, IDisposable
    {
        [Inject] private readonly ConnectionService _connection;

        public void Initialize()
        {
            _connection.SetAutoReconnect(true);
        }

        public void Dispose()
        {
            _connection.SetAutoReconnect(false);
        }
    }
}
