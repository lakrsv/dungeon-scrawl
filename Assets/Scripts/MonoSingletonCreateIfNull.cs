// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="MonoSingletonCreateIfNull.cs" author="Lars-Kristian Svenoy" company="Cardlike">
// //  Copyright @ 2018 Cardlike. All rights reserved.
// // </copyright>
// // <summary>
// //   TODO - Insert file description
// // </summary>
// // --------------------------------------------------------------------------------------------------------------------

namespace Utilities.Game
{
    using UnityEngine;

    /// <summary>
    ///     Any class inheriting MonoSingletonCreateIfNull will automatically become a MonoBehaviour Singleton,
    ///     and will be automatically instantiated if it does not currently exit.
    ///     WARNING: Before you inherit from this class, PLEASE consider the possible memory/reference leak situation.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MonoSingletonCreateIfNull<T> : MonoBehaviour
        where T : MonoBehaviour
    {
        protected static T instance;

        /**
       Returns the instance of this singleton.
    */
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = (T)FindObjectOfType(typeof(T));

                    if (instance == null)
                    {
                        var instanceObj = new GameObject(typeof(T).Name, typeof(T));
                        instance = instanceObj.GetComponent<T>();
                    }
                }

                return instance;
            }
        }

        private void OnDestroy()
        {
            instance = null;
        }
    }
}