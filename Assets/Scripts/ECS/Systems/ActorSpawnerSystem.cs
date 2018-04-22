// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="ActorSpawnerSystem.cs" author="Lars" company="None">
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

namespace ECS.Systems
{
    using Controllers;

    using ECS.Entities;
    using ECS.Entities.Blueprint;

    using UnityEngine;

    using Utilities.Game.ECSCache;
    using Utilities.Game.ObjectPool;

    public class ActorSpawnerSystem : MonoBehaviour, IInitializeSystem
    {
        [SerializeField]
        private Transform _board;

        public void Initialize()
        {
            if (GameController.CurrentLevel == 1)
            {
                SpawnLevel1Actors();
            }
            else if (GameController.CurrentLevel == 2)
            {
                SpawnLevel2Actors();
            }
            else if (GameController.CurrentLevel == 3)
            {
                SpawnLevel3Actors();
            }
            else if (GameController.CurrentLevel == 4)
            {
                SpawnLevel4Actors();
            }
            else if (GameController.CurrentLevel == 5)
            {
                SpawnLevel5Actors();
            }
        }

        private void SpawnActor<T>()
            where T : IEntityBlueprint, new()
        {
            var blueprint = new T();

            var actor = ObjectPools.Instance.GetPooledObject<Actor>();
            actor.Initialize(blueprint);
            actor.gameObject.transform.SetParent(_board, true);

            ActorCache.Instance.Add(actor, blueprint);
        }

        public void SpawnLevel1Actors()
        {
            // Spawn Player
            SpawnActor<Player>();

            for (var i = 0; i < 8; i++)
            {
                SpawnActor<Pest>();
            }
        }

        public void SpawnLevel2Actors()
        {
            // Spawn Player
            SpawnActor<Player>();

            // Spawn some Demons
            for (var i = 0; i < 6; i++)
            {
                SpawnActor<Pest>();
            }

            // Spawn some Demons
            for (var i = 0; i < 2; i++)
            {
                SpawnActor<Demon>();
            }

            // Spawn some Demons
            for (var i = 0; i < 3; i++)
            {
                SpawnActor<Slime>();
            }

            // Make one fake chest.. HEHEHE
            SpawnActor<FakeChest>();

        }

        public void SpawnLevel3Actors()
        {
            // Spawn Player
            SpawnActor<Player>();

            // Spawn some Demons
            for (var i = 0; i < 5; i++)
            {
                SpawnActor<Demon>();
            }

            // Spawn some Demons
            for (var i = 0; i < 3; i++)
            {
                SpawnActor<Pest>();
            }

            // Spawn some Demons
            for (var i = 0; i < 4; i++)
            {
                SpawnActor<Slime>();
            }
        }

        public void SpawnLevel4Actors()
        {
            // Spawn Player
            SpawnActor<Player>();

            // Spawn some Demons
            for (var i = 0; i < 8; i++)
            {
                SpawnActor<Demon>();
            }

            for (var i = 0; i < 3; i++)
            {
                SpawnActor<FakeChest>();
            }
        }

        public void SpawnLevel5Actors()
        {
            // Spawn Player
            SpawnActor<Player>();

            // Spawn some Demons
            for (var i = 0; i < 12; i++)
            {
                SpawnActor<Demon>();
            }
        }
    }
}