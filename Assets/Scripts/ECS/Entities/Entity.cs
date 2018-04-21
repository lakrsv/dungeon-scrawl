// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="Entity.cs" author="Lars" company="None">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using ECS.Components;
    using ECS.Components.Type;

    using UnityEngine;

    public class Entity
    {
        private readonly List<IComponent> _components;

        public Entity(string name)
        {
            Name = name;
            _components = new List<IComponent>();
        }

        public Entity(GameObject gameObject)
            : this(gameObject.name)
        {
            GameObject = gameObject;
        }

        public string Name { get; private set; }

        public GameObject GameObject { get; private set; }

        public void AddComponent(IComponent component)
        {
            if (_components.Contains(component)) throw new InvalidOperationException(string.Format("Entity already has {0} component", component));

            _components.Add(component);
            component.OnAdd();
        }

        public void AddComponents(IComponent[] components)
        {
            foreach (var component in components)
            {
                AddComponent(component);
            }
        }

        public T GetComponent<T>()
            where T : class, IComponent
        {
            return (T)_components.FirstOrDefault(x => x.GetType() == typeof(T));
        }

        public T GetComponent<T>(ComponentType type)
            where T : class, IComponent
        {
            return (T)_components.FirstOrDefault(
                x =>
                    {
                        var tComponent = x as T;
                        return tComponent != null && tComponent.Type == type;
                    });
        }

        public void RemoveComponent(IComponent component)
        {
            if (!_components.Contains(component)) throw new InvalidOperationException(string.Format("Entity doesn't have component {0}", component));

            _components.Remove(component);
            component.OnRemove();
        }
    }
}