// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="MapSystem.cs" author="Lars" company="None">
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
    using System.Linq;
    using System.Threading;

    using ECS.Components;
    using ECS.Components.Type;
    using ECS.Entities;

    using Imported.RogueSharp;
    using Imported.RogueSharp.MapCreation;
    using Imported.RogueSharp.Random;

    using Tiles.Rule_Tile.Scripts;

    using UnityEngine;
    using UnityEngine.Tilemaps;

    using Utilities;
    using Utilities.Game;
    using Utilities.Game.ECSCache;

    public class MapSystem : MonoSingleton<MapSystem>, IInitializeSystem
    {
        [SerializeField]
        private Tilemap _map;

        [SerializeField]
        private RuleTile _wallTile;

        [Header("Generation Rules")]
        [SerializeField]
        private int _mapWidth;

        [SerializeField]
        private int _mapHeight;

        [SerializeField]
        private int _maxRooms;

        [SerializeField]
        private int _maxRoomSize;

        [SerializeField]
        private int _minRoomSize;

        private IRandom _random;

        private PathFinder _pathFinder;

        private List<Vector3Int> _currentFov = new List<Vector3Int>();

        public Map Map { get; private set; }

        public void Initialize()
        {
            _random = new DotNetRandom(Constants.RandomSeed);
            CreateDefaultMap();
        }

        public Vector2Int GetMoveDirection(Vector2Int from, Vector2Int to)
        {
            _pathFinder = new PathFinder(Map);

            var fromCell = Map.GetCell(from.x, from.y);
            var toCell = Map.GetCell(to.x, to.y);

            var path = _pathFinder.TryFindShortestPath(fromCell, toCell);
            if (path == null) return Vector2Int.zero;

            var nextCell = path.TryStepForward();
            if (nextCell == null) return Vector2Int.zero;

            var moveDirection = new Vector2Int(nextCell.X - from.x, nextCell.Y - from.y);
            return moveDirection;
        }

        public Vector2Int GetRandomAvailableTile()
        {
            var maxIterations = 100;
            var currentIterations = 0;

            while (currentIterations < maxIterations)
            {
                var randomPosition = Map.RandomLocation(_random);

                if (IsWalkable(randomPosition)) return randomPosition;

                currentIterations++;
            }

            throw new InvalidOperationException("No available tile found!");
        }

        public bool IsWalkable(Vector2Int position)
        {
            var entityAtPos = GetEntityAtPosition(position);
            return Map.IsWalkable(position.x, position.y)
                   && (entityAtPos == null || entityAtPos.GetComponent<BooleanComponent>(ComponentType.Turn) == null);
        }

        public Entity GetEntityAtPosition(Vector2Int position)
        {
            var entityPositions = ComponentCache.Instance.GetCached<GridPositionComponent>();
            if (entityPositions == null) return null;

            var first = entityPositions.FirstOrDefault(x => x.Position == position);
            if (first == null) return null;

            return first.Owner;
        }

        public void ComputeFov(int xOrigin, int yOrigin, int radius, bool lightWalls = true)
        {
            ThreadPool.QueueUserWorkItem(
                state =>
                    {
                        var fovCells = Map.ComputeFov(xOrigin, yOrigin, radius, lightWalls);
                        var exploredPositions = fovCells.Select(c => new Vector3Int(c.X, c.Y, 0)).ToList();
                        RoutineDispatcher.Instance.ExecuteOnMainThread(() => SetTileVisibilities(exploredPositions));
                    });
        }

        private void CreateDefaultMap()
        {
            _map.ClearAllTiles();

            var mapCreationStrategy = new RandomRoomsMapCreationStrategy<Map>(
                _mapWidth,
                _mapHeight,
                _maxRooms,
                _maxRoomSize,
                _minRoomSize,
                _random);

            Map = mapCreationStrategy.CreateMap();

            var tilePosition = new Vector3Int();

            for (var x = 0; x < _mapWidth; x++)
            for (var y = 0; y < _mapHeight; y++)
            {
                var cell = Map.GetCell(x, y);
                tilePosition.Set(x, y, 0);

                if (cell.IsWalkable)
                {
                    _map.SetTile(tilePosition, _wallTile);
                    _map.SetColor(tilePosition, Visibility.Invisible);
                    _map.RefreshTile(tilePosition);
                }
            }
        }

        private void SetTileVisibilities(List<Vector3Int> visiblePositions)
        {
            foreach (var pos in _currentFov)
            {
                _map.SetColor(
                    pos,
                    Map.IsExplored(pos.x, pos.y) ? Visibility.ExploredInvisible : Visibility.Invisible);

                _map.RefreshTile(pos);
            }

            foreach (var pos in visiblePositions)
            {
                Map.SetCellExplored(pos.x, pos.y, true);

                _map.SetColor(pos, Visibility.Visible);
                _map.RefreshTile(pos);
            }

            _currentFov = visiblePositions;
        }
    }
}