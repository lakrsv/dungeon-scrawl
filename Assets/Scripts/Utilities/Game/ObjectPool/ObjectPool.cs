// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="ObjectPool.cs" author="Lars-Kristian Svenoy" company="Cardlike">
// //  Copyright @ 2018 Cardlike. All rights reserved.
// // </copyright>
// // <summary>
// //   TODO - Insert file description
// // </summary>
// // --------------------------------------------------------------------------------------------------------------------

namespace Utilities.Game.ObjectPool
{
    using System;
    using System.Collections.Generic;

    using UnityEngine;

    [Serializable]
    public class ObjectPool
    {
        public int Amount;

        public bool CanExpand;

        public GameObject Prefab;

        private readonly List<IPoolable> _objects = new List<IPoolable>();

        public Type PoolType { get; private set; }

        public T GetPooled<T>()
            where T : class, IPoolable
        {
            foreach (var obj in _objects)
                if (!obj.IsEnabled)
                {
                    obj.Enable();
                    return (T)obj;
                }

            if (CanExpand)
            {
                var newObj = GameObject.Instantiate(Prefab).GetComponent<IPoolable>();
                newObj.Disable();
                _objects.Add(newObj);

                newObj.Enable();
                return (T)newObj;
            }

            return null;
        }

        public void Initialize()
        {
            var poolable = Prefab.GetComponent<IPoolable>();
            if (poolable == null)
                throw new InvalidOperationException(
                    string.Format("ObjectPool Prefab {0} is not IPoolable", Prefab.name));

            PoolType = poolable.GetType();

            for (var i = 0; i < Amount; i++)
            {
                var newObj = GameObject.Instantiate(Prefab).GetComponent<IPoolable>();
                newObj.Disable();
                _objects.Add(newObj);
            }
        }
    }
}