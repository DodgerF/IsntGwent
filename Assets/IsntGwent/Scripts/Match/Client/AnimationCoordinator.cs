using System;
using System.Collections;
using System.Collections.Generic;
using IsntGwent.Scripts.Core;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Match.Client
{
    public class AnimationCoordinator : IDisposable
    {
        [Inject] private readonly CoroutineRunner _runner;

        private readonly Queue<Func<IEnumerator>> _beats = new();

        private Coroutine _worker;
        private bool _running;

        public void Enqueue(Action apply, float duration = 0f)
        {
            EnqueueRoutine(() => RunAction(apply, duration));
        }

        public void EnqueueRoutine(Func<IEnumerator> beat)
        {
            if (_runner == null)
            {
                RunImmediately(beat);
                return;
            }

            _beats.Enqueue(beat);

            if (_running) return;

            _running = true;
            _worker = _runner.StartCoroutine(Run());
        }

        public void Clear()
        {
            _beats.Clear();

            if (_worker != null && _runner != null)
                _runner.StopCoroutine(_worker);

            _worker = null;
            _running = false;
        }

        private IEnumerator Run()
        {
            while (_beats.Count > 0)
            {
                var beat = _beats.Dequeue();
                yield return beat();
            }

            _worker = null;
            _running = false;
        }

        private static void RunImmediately(Func<IEnumerator> beat)
        {
            var routine = beat();
            while (routine.MoveNext())
            {
            }
        }

        private static IEnumerator RunAction(Action apply, float duration)
        {
            try
            {
                apply?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            if (duration > 0f)
                yield return new WaitForSeconds(duration);
        }

        public void Dispose()
        {
            Clear();
        }
    }
}
