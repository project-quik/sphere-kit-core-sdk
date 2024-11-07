using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

#nullable enable
namespace SphereKit
{
    internal static class MainThreadDispatcher
    {
        private static readonly Queue<Action> _executionQueue = new();
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        private static SynchronizationContext _mainThreadContext;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

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