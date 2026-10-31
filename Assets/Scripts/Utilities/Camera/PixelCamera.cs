// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="PixelCamera.cs" author="Lars" company="None">
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
// //   Sizes an orthographic camera so the pixel art is drawn at a whole-number zoom.
// // </summary>
// // --------------------------------------------------------------------------------------------------------------------

namespace Utilities.Camera
{
    using UnityEngine;

    /// <summary>
    /// Sets the orthographic size for <see cref="PixelPerfect.Zoom"/>. It only resizes when the screen does, so
    /// tweens on the orthographic size (the level transitions) are left alone.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class PixelCamera : MonoBehaviour
    {
        private UnityEngine.Camera _camera;

        private int _screenHeight;

        public void Snap()
        {
            _camera.orthographicSize = PixelPerfect.OrthographicSize;
            _screenHeight = Screen.height;
        }

        private void Awake()
        {
            _camera = GetComponent<UnityEngine.Camera>();
            Snap();
        }

        private void LateUpdate()
        {
            if (Screen.height != _screenHeight) Snap();
        }
    }
}
