using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

namespace IsntGwent.Scripts.Match.Server
{
    public class GameEventBus : IDisposable
    {
        private const int MaxEventsPerDispatch = 256;

        private readonly Subject<IGameEvent> _subject = new();
        private readonly Queue<IGameEvent> _queue = new();
        private bool _dispatching;

        public IObservable<IGameEvent> Stream => _subject;

        public void Publish(IGameEvent gameEvent)
        {
            _queue.Enqueue(gameEvent);

            if (_dispatching) return;

            _dispatching = true;
            try
            {
                var guard = 0;
                while (_queue.Count > 0)
                {
                    if (++guard > MaxEventsPerDispatch)
                    {
                        Debug.LogError($"GameEventBus: превышен лимит событий за диспатч ({MaxEventsPerDispatch}), " +
                                       "очередь сброшена — похоже на зацикленные триггеры карт");
                        _queue.Clear();
                        break;
                    }

                    _subject.OnNext(_queue.Dequeue());
                }
            }
            finally
            {
                _dispatching = false;
            }
        }

        public void Dispose()
        {
            _queue.Clear();
            _subject.Dispose();
        }
    }
}
