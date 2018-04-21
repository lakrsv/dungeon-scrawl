// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="DontDestroy.cs" author="Lars-Kristian Svenoy" company="Cardlike">
// //  Copyright @ 2018 Cardlike. All rights reserved.
// // </copyright>
// // <summary>
// //   TODO - Insert file description
// // </summary>
// // --------------------------------------------------------------------------------------------------------------------

namespace Utilities.Game
{
    using System.Linq;

    using UnityEngine;

    /// <summary>
    ///     Keeps gameObjects this is attached to persistent throughout scenes
    /// </summary>
    public class DontDestroy : MonoBehaviour
    {
        public bool DestroyOriginal;

        public bool IsUnique;

        public void SetDoNotDestroy()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Awake()
        {
            if (IsUnique)
            {
                var gameObjects = FindObjectsOfType<GameObject>().Where(x => x.name == gameObject.name).ToArray();
                if (gameObjects.Length > 1)
                    if (!DestroyOriginal)
                    {
                        Destroy(gameObject);
                        return;
                    }
                    else
                    {
                        for (var i = 0; i < gameObjects.Length; i++)
                        {
                            var otherObj = gameObjects[i];
                            if (gameObject != otherObj) Destroy(otherObj);
                        }
                    }
            }

            DontDestroyOnLoad(gameObject);
        }
    }
}