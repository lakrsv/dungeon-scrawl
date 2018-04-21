// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="RoutineDispatcher.cs" author="Lars-Kristian Svenoy" company="Cardlike">
// //  Copyright @ 2018 Cardlike. All rights reserved.
// // </copyright>
// // <summary>
// //   TODO - Insert file description
// // </summary>
// // --------------------------------------------------------------------------------------------------------------------

namespace Utilities.Game
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    using UnityEngine;

    [RequireComponent(typeof(DontDestroy))]
    public class RoutineDispatcher : MonoSingletonCreateIfNull<RoutineDispatcher>
    {
        private readonly Queue<Action> _executeOnMainThread = new Queue<Action>();

        /// <summary>
        ///     Start a coroutine and allow it to run asynchronously.
        /// </summary>
        /// <param name="routine"></param>
        public Coroutine Dispatch(IEnumerator routine)
        {
            return StartCoroutine(routine);
        }

        /// <summary>
        ///     Start a coroutine and allow it to run asynchronously, with a completion callback.
        /// </summary>
        /// <param name="routine"></param>
        /// <param name="onComplete"></param>
        public Coroutine Dispatch(IEnumerator routine, Action onComplete)
        {
            return StartCoroutine(CallbackRoutine(routine, onComplete));
        }

        /// <summary>
        ///     Start a coroutine and allow it to run asynchronously, with a result callback
        /// </summary>
        /// <typeparam name="T">The type of result expected from the routine</typeparam>
        /// <param name="routine">The routine to run</param>
        /// <param name="onComplete">The callback to fire on completion</param>
        public Coroutine DispatchResult<T>(IEnumerator routine, Action<T> onComplete)
        {
            return StartCoroutine(CallbackRoutineResult(routine, onComplete));
        }

        /// <summary>
        ///     Execute an Action on the main thread
        /// </summary>
        /// <param name="action">The action to execute</param>
        public void ExecuteOnMainThread(Action action)
        {
            _executeOnMainThread.Enqueue(action);
        }

        private void Awake()
        {
            instance = this;
        }

        private IEnumerator CallbackRoutine(IEnumerator routine, Action onComplete)
        {
            yield return routine;
            if (onComplete != null) onComplete.Invoke();
        }

        private IEnumerator CallbackRoutineResult<T>(IEnumerator routine, Action<T> onComplete)
        {
            yield return routine;
            if (onComplete != null) onComplete((T)routine.Current);
        }

        private void Start()
        {
            var dontDestroy = GetComponent<DontDestroy>();
            dontDestroy.IsUnique = true;
            dontDestroy.DestroyOriginal = false;
        }

        private void Update()
        {
            while (_executeOnMainThread.Count > 0) _executeOnMainThread.Dequeue().Invoke();
        }
    }
}