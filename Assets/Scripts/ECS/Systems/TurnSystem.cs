// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="TurnSystem.cs" author="Lars" company="None">
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
    using System.Linq;

    using Controllers;

    using ECS.Components;
    using ECS.Components.Type;
    using ECS.Entities;

    using UnityEngine;

    using Utilities.Game.ECSCache;

    public class TurnSystem : MonoBehaviour, IExecuteSystem, IInitializeSystem
    {
        private float _currentTurnDelay = 0.4f;

        private int _currentTurnIndex;

        private float _turnEndTime;

        public void Execute()
        {
            if (!GameController.Instance.PlayerHasMoved) return;

            var turnComponents = ComponentCache.Instance.GetCached(ComponentType.Turn);

            if (turnComponents.Count == 0) return;
            if (_currentTurnIndex >= turnComponents.Count) _currentTurnIndex = 0;

            var currentTurnComponent = turnComponents[_currentTurnIndex] as BooleanComponent;
            if (currentTurnComponent == null) return;

            if (currentTurnComponent.Value) return;

            if (_turnEndTime == 0) _turnEndTime = Time.time;
            var timePassed = Time.time - _turnEndTime;
            if (timePassed < _currentTurnDelay) return;
            _turnEndTime = 0;

            Debug.Log("Turn Started!");

            _currentTurnIndex++;
            if (_currentTurnIndex >= turnComponents.Count) _currentTurnIndex = 0;

            var nextTurnComponent = turnComponents[_currentTurnIndex] as BooleanComponent;
            if (nextTurnComponent == null) throw new InvalidOperationException("Component was invalid!");

            nextTurnComponent.Value = true;
        }

        public void Initialize()
        {
            _currentTurnDelay = 0.4f / GameController.Instance.GetDifficulty();
            var turnComponents = ComponentCache.Instance.GetCached(ComponentType.Turn);
            var currentTurnComponent = turnComponents[_currentTurnIndex] as BooleanComponent;

            if (currentTurnComponent == null) throw new InvalidOperationException("Component was invalid!");
            currentTurnComponent.Value = true;
        }
    }
}