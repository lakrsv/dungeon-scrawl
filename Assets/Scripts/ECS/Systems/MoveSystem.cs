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
    using ECS.Components.Type;
    using ECS.Entities;

    using UI.Notification;

    using UnityEngine;

    using Utilities.Game.ECSCache;
    using Utilities.Game.ObjectPool;

    public class MoveSystem : MonoBehaviour, IExecuteSystem
    {
        // TODO - Component for movement duration?
        private const float MovementDuration = 0.25f;

        private readonly HashSet<Entity> _movingEntities = new HashSet<Entity>();

        [SerializeField]
        private AttackSystem _attackSystem;

        private Vector3 _temp = Vector2.zero;

        public void Execute()
        {
            var positionComponents = ComponentCache.Instance.GetCached<GridPositionComponent>();
            if (positionComponents == null) return;

            foreach (var component in positionComponents)
            {
                UpdatePosition(component);
                MovePosition(component);
            }
        }

        public bool IsMoving(Entity entity)
        {
            return _movingEntities.Contains(entity);
        }

        private void UpdatePosition(GridPositionComponent component)
        {
            if (IsMoving(component.Owner) || _attackSystem.IsAttacking(component.Owner)) return;

            // Not our turn.
            var turnComponent = component.Owner.GetComponent<BooleanComponent>(ComponentType.Turn);
            if (turnComponent == null || !turnComponent.Value) return;

            // No Vision
            var fieldOfView = component.Owner.GetComponent<IntegerComponent>(ComponentType.FieldOfView);
            if (fieldOfView == null) return;

            // No Movement Defined
            var chaseTarget = component.Owner.GetComponent<ChaseTargetComponent>();
            if (chaseTarget == null) return;

            // Neutral is Defined
            var neutral = component.Owner.GetComponent<NeutralComponent>();
            if (neutral != null)
            {
                turnComponent.Value = false;
                return;
            }

            // No Target Position
            var targetPosition = chaseTarget.Target.GetComponent<GridPositionComponent>();
            if (targetPosition == null) return;

            // Out of Range.
            var distance = Vector2Int.Distance(targetPosition.Position, component.Position);
            if (distance > fieldOfView.Value)
            {
                turnComponent.Value = false;
                return;
            }

            var reach = component.Owner.GetComponent<IntegerComponent>(ComponentType.Reach);
            if (reach != null && reach.Value >= distance) return;

            var moveDirection = MapSystem.Instance.GetMoveDirection(component.Position, targetPosition.Position);

            if (!MapSystem.Instance.IsWalkable(component.Position + moveDirection))
            {
                turnComponent.Value = false;
            }
            else
            {
                component.Position += moveDirection;
            }
        }

        private void MovePosition(GridPositionComponent component)
        {
            if (IsMoving(component.Owner) || _attackSystem.IsAttacking(component.Owner)) return;

            _temp.Set(component.Position.x, component.Position.y, 0);
            if (component.Owner.GameObject.transform.position == _temp) return;

           _movingEntities.Add(component.Owner);
           MoveEntity(component.Owner, _temp);
        }

        private void MoveEntity(Entity entity, Vector3 position)
        {
            entity.GameObject.transform.DOMove(position, MovementDuration).SetEase(Ease.OutBack)
                .OnComplete(() =>
                    {
                        var turnComponent = entity.GetComponent<BooleanComponent>(ComponentType.Turn);
                        if (turnComponent != null) turnComponent.Value = false;
                        _movingEntities.Remove(entity);
                    });
        }
    }
}