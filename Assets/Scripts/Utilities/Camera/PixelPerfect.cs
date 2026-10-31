// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="PixelPerfect.cs" author="Lars" company="None">
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
// //   Whole-number scaling for the pixel art.
// // </summary>
// // --------------------------------------------------------------------------------------------------------------------

namespace Utilities.Camera
{
    using UnityEngine;

    /// <summary>
    /// One world pixel is always <see cref="Zoom"/> screen pixels, and one interface pixel <see cref="UiZoom"/>, so
    /// no pixel is ever drawn wider than its neighbour.
    /// </summary>
    public static class PixelPerfect
    {
        public const int PixelsPerUnit = 16;

        // World pixels shown top to bottom: about 10 tiles, as the game was first framed. 1080p draws at 7x.
        private const int TargetHeight = 160;

        // Interface pixels: the HUD was laid out at 4x for 1080p.
        private const int UiTargetHeight = 270;

        public static int Zoom
        {
            get
            {
                return Scale(TargetHeight);
            }
        }

        public static int UiZoom
        {
            get
            {
                return Scale(UiTargetHeight);
            }
        }

        public static float OrthographicSize
        {
            get
            {
                return Screen.height / (2f * PixelsPerUnit * Zoom);
            }
        }

        private static int Scale(int targetHeight)
        {
            return Mathf.Max(1, (int)System.Math.Round(Screen.height / (double)targetHeight, System.MidpointRounding.AwayFromZero));
        }
    }
}
