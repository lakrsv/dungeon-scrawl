// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="BridgeTile.cs" author="Lars" company="None">
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
// //   The planks of the level 5 drawbridge.
// // </summary>
// // --------------------------------------------------------------------------------------------------------------------

namespace Tiles
{
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// One column of the drawbridge over the void on level 5: its left edge, its middle or its right edge, each with a
    /// few plank variants picked by position. Drawn by Tools/PixelArt/boss.py.
    /// </summary>
    [CreateAssetMenu(menuName = "Tiles/Bridge Tile")]
    public class BridgeTile : TileBase
    {
        [SerializeField]
        private Sprite[] _sprites;

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            // Bridges are long: vary by row so neighbouring planks don't repeat.
            unchecked
            {
                var h = position.y * 668265263 + position.x * 374761393;
                h = (h ^ (h >> 13)) * 1274126177;
                tileData.sprite = _sprites[((h ^ (h >> 16)) & int.MaxValue) % _sprites.Length];
            }

            tileData.color = Color.white;
            tileData.flags = TileFlags.LockTransform;
            tileData.colliderType = Tile.ColliderType.None;
            tileData.transform = Matrix4x4.identity;
        }
    }
}
