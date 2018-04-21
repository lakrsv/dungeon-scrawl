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

    using ECS.Components;

    using UnityEngine;

    public class Entity
    {
        public Entity(string name)
        {
            Name = name;
            Components = new List<IComponent>();
        }

        public Entity(GameObject gameObject)
            : this(gameObject.name)
        {
            GameObject = gameObject;
        }

        public string Name { get; private set; }

        public GameObject GameObject { get; private set; }

        public List<IComponent> Components { get; private set; }

        public void AddComponent(IComponent component)
        {
            if (Components.Contains(component)) throw new InvalidOperationException(string.Format("Entity already has {0} component", component));

            Components.Add(component);
        }

        public void RemoveComponent(IComponent component)
        {
            if (!Components.Contains(component)) throw new InvalidOperationException(string.Format("Entity doesn't have component {0}", component));
        }
    }
}