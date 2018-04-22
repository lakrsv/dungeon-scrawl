// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="GameResource.cs" author="Lars" company="None">
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

namespace UI
{
    using DG.Tweening;

    using UnityEngine;
    using UnityEngine.UI;

    public class GameResource : MonoBehaviour
    {
        [SerializeField]
        private Sprite _disabledSprite;

        [SerializeField]
        private Sprite _enabledSprite;

        [SerializeField]
        private Image _image;

        private Ease _ease = Ease.OutElastic;

        private float _animationDuration = 0.5f;

        private float _scaleAmount = 1.0f;

        private Color _badColor = Color.red;

        public void Enable()
        {
            if (_image.sprite == _enabledSprite) return;

            _image.sprite = _enabledSprite;
            PlayAnimation(false);
        }

        public void Disable()
        {
            if (_image.sprite == _disabledSprite) return;

            _image.sprite = _disabledSprite;
            PlayAnimation(true);
        }

        private void PlayAnimation(bool damage)
        {
            var sequence = DOTween.Sequence();
            sequence.Append(transform.DOScale(_scaleAmount, _animationDuration).SetRelative().SetEase(_ease));
            sequence.Append(transform.DOScale(-_scaleAmount, _animationDuration).SetRelative().SetEase(_ease));

            if (damage)
            {
                _image.DOColor(_badColor, 0.25f).SetEase(Ease.OutQuint);
                _image.DOColor(Color.white, 0.25f).SetDelay(0.25f).SetEase(Ease.OutBack);
            }
        }
    }
}