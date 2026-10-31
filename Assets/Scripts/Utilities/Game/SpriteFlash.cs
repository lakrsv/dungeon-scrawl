// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="SpriteFlash.cs" author="Lars" company="None">
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
// //   Blinks a sprite white when it is hit.
// // </summary>
// // --------------------------------------------------------------------------------------------------------------------

namespace Utilities.Game
{
    using DG.Tweening;

    using UnityEngine;

    /// <summary>
    /// Blinks a sprite to a flat colour and fades back. Needs the DungeonScrawl/Sprite Flash shader.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteFlash : MonoBehaviour
    {
        private static readonly int FlashAmount = Shader.PropertyToID("_FlashAmount");

        [SerializeField]
        private float _duration = 0.2f;

        private SpriteRenderer _renderer;

        private MaterialPropertyBlock _block;

        private Tween _tween;

        private float _amount;

        public void Flash()
        {
            if (_tween != null) _tween.Kill();

            Set(1f);
            _tween = DOTween.To(() => _amount, Set, 0f, _duration).SetEase(Ease.InQuad);
        }

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _block = new MaterialPropertyBlock();
        }

        private void OnDisable()
        {
            // Pooled actors come back without a lingering flash.
            if (_tween != null) _tween.Kill();
            if (_renderer != null) Set(0f);
        }

        private void Set(float amount)
        {
            _amount = amount;
            _renderer.GetPropertyBlock(_block);
            _block.SetFloat(FlashAmount, amount);
            _renderer.SetPropertyBlock(_block);
        }
    }
}
