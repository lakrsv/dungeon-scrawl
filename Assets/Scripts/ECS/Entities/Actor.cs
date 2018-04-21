// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="Actor.cs" author="Lars" company="None">
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

namespace ECS.Entities
{
    using ECS.Entities.Blueprint;

    using UnityEngine;

    using Utilities.Game.ECSCache;
    using Utilities.Game.ObjectPool;

    public class Actor : MonoBehaviour, IPoolable
    {
        public Entity Entity { get; private set; }

        public bool IsActive { get; set; }

        public bool IsEnabled
        {
            get
            {
                return gameObject.activeInHierarchy;
            }
        }

        public void Disable()
        {
            gameObject.SetActive(false);

            ActorCache.Instance.Remove(this);
        }

        public void Enable()
        {
            gameObject.SetActive(true);

            Entity = new Entity(gameObject);
            ActorCache.Instance.Add(this);
        }

        public void Initialize(IEntityBlueprint blueprint)
        {
            Entity.AddComponents(blueprint.GetComponents(Entity));
        }
    }
}