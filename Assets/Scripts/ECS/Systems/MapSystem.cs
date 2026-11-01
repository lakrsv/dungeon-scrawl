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
// //   Generates the dungeon, paints it, and lights it.
// // </summary>
// // --------------------------------------------------------------------------------------------------------------------

namespace ECS.Systems
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Controllers;

    using ECS.Components;
    using ECS.Components.Type;
    using ECS.Entities;

    using Imported.RogueSharp;
    using Imported.RogueSharp.MapCreation;
    using Imported.RogueSharp.Random;

    using UnityEngine;
    using UnityEngine.Tilemaps;

    using Utilities;
    using Utilities.Game;
    using Utilities.Game.ECSCache;

    public class MapSystem : MonoSingleton<MapSystem>, IInitializeSystem
    {
        // Level 5 is not random (see BOSS.md): a landing, a long drawbridge north over the void, and a far landing
        // with the way out. The bridge is three lanes wide; the Scrawl rises when the knight reaches BossRow.
        public const int BridgeLanes = 3;

        public const int BridgeX = 9;

        public const int BossRow = 22;

        private const int BridgeWidth = 21;

        private const int BridgeHeight = 40;

        private const int BridgeStart = 7;

        private const int BridgeEnd = 31;

        [SerializeField]
        private Tilemap _map;

        [SerializeField]
        private TileBase _floorTile;

        [SerializeField]
        private TileBase _cliffTile;

        [Header("Level 5")]
        [SerializeField]
        private TileBase _bridgeLeft;

        [SerializeField]
        private TileBase _bridgeMiddle;

        [SerializeField]
        private TileBase _bridgeRight;

        // How fast tiles fade between light levels, in colour units per second.
        [SerializeField]
        private float _lightFadeSpeed = 4f;

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

        // Tiles in view, plus tiles still fading after they left it.
        private readonly HashSet<Vector3Int> _changingTiles = new HashSet<Vector3Int>();

        private readonly List<Vector3Int> _settledTiles = new List<Vector3Int>();

        public Map Map { get; private set; }

        public static bool IsBossLevel
        {
            get { return GameController.CurrentLevel == RunStats.Levels; }
        }

        /// <summary>
        /// Gets where the player starts, on levels that say so (level 5); otherwise null, and they start anywhere.
        /// </summary>
        public Vector2Int? SpawnPoint { get; private set; }

        /// <summary>
        /// Gets the way out of level 5, at the far end of the bridge. It only opens once the Scrawl is beaten.
        /// </summary>
        public Vector2Int BossExit
        {
            get { return new Vector2Int(BridgeX + 1, BridgeEnd + 2); }
        }

        // Where the player's light is and how far it reaches. The RenderSystem moves it every frame.
        public Vector2 LightPosition { get; set; }

        public float LightRadius { get; set; }

        public void Initialize()
        {
            _random = new DotNetRandom(Constants.GetRandomSeed());
            if (IsBossLevel) CreateBridgeMap();
            else CreateDefaultMap();
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
            var entityPositions = ActorCache.Instance.GetCached();
            if (entityPositions == null) return null;

            var first = entityPositions.FirstOrDefault(x =>
                {
                    var gridPos = x.Entity.GetComponent<GridPositionComponent>();
                    return gridPos != null && gridPos.Position == position;
                });

            return first == null ? null : first.Entity;
        }

        public void ComputeFov(int xOrigin, int yOrigin, int radius, bool lightWalls = true)
        {
            // Computed on the main thread: Map.ComputeFov clears and refills the FOV set that Map.IsInFov reads
            // every frame, so running it on a worker thread raced with those reads. It costs well under a millisecond.
            var fovCells = Map.ComputeFov(xOrigin, yOrigin, radius, lightWalls);

            _currentFov = fovCells.Select(c => new Vector3Int(c.X, c.Y, 0)).Where(p => _map.HasTile(p)).ToList();
            foreach (var cell in fovCells) Map.SetCellExplored(cell.X, cell.Y, true);
        }

        /// <summary>
        /// The light falling on a position. In view, the player's light fades with distance; explored places out of
        /// view keep a dim, cold memory; unexplored places are not drawn at all.
        /// </summary>
        public Color GetLight(Vector2Int cell, Vector2 position)
        {
            if (Map.IsInFov(cell.x, cell.y))
            {
                var t = Mathf.Clamp01(Vector2.Distance(position, LightPosition) / (LightRadius + 1f));
                return Visibility.Lit(t);
            }

            return Map.IsExplored(cell.x, cell.y) ? Visibility.ExploredInvisible : Visibility.Invisible;
        }

        /// <summary>
        /// Eases every tile toward the light falling on it, so the dungeon fades in and out of view.
        /// </summary>
        public void UpdateTileLighting(float deltaTime)
        {
            foreach (var pos in _currentFov) _changingTiles.Add(pos);

            _settledTiles.Clear();
            var step = deltaTime * _lightFadeSpeed;
            foreach (var pos in _changingTiles)
            {
                var target = GetLight(new Vector2Int(pos.x, pos.y), new Vector2(pos.x, pos.y));
                var current = _map.GetColor(pos);
                var next = (Color)Vector4.MoveTowards(current, target, step);
                if (next != current) _map.SetColor(pos, next);

                if (next == target && !Map.IsInFov(pos.x, pos.y)) _settledTiles.Add(pos);
            }

            foreach (var pos in _settledTiles) _changingTiles.Remove(pos);
        }

        private void CreateBridgeMap()
        {
            _map.ClearAllTiles();
            Map = new Map(BridgeWidth, BridgeHeight);

            // Landings: a small one to start on, and a far one with the way out.
            for (var x = BridgeX - 2; x <= BridgeX + 4; x++)
            {
                for (var y = 2; y < BridgeStart; y++) Map.SetCellProperties(x, y, true, true);
                for (var y = BridgeEnd + 1; y <= BridgeEnd + 3; y++) Map.SetCellProperties(x, y, true, true);
            }

            for (var y = BridgeStart; y <= BridgeEnd; y++)
                for (var lane = 0; lane < BridgeLanes; lane++)
                    Map.SetCellProperties(BridgeX + lane, y, true, true);

            var tilePosition = new Vector3Int();
            for (var x = 0; x < BridgeWidth; x++)
            {
                for (var y = 0; y < BridgeHeight; y++)
                {
                    tilePosition.Set(x, y, 0);
                    var onBridge = x >= BridgeX && x < BridgeX + BridgeLanes && y >= BridgeStart && y <= BridgeEnd;
                    if (onBridge)
                    {
                        _map.SetTile(tilePosition, x == BridgeX ? _bridgeLeft : x == BridgeX + BridgeLanes - 1 ? _bridgeRight : _bridgeMiddle);
                    }
                    else if (Map.IsWalkable(x, y))
                    {
                        _map.SetTile(tilePosition, _floorTile);
                    }
                    else if (y + 1 < BridgeHeight && Map.IsWalkable(x, y + 1) && !(y + 1 >= BridgeStart && y + 1 <= BridgeEnd))
                    {
                        // Rock faces under the landings; the bridge hangs over nothing.
                        _map.SetTile(tilePosition, _cliffTile);
                    }
                    else
                    {
                        continue;
                    }

                    _map.SetColor(tilePosition, Visibility.Invisible);
                }
            }

            SpawnPoint = new Vector2Int(BridgeX + 1, 3);
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
                    _map.SetTile(tilePosition, _floorTile);
                    _map.SetColor(tilePosition, Visibility.Invisible);
                }
                else
                {
                    if (y >= _mapHeight - 1) continue;

                    var cellAbove = Map.GetCell(x, y + 1);
                    if (cellAbove != null && cellAbove.IsWalkable)
                    {
                        _map.SetTile(tilePosition, _cliffTile);
                        _map.SetColor(tilePosition, Visibility.Invisible);
                    }
                }
            }
        }
    }
}