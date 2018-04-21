// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="RenderSystem.cs" author="Lars" company="None">
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

    using ECS.Components;
    using ECS.Components.Type;
    using ECS.Entities;

    using UI.Hint;

    using UnityEngine;

    using Utilities.Game.ECSCache;

    public class RenderSystem : MonoBehaviour, IInitializeSystem, IExecuteSystem
    {
        private Vector2Int _lastFovPosition;

        public void Execute()
        {
            CalculateFov();
            UpdateVisibilities();
        }

        public void Initialize()
        {
            // TODO - Caching (IGroup) of similar components
            foreach (var component in ComponentCache.Instance.GetCached<RenderComponent>())
            {
                component.Renderer.enabled = false;
            }
        }

        private void CalculateFov()
        {
            var player = ActorCache.Instance.Player;
            if (player == null) return;

            var positionComponent = player.Entity.GetComponent<GridPositionComponent>();
            if (positionComponent == null) return;

            var fieldOfView = player.Entity.GetComponent<IntegerComponent>(ComponentType.FieldOfView);
            if (fieldOfView == null) return;

            if (_lastFovPosition == positionComponent.Position) return;

            _lastFovPosition = positionComponent.Position;
            MapSystem.Instance.ComputeFov(_lastFovPosition.x, _lastFovPosition.y, fieldOfView.Value);
        }

        private void UpdateVisibilities()
        {
            var renderComponents = ComponentCache.Instance.GetCached<RenderComponent>();
            if (renderComponents == null) return;

            foreach (var render in renderComponents)
            {
                var position = render.Owner.GetComponent<GridPositionComponent>();
                if (position == null) continue;

                var visible = MapSystem.Instance.Map.IsInFov(position.Position.x, position.Position.y);

                render.Renderer.enabled = visible;
                render.Renderer.sprite = render.Sprite;
            }
        }
    }
}