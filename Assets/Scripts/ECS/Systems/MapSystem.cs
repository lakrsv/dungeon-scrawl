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
    using Imported.RogueSharp;
    using Imported.RogueSharp.MapCreation;
    using Imported.RogueSharp.Random;

    using Tiles.Rule_Tile.Scripts;

    using UnityEngine;
    using UnityEngine.Tilemaps;

    public class MapSystem : MonoBehaviour, IInitializeSystem
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

        [SerializeField]
        private int _randomSeed = 1234;

        private IRandom _random;

        public Map Map { get; private set; }

        public void Initialize()
        {
            _random = new DotNetRandom(_randomSeed);
            CreateDefaultMap();
        }

        private void Awake()
        {
            Initialize();
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

                    ////_map.SetColor(tilePosition, Visibility.Invisible);
                    _map.RefreshTile(tilePosition);
                }
            }
        }
    }
}