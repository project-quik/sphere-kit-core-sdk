using System.Collections.Generic;
using System.Threading;
using System;
using UnityEngine;

namespace SphereKit
{
    internal static class MainThreadDispatcher
    {
        private static readonly Queue<Action> _executionQueue = new();
        private static SynchronizationContext _mainThreadContext;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            _mainThreadContext = SynchronizationContext.Current;
            if (_mainThreadContext == null)
            {
                Debug.LogError("MainThreadDispatcher: SynchronizationContext.Current is null. Ensure this is called from the main thread.");
            }
        }

        internal static void Execute(Action action)
        {
            _mainThreadContext?.Post(_ => action(), null);
        }
    }
}