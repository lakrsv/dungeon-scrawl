// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="ObjectPools.cs" author="Lars-Kristian Svenoy" company="Cardlike">
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

    public class ObjectPools : MonoSingleton<ObjectPools>
    {
        private readonly Dictionary<Type, ObjectPool> _objectPoolsByType = new Dictionary<Type, ObjectPool>();

        [SerializeField]
        private List<ObjectPool> _objectPools;

        public T GetPooledObject<T>()
            where T : class, IPoolable
        {
            var type = typeof(T);
            return _objectPoolsByType[type].GetPooled<T>();
        }

        private void Awake()
        {
            foreach (var pool in _objectPools)
            {
                pool.Initialize();
                _objectPoolsByType.Add(pool.PoolType, pool);
            }
        }
    }
}