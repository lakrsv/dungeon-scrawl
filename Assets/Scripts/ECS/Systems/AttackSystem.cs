// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="AttackSystem.cs" author="Lars" company="None">
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

    using DG.Tweening;

    using ECS.Components;
    using ECS.Components.Type;
    using ECS.Entities;

    using UI.Hint;
    using UI.Notification;

    using UnityEngine;

    using Utilities.Game;
    using Utilities.Game.ECSCache;
    using Utilities.Game.ObjectPool;

    public class AttackSystem : MonoBehaviour, IExecuteSystem
    {
        private readonly HashSet<Entity> _attackingEntities = new HashSet<Entity>();

        private readonly Dictionary<Entity, SpellHint> _hintsForEntity = new Dictionary<Entity, SpellHint>();

        [SerializeField]
        private MoveSystem _moveSystem;

        public event Action<Entity, SpellHint> OnEntityInvisible;

        public event Action<Entity, SpellHint> OnEntityVisible;

        public void AttackMelee(Entity entity, Entity target)
        {
            _attackingEntities.Add(entity);

            var entityPosition = entity.GetComponent<GridPositionComponent>();
            if (entityPosition == null) return;

            var targetPosition = target.GetComponent<GridPositionComponent>();
            if (targetPosition == null) return;

            var direction = ((Vector2)targetPosition.Position - entityPosition.Position).normalized;
            var distance = Vector2.Distance(entityPosition.Position, targetPosition.Position);

            var damage = entity.GetComponent<IntegerComponent>(ComponentType.Damage);
            if (damage == null) return;

            var targetHealth = target.GetComponent<IntegerComponent>(ComponentType.Health);
            if (targetHealth == null) return;

            var sequence = DOTween.Sequence();

            sequence.Append(
                entity.GameObject.transform.DOMove(entityPosition.Position + direction * distance / 2f, 0.25f)
                    .SetEase(Ease.InOutBack).OnComplete(
                        () =>
                        {
                            ApplyDamage(targetPosition, damage, targetHealth);
                        }));

            sequence.Append(
                entity.GameObject.transform.DOMove((Vector2)entityPosition.Position, 0.25f).SetEase(Ease.OutBack));

            sequence.OnComplete(
                () =>
                    {
                        var turnComponent = entity.GetComponent<BooleanComponent>(ComponentType.Turn);
                        if (turnComponent != null) turnComponent.Value = false;
                        _attackingEntities.Remove(entity);
                    });
        }

        private static void ApplyDamage(GridPositionComponent targetPosition, IntegerComponent damage, IntegerComponent targetHealth)
        {
            ObjectPools.Instance.GetPooledObject<TextPopup>().Enable(
                                                (-damage.Value).ToString(),
                                                targetPosition.Position + Vector2.up,
                                                1.0f);
            targetHealth.Value -= damage.Value;

            if (targetHealth.Value <= 0)
            {
                var render = targetHealth.Owner.GetComponent<RenderComponent>();
                if (render != null) render.Sprite = Sprites.Instance.GetDeathSprite();

                var turnComponent = targetHealth.Owner.GetComponent<BooleanComponent>(ComponentType.Turn);
                if (turnComponent != null) targetHealth.Owner.RemoveComponent(turnComponent);
            }
        }

        public void AttackRanged(Entity entity, Entity target)
        {
            // TODO - Implement ranged attack
            AttackMelee(entity, target);
        }

        public void Execute()
        {
            UpdateAI();
        }

        public bool IsAttacking(Entity entity)
        {
            return _attackingEntities.Contains(entity);
        }

        private void UpdateAI()
        {
            foreach (var actor in ActorCache.Instance.GetCached())
            {
                if (actor.Entity == ActorCache.Instance.Player.Entity) continue;

                UpdateAISpellHints(actor.Entity);
                UpdateAIAttack(actor.Entity);
            }
        }

        private void UpdateAIAttack(Entity entity)
        {
            if (IsAttacking(entity) || _moveSystem.IsMoving(entity)) return;

            var turnComponent = entity.GetComponent<BooleanComponent>(ComponentType.Turn);
            if (turnComponent == null || !turnComponent.Value) return;

            var gridPos = entity.GetComponent<GridPositionComponent>();
            if (gridPos == null) return;

            var reach = entity.GetComponent<IntegerComponent>(ComponentType.Reach);
            if (reach == null) return;

            var chase = entity.GetComponent<ChaseTargetComponent>();
            if (chase == null || chase.Target == null) return;

            var chaseGridPos = chase.Target.GetComponent<GridPositionComponent>();
            if (chaseGridPos == null) return;

            var distanceToTarget = Vector2.Distance(gridPos.Position, chaseGridPos.Position);
            if (!(reach.Value >= distanceToTarget)) return;

            if (distanceToTarget <= 1) AttackMelee(turnComponent.Owner, chase.Target);
            else AttackRanged(turnComponent.Owner, chase.Target);
        }

        private void UpdateAISpellHints(Entity entity)
        {
            var render = entity.GetComponent<RenderComponent>();

            if (render == null || !render.Renderer.enabled || entity.GetComponent<BooleanComponent>(ComponentType.Turn) == null)
            {
                if (_hintsForEntity.ContainsKey(entity))
                {
                    var spellHint = _hintsForEntity[entity];

                    _hintsForEntity[entity].Disable();
                    _hintsForEntity.Remove(entity);

                    if (OnEntityInvisible != null) OnEntityInvisible(entity, spellHint);
                }

                return;
            }

            if (!_hintsForEntity.ContainsKey(entity))
            {
                var spellHint = ObjectPools.Instance.GetPooledObject<SpellHint>();
                _hintsForEntity.Add(entity, spellHint);

                if (OnEntityVisible != null) OnEntityVisible(entity, spellHint);
            }
        }
    }
}