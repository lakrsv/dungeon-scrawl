// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="BossBar.cs" author="Lars" company="None">
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
// //   THE SCRAWL's health, at the bottom of the screen during the fight.
// // </summary>
// // --------------------------------------------------------------------------------------------------------------------

namespace Boss
{
    using DG.Tweening;

    using UnityEngine;

    /// <summary>
    /// The boss bar: THE SCRAWL, with a bar each for its left hand, its head and its right hand. It fades in when the
    /// Scrawl rises and out when it dies.
    /// </summary>
    public class BossBar : MonoBehaviour
    {
        public const int LeftHand = 0;

        public const int Head = 1;

        public const int RightHand = 2;

        [SerializeField]
        private CanvasGroup _group;

        // The red fills, scaled from the left.
        [SerializeField]
        private RectTransform[] _fills;

        public void Show()
        {
            gameObject.SetActive(true);
            foreach (var fill in _fills) fill.localScale = Vector3.one;
            _group.DOKill();
            _group.alpha = 0f;
            _group.DOFade(1f, 0.6f);
        }

        public void Hide()
        {
            _group.DOKill();
            _group.DOFade(0f, 0.8f).OnComplete(() => gameObject.SetActive(false));
        }

        public void Set(int part, float fraction)
        {
            var fill = _fills[part];
            fill.DOKill();
            fill.DOScaleX(Mathf.Clamp01(fraction), 0.3f).SetEase(Ease.OutQuad);
        }
    }
}
