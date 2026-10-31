// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="FadeIn.cs" author="Lars" company="None">
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
// //   Fades a piece of UI in when its screen opens.
// // </summary>
// // --------------------------------------------------------------------------------------------------------------------

namespace UI
{
    using DG.Tweening;

    using UnityEngine;

    /// <summary>
    /// Fades its CanvasGroup in from nothing when the screen opens, so screens never cut in from the dark.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class FadeIn : MonoBehaviour
    {
        [SerializeField]
        private float _duration = 0.75f;

        [SerializeField]
        private float _delay;

        private CanvasGroup _group;

        private Tween _tween;

        /// <summary>
        /// Brings the fade to full over the given time, for when the screen is left before it has finished.
        /// </summary>
        public void Finish(float duration)
        {
            if (_tween != null) _tween.Kill();
            _tween = _group.DOFade(1f, duration);
        }

        private void Start()
        {
            _group = GetComponent<CanvasGroup>();
            _tween = _group.DOFade(0f, _duration).From().SetDelay(_delay).SetEase(Ease.OutQuad);
        }
    }
}
