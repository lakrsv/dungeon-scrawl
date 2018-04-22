// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="Zombie.cs" author="Lars" company="None">
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

namespace ECS.Entities.Blueprint
{
    using ECS.Components;
    using ECS.Components.Type;
    using ECS.Systems;

    using UnityEngine;

    using Utilities.Game;
    using Utilities.Game.ECSCache;

    public class Demon : IEntityBlueprint
    {
        public IComponent[] GetComponents(Entity owner)
        {
            var components = new IComponent[]
                                 {
                                     new GridPositionComponent(owner) { Position = MapSystem.Instance.GetRandomAvailableTile()},
                                     new RenderComponent(owner) { Sprite = Sprites.Instance.GetDemonSprite() },
                                     new IntegerComponent(owner, ComponentType.Health) { Value = 4 },
                                     new IntegerComponent(owner, ComponentType.Damage) { Value = 2 },
                                     new IntegerComponent(owner, ComponentType.Reach) { Value = 3 },
                                     new IntegerComponent(owner, ComponentType.FieldOfView) { Value = 5 },
                                     new BooleanComponent(owner, ComponentType.Turn),
                                     new ChaseTargetComponent(owner) { Target = ActorCache.Instance.Player.Entity }, 
                                 };

            return components;
        }
    }
}