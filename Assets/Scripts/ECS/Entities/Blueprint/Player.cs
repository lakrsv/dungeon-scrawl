// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="Player.cs" author="Lars" company="None">
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
    using System.Collections.Generic;

    using ECS.Components;
    using ECS.Components.Type;
    using ECS.Systems;

    using UnityEngine;

    using Utilities.Game.ECSCache;

    // TODO - Make this all nice and editor creatable like a prefab
    public class Player : IEntityBlueprint
    {
        public static readonly Dictionary<ComponentType, int> DefaultValues =
            new Dictionary<ComponentType, int>
                {
                    { ComponentType.Health, 5 },
                    { ComponentType.Damage, 1 },
                    { ComponentType.Reach, 1 },
                    { ComponentType.FieldOfView, 3 }
                };

        public static void ClearPlayerStats()
        {
            ClearPlayerStat(ComponentType.Health);
            ClearPlayerStat(ComponentType.Damage);
            ClearPlayerStat(ComponentType.Reach);
            ClearPlayerStat(ComponentType.FieldOfView);
        }

        public static int GetSavedStat(ComponentType type)
        {
            return PlayerPrefs.GetInt(type.ToString(), DefaultValues[type]);
        }

        public static void SavePlayerStats()
        {
            SavePlayerStat(ComponentType.Health);
            SavePlayerStat(ComponentType.Damage);
            SavePlayerStat(ComponentType.Reach);
            SavePlayerStat(ComponentType.FieldOfView);
        }

        public IComponent[] GetComponents(Entity owner)
        {
            var components = new IComponent[]
                                 {
                                     new GridPositionComponent(owner)
                                         {
                                             Position = MapSystem.Instance
                                                 .GetRandomAvailableTile()
                                         },
                                     new RenderComponent(owner) { Sprite = Resources.Load<Sprite>("Sprites/Player") },
                                     new IntegerComponent(owner, ComponentType.Health)
                                         {
                                             Value = GetSavedStat(
                                                 ComponentType.Health)
                                         },
                                     new IntegerComponent(owner, ComponentType.Damage)
                                         {
                                             Value = GetSavedStat(
                                                 ComponentType.Damage)
                                         },
                                     new IntegerComponent(owner, ComponentType.Reach)
                                         {
                                             Value = GetSavedStat(
                                                 ComponentType.Reach)
                                         },
                                     new IntegerComponent(owner, ComponentType.FieldOfView)
                                         {
                                             Value = GetSavedStat(
                                                 ComponentType.FieldOfView)
                                         }
                                 };

            return components;
        }

        private static void ClearPlayerStat(ComponentType type)
        {
            PlayerPrefs.SetInt(type.ToString(), DefaultValues[type]);
            PlayerPrefs.Save();
        }

        private static void SavePlayerStat(ComponentType type)
        {
            var playerStat = ActorCache.Instance.Player.Entity.GetComponent<IntegerComponent>(type);
            PlayerPrefs.SetInt(type.ToString(), playerStat.Value);
            PlayerPrefs.Save();
        }
    }
}