// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="PixelCanvas.cs" author="Lars" company="None">
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
// //   Scales a screen-space canvas by the same whole-number zoom as the world.
// // </summary>
// // --------------------------------------------------------------------------------------------------------------------

namespace UI
{
    using UnityEngine;
    using UnityEngine.UI;

    using Utilities.Camera;

    /// <summary>
    /// Scales a screen-space canvas so its pixel art is drawn at <see cref="PixelPerfect.UiZoom"/>, a whole number
    /// of screen pixels per art pixel.
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    public class PixelCanvas : MonoBehaviour
    {
        // Canvas units per art pixel in this canvas's layout. The HUD was laid out at 4 (a 16px heart is 64 units).
        [SerializeField]
        private float _unitsPerPixel = 4f;

        private CanvasScaler _scaler;

        private int _screenHeight;

        private void Awake()
        {
            _scaler = GetComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            Apply();
        }

        private void Update()
        {
            if (Screen.height != _screenHeight) Apply();
        }

        private void Apply()
        {
            _scaler.scaleFactor = PixelPerfect.UiZoom / _unitsPerPixel;
            _screenHeight = Screen.height;
        }
    }
}
