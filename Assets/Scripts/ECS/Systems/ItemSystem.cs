// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="ItemSystem.cs" author="Lars" company="None">
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
    using System;
    using System.Collections.Generic;

    using Controllers;

    using ECS.Components;

    using UnityEngine;

    using Utilities.Game.ECSCache;

    using Random = UnityEngine.Random;

    public class ItemSystem : MonoBehaviour, IInitializeSystem, IExecuteSystem
    {
        private const int MaxFireballPerLevel = 3;

        private const int MinFireballPerLevel = 1;

        private const int MaxHealthPerLevel = 6;

        private const int MinHealthPerLevel = 2;

        private const int MaxFovPerLevel = 3;

        private const int MinFovPerLevel = 2;

        [SerializeField]
        private Transform _board;

        [SerializeField]
        private Chest _chestPrefab;

        [SerializeField]
        private GameObject _door;

        private Dictionary<Vector2Int, Chest> _chestsAtPositions = new Dictionary<Vector2Int, Chest>();

        private Vector2Int _doorPosition;

        public event Action<Chest> OnPlayerEnterLootArea;

        public event Action<Chest> OnPlayerLeaveLootArea;

        private Chest _currentAvailableChest;

        public void Execute()
        {
            var player = ActorCache.Instance.Player;
            if (player == null) return;

            var playerPos = player.Entity.GetComponent<GridPositionComponent>();
            if (_chestsAtPositions.ContainsKey(playerPos.Position))
            {
                if (_currentAvailableChest != null) return;

                var chestAtPos = _chestsAtPositions[playerPos.Position];
                _currentAvailableChest = chestAtPos;

                if (OnPlayerEnterLootArea != null) OnPlayerEnterLootArea(_currentAvailableChest);
            }
            else
            {
                if (_currentAvailableChest != null)
                {
                    if (OnPlayerLeaveLootArea != null) OnPlayerLeaveLootArea(_currentAvailableChest);
                    _currentAvailableChest = null;
                }
            }

            if (playerPos.Position == _doorPosition)
            {
                GameController.Instance.EndLevel();
            }
        }

        public void Initialize()
        {
            var healthForLevel = Random.Range(MinHealthPerLevel, MaxHealthPerLevel + 1);
            var powerForLevel = Random.Range(MinFireballPerLevel, MaxFireballPerLevel + 1);
            var fovForLevel = Random.Range(MinFovPerLevel, MaxFovPerLevel + 1);

            for (var i = 0; i < healthForLevel; i++)
            {
                var chest = Instantiate(_chestPrefab);
                var chestPosition = GetChestPosition();
                chest.Create(chestPosition, Chest.Pickup.Health);

                _chestsAtPositions.Add(chestPosition, chest);

                chest.transform.SetParent(_board, true);
            }

            for (var i = 0; i < powerForLevel; i++)
            {
                var chest = Instantiate(_chestPrefab);
                var chestPosition = GetChestPosition();
                chest.Create(chestPosition, Chest.Pickup.Power);

                _chestsAtPositions.Add(chestPosition, chest);

                chest.transform.SetParent(_board, true);
            }

            for (var i = 0; i < fovForLevel; i++)
            {
                var chest = Instantiate(_chestPrefab);
                var chestPosition = GetChestPosition();
                chest.Create(chestPosition, Chest.Pickup.Fov);

                _chestsAtPositions.Add(chestPosition, chest);

                chest.transform.SetParent(_board, true);
            }

            var door = Instantiate(_door);
            var doorPosition = GetDoorPosition();
            door.transform.position = (Vector2)doorPosition;
            _doorPosition = doorPosition;

            door.transform.SetParent(_board, true);
        }

        public void ChestPickup(Chest chest)
        {
            if (_chestsAtPositions.ContainsKey(chest.Position))
            {
                _chestsAtPositions.Remove(chest.Position);
            }

            if (_currentAvailableChest == chest) _currentAvailableChest = null;
        }

        private Vector2Int GetChestPosition()
        {
            var maxIterations = 100;
            var currentIterations = 0;

            while (currentIterations < maxIterations)
            {
                var position = MapSystem.Instance.GetRandomAvailableTile();
                if (!_chestsAtPositions.ContainsKey(position)) return position;

                currentIterations++;
            }

            return Vector2Int.zero;
        }

        private Vector2Int GetDoorPosition()
        {
            var maxIterations = 100;
            var currentIterations = 0;

            var playerPos = ActorCache.Instance.Player.Entity.GetComponent<GridPositionComponent>();

            var lastPos = Vector2Int.zero;
            var highestDistanceFound = 0f;

            while (currentIterations < maxIterations)
            {
                var position = MapSystem.Instance.GetRandomAvailableTile();
                if (!_chestsAtPositions.ContainsKey(position))
                {
                    var distance = Vector2.Distance(playerPos.Position, position);

                    if (distance > highestDistanceFound)
                    {
                        highestDistanceFound = distance;
                        lastPos = position;
                    }
                }

                currentIterations++;
            }
            return lastPos;
        }
    }
}