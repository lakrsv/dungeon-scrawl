// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="FloorTile.cs" author="Lars" company="None">
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
// //   The dungeon floor: autotiles against its 8 neighbours.
// // </summary>
// // --------------------------------------------------------------------------------------------------------------------

namespace Tiles
{
    using System.Collections.Generic;

    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// A floor platform over the abyss. Edges facing the abyss get a lit rim, picked from the 47 blob cases that
    /// Tools/PixelArt/tiles.py draws. Fully enclosed tiles pick a slab variant, sometimes a decorated one.
    /// </summary>
    [CreateAssetMenu(menuName = "Tiles/Floor Tile")]
    public class FloorTile : TileBase
    {
        private const int N = 1, NE = 2, E = 4, SE = 8, S = 16, SW = 32, W = 64, NW = 128;

        private const int Enclosed = 255;

        private const int BlobCount = 47;

        private static readonly Vector3Int[] Offsets =
            {
                new Vector3Int(0, 1, 0), new Vector3Int(1, 1, 0), new Vector3Int(1, 0, 0), new Vector3Int(1, -1, 0),
                new Vector3Int(0, -1, 0), new Vector3Int(-1, -1, 0), new Vector3Int(-1, 0, 0), new Vector3Int(-1, 1, 0)
            };

        private static int[] _blobIndex;

        // Edge sprites in blob order (ascending canonical mask), one set of 47 per floor base.
        [SerializeField]
        private Sprite[] _edgeSprites;

        [SerializeField]
        private Sprite[] _plainSprites;

        [SerializeField]
        private Sprite[] _decorSprites;

        [SerializeField]
        [Range(0f, 1f)]
        private float _decorChance = 0.08f;

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            var mask = 0;
            for (var i = 0; i < Offsets.Length; i++)
            {
                if (tilemap.GetTile(position + Offsets[i]) == this) mask |= 1 << i;
            }

            mask = Canonical(mask);
            var hash = Hash(position);

            if (mask == Enclosed)
            {
                var roll = (hash & 0xFFFF) / 65536f;
                tileData.sprite = roll < _decorChance && _decorSprites.Length > 0
                                      ? _decorSprites[(hash >> 16) % _decorSprites.Length]
                                      : _plainSprites[(hash >> 16) % _plainSprites.Length];
            }
            else
            {
                var blobIndex = BlobIndex();
                var bases = _edgeSprites.Length / BlobCount;
                tileData.sprite = _edgeSprites[(hash % bases) * BlobCount + blobIndex[mask]];
            }

            tileData.color = Color.white;
            tileData.flags = TileFlags.LockTransform;
            tileData.colliderType = Tile.ColliderType.None;
            tileData.transform = Matrix4x4.identity;
        }

        public override void RefreshTile(Vector3Int position, ITilemap tilemap)
        {
            for (var y = -1; y <= 1; y++)
            for (var x = -1; x <= 1; x++)
            {
                var neighbour = position + new Vector3Int(x, y, 0);
                if (tilemap.GetTile(neighbour) == this) tilemap.RefreshTile(neighbour);
            }
        }

        // Diagonals only matter when both sides next to them are floor too. This leaves 47 distinct cases.
        private static int Canonical(int mask)
        {
            if ((mask & N) == 0 || (mask & E) == 0) mask &= ~NE;
            if ((mask & S) == 0 || (mask & E) == 0) mask &= ~SE;
            if ((mask & S) == 0 || (mask & W) == 0) mask &= ~SW;
            if ((mask & N) == 0 || (mask & W) == 0) mask &= ~NW;
            return mask;
        }

        private static int Hash(Vector3Int position)
        {
            unchecked
            {
                var h = position.x * 374761393 + position.y * 668265263;
                h = (h ^ (h >> 13)) * 1274126177;
                return (h ^ (h >> 16)) & int.MaxValue;
            }
        }

        // Maps a canonical mask to its position in the blob order.
        private static int[] BlobIndex()
        {
            if (_blobIndex != null) return _blobIndex;

            var masks = new SortedSet<int>();
            for (var m = 0; m < 256; m++) masks.Add(Canonical(m));

            _blobIndex = new int[256];
            var i = 0;
            foreach (var m in masks) _blobIndex[m] = i++;
            return _blobIndex;
        }
    }
}
