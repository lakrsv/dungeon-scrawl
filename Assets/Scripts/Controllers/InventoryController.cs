// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="InventoryController.cs" author="Lars" company="None">
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

namespace Controllers
{
    using ECS.Components;
    using ECS.Components.Type;
    using ECS.Systems;

    using UI;
    using UI.Notification;

    using UnityEngine;

    using Utilities.Game.ECSCache;
    using Utilities.Game.ObjectPool;

    public class InventoryController : MonoBehaviour
    {
        public const int MaxDamage = 10;

        public const int MaxReach = 6;

        public const int MaxHealth = 10;

        public const int MaxFov = 10;

        [SerializeField]
        private ReachDisplay _reachDisplay;

        [SerializeField]
        private HealthDisplay _healthDisplay;

        [SerializeField]
        private FovDisplay _fovDisplay;

        public void AddPower()
        {
            var player = ActorCache.Instance.Player;

            var damageComponent = player.Entity.GetComponent<IntegerComponent>(ComponentType.Damage);
            var reachComponent = player.Entity.GetComponent<IntegerComponent>(ComponentType.Reach);

            if (damageComponent.Value >= MaxDamage) return;
            damageComponent.Value += 1;

            if (reachComponent.Value < MaxReach) reachComponent.Value += 1;

            _reachDisplay.SetReachCount(damageComponent.Value - 1);

            AudioPlayer.Instance.PlayAudio(AudioPlayer.Type.ItemApply);
            ObjectPools.Instance.GetPooledObject<TextPopup>().Enable("POWER+", GetDisplayPos(), 3.0f);
        }

        public void AddHealth()
        {
            var player = ActorCache.Instance.Player;
            var playerHealth = player.Entity.GetComponent<IntegerComponent>(ComponentType.Health);
            if (playerHealth.Value >= MaxHealth) return;

            playerHealth.Value += 1;

            _healthDisplay.SetHealth(playerHealth.Value);

            AudioPlayer.Instance.PlayAudio(AudioPlayer.Type.ItemApply);
            ObjectPools.Instance.GetPooledObject<TextPopup>().Enable("HEALTH+", GetDisplayPos(), 3.0f);
        }

        public void AddFov()
        {
            var player = ActorCache.Instance.Player;
            var playerFov = player.Entity.GetComponent<IntegerComponent>(ComponentType.FieldOfView);
            if (playerFov.Value >= MaxFov) return;

            playerFov.Value += 1;

            _fovDisplay.SetFov(playerFov.Value);

            var playerPos = player.Entity.GetComponent<GridPositionComponent>();
            if (playerPos != null)
            {
                MapSystem.Instance.ComputeFov(playerPos.Position.x, playerPos.Position.y, playerFov.Value);
            }

            AudioPlayer.Instance.PlayAudio(AudioPlayer.Type.ItemApply);
            ObjectPools.Instance.GetPooledObject<TextPopup>().Enable("LIGHT+", GetDisplayPos(), 3.0f);
        }

        private Vector2 GetDisplayPos()
        {
            return (Vector2)ActorCache.Instance.Player.Entity.GameObject.transform.position + Vector2.up;
        }
    }
}