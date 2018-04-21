// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="MoveSystem.cs" author="Lars" company="None">
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
    using System.Collections.Generic;

    using DG.Tweening;

    using ECS.Components;
    using ECS.Entities;

    using UnityEngine;

    using Utilities.Game.ECSCache;

    public class MoveSystem : MonoBehaviour, IExecuteSystem
    {
        // TODO - Component for movement duration?
        private const float MovementDuration = 0.25f;

        private readonly HashSet<Actor> _movingActors = new HashSet<Actor>();

        private Vector3 _temp = Vector2.zero;

        public void Execute()
        {
            // TODO - Cache for performance
            foreach (var actor in ActorCache.Instance.GetCached())
            {
                if (_movingActors.Contains(actor)) continue;

                var gridPosition = actor.Entity.GetComponent<GridPositionComponent>();
                if (gridPosition == null) continue;

                _temp.Set(gridPosition.Position.x, gridPosition.Position.y, 0);
                if (actor.gameObject.transform.position == _temp) continue;

                _movingActors.Add(actor);
                MoveActor(actor, _temp);
            }
        }

        private void MoveActor(Actor actor, Vector3 position)
        {
            actor.gameObject.transform.DOMove(position, MovementDuration).SetEase(Ease.OutBack)
                .OnComplete(() => _movingActors.Remove(actor));
        }
    }
}