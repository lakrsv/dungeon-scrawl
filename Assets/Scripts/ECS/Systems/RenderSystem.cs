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
    using ECS.Components;
    using ECS.Components.Type;

    using UnityEngine;

    using Utilities.Game;
    using Utilities.Game.ECSCache;

    public class RenderSystem : MonoBehaviour, IInitializeSystem, IExecuteSystem
    {
        // How much the light's reach wavers, in tiles, like a flame.
        [SerializeField]
        private float _flicker = 0.35f;

        private Vector2Int _lastFovPosition;

        // The first pass happens before the board slides in, while systems are paused, so it lights at once.
        private bool _lit;

        public void Execute()
        {
            CalculateFov();
            UpdateLight();
            MapSystem.Instance.UpdateTileLighting(_lit ? Time.deltaTime : float.MaxValue);
            UpdateVisibilities();
            _lit = true;
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

        private void UpdateLight()
        {
            var player = ActorCache.Instance.Player;
            if (player == null) return;

            var fieldOfView = player.Entity.GetComponent<IntegerComponent>(ComponentType.FieldOfView);
            if (fieldOfView == null) return;

            // The light rides on the player's sprite, so it glides with them between tiles.
            MapSystem.Instance.LightPosition = player.Entity.GameObject.transform.position;
            MapSystem.Instance.LightRadius =
                fieldOfView.Value + (Mathf.PerlinNoise(Time.time * 2.5f, 0.37f) - 0.5f) * 2f * _flicker;
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
                render.Renderer.sprite = Sprites.Instance.GetFrame(render.Sprite, Time.time + render.AnimationOffset);

                if (visible)
                {
                    render.Renderer.color = MapSystem.Instance.GetLight(
                        position.Position,
                        render.Owner.GameObject.transform.position);
                }

                if (render.Shadow != null) render.Shadow.enabled = visible && render.ShowShadow;
            }
        }
    }
}