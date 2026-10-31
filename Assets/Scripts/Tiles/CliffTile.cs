// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="CliffTile.cs" author="Lars" company="None">
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
// //   The rock face under a floor edge.
// // </summary>
// // --------------------------------------------------------------------------------------------------------------------

namespace Tiles
{
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// The rock face hanging under the south edge of a floor platform. Its ends are capped where the face stops,
    /// and runs of it mix several stalactite layouts.
    /// </summary>
    [CreateAssetMenu(menuName = "Tiles/Cliff Tile")]
    public class CliffTile : TileBase
    {
        // Indexed by layout * 4 + (left end ? 1 : 0) + (right end ? 2 : 0).
        [SerializeField]
        private Sprite[] _sprites;

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            var leftEnd = tilemap.GetTile(position + Vector3Int.left) != this;
            var rightEnd = tilemap.GetTile(position + Vector3Int.right) != this;

            var layouts = _sprites.Length / 4;
            var layout = (int)((uint)(position.x * 73856093 ^ position.y * 19349663) % (uint)layouts);

            tileData.sprite = _sprites[layout * 4 + (leftEnd ? 1 : 0) + (rightEnd ? 2 : 0)];
            tileData.color = Color.white;
            tileData.flags = TileFlags.LockTransform;
            tileData.colliderType = Tile.ColliderType.None;
            tileData.transform = Matrix4x4.identity;
        }

        public override void RefreshTile(Vector3Int position, ITilemap tilemap)
        {
            tilemap.RefreshTile(position);
            if (tilemap.GetTile(position + Vector3Int.left) == this) tilemap.RefreshTile(position + Vector3Int.left);
            if (tilemap.GetTile(position + Vector3Int.right) == this) tilemap.RefreshTile(position + Vector3Int.right);
        }
    }
}
