// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="ComponentCache.cs" author="Lars" company="None">
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

namespace Utilities.Game.ECSCache
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    using ECS.Components;
    using ECS.Components.Type;

    public class ComponentCache : MonoSingleton<ComponentCache>, ICache<IComponent, ComponentType>
    {
        private readonly Dictionary<Type, List<IComponent>> _componentsBySpecificType = new Dictionary<Type, List<IComponent>>();

        private readonly Dictionary<ComponentType, List<IComponent>> _componentsByGenericType = new Dictionary<ComponentType, List<IComponent>>();

        private readonly List<IComponent> _components = new List<IComponent>();

        public void Add(IComponent component, ComponentType type)
        {
            if (_components.Contains(component))
                throw new InvalidOperationException("Components already contains this component");
            _components.Add(component);

            if (type == ComponentType.SpecificType) AddSpecific(component);
            else AddGeneric(component, type);
        }

        public void Remove(IComponent component)
        {
            if (!_components.Contains(component))
                throw new InvalidOperationException("Components does not contain this component");
        }

        public bool Contains(IComponent component)
        {
            return _components.Contains(component);
        }

        public IEnumerable<IComponent> GetCached()
        {
            return _components.AsReadOnly();
        }

        public ReadOnlyCollection<IComponent> GetCached(ComponentType type)
        {
            return !_componentsByGenericType.ContainsKey(type) ? null : _componentsByGenericType[type].AsReadOnly();
        }

        public IEnumerable<T> GetCached<T>()
            where T : class, IComponent
        {
            var type = typeof(T);
            return !_componentsBySpecificType.ContainsKey(type) ? null : _componentsBySpecificType[type].Cast<T>();
        }

        private void AddSpecific(IComponent component)
        {
            var specificType = component.GetType();
            if (!_componentsBySpecificType.ContainsKey(specificType))
            {
                _componentsBySpecificType.Add(specificType, new List<IComponent> { component });
            }
            else
            {
                _componentsBySpecificType[specificType].Add(component);
            }
        }

        private void AddGeneric(IComponent component, ComponentType type)
        {
            if (!_componentsByGenericType.ContainsKey(type))
            {
                _componentsByGenericType.Add(type, new List<IComponent> { component });
            }
            else
            {
                _componentsByGenericType[type].Add(component);
            }
        }
    }
}