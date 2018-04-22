// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="Systems.cs" author="Lars" company="None">
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), 
// to deal in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software,
// and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// // </copyright>
// // <summary>
// //   TODO - Insert file description
// // </summary>
// // --------------------------------------------------------------------------------------------------------------------

namespace ECS.Systems
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    using Controllers;

    using UnityEngine;

    using Utilities.Game;

    [SuppressMessage("ReSharper", "ForCanBeConvertedToForeach", Justification = "Performance for ECS")]
    public class Systems : MonoSingleton<Systems>
    {
        private IInitializeSystem[] _initializeSystems;

        private IExecuteSystem[] _executeSystems;

        private void Awake()
        {
            var initializeSystems = new List<IInitializeSystem>();
            var executeSystems = new List<IExecuteSystem>();

            for (var i = 0; i < transform.childCount; i++)
            {
                var system = transform.GetChild(i).GetComponent<ISystem>();
                if (system == null) throw new InvalidOperationException("Systems children should implement ISystem");

                if (system is IInitializeSystem)
                {
                    initializeSystems.Add(system as IInitializeSystem);
                }

                if (system is IExecuteSystem)
                {
                    executeSystems.Add(system as IExecuteSystem);
                }
            }

            _initializeSystems = initializeSystems.ToArray();
            _executeSystems = executeSystems.ToArray();
        }

        private void Start()
        {
            for (var i = 0; i < _initializeSystems.Length; i++)
            {
                _initializeSystems[i].Initialize();
            }
        }

        private void Update()
        {
            if (!GameController.Instance.IsPlaying) return;

            for (var i = 0; i < _executeSystems.Length; i++)
            {
                _executeSystems[i].Execute();
            }
        }
    }
}