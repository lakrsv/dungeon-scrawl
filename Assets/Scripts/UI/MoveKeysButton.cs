// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="MoveKeysButton.cs" author="Lars" company="None">
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
// //   A main menu button that picks the keys for quick moves.
// // </summary>
// // --------------------------------------------------------------------------------------------------------------------

namespace UI
{
    using Controllers;

    using DG.Tweening;

    using Sound;

    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    /// <summary>
    /// A main menu button that switches the quick moves (used while nothing hostile is in sight) between WASD, ZQSD
    /// and a random letter per direction.
    /// </summary>
    public class MoveKeysButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        // Between the icon and the label.
        private const float Gap = 20f;

        [SerializeField]
        private RectTransform _icon;

        [SerializeField]
        private Text _label;

        // Less than the other buttons' 1.25: this one is wider, and would grow into the IMPOSSIBLE button.
        [SerializeField]
        private float _hoverScale = 1.15f;

        public void OnPointerClick(PointerEventData eventData)
        {
            MoveKeys.Next();
            ShowKeys();
            Jukebox.Select();
            _label.transform.DOKill(true);
            _label.transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0f), 0.25f, 1, 0f);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            transform.DOScale(new Vector3(_hoverScale, _hoverScale, 1f), 0.25f).SetEase(Ease.OutBack);
            Jukebox.Hover();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
        }

        private void Start()
        {
            ShowKeys();
        }

        // Centres the icon and the label together in the frame, on whole art pixels (4 canvas units).
        private void ShowKeys()
        {
            _label.text = MoveKeys.Name;

            var iconWidth = _icon.sizeDelta.x;
            // The label's preferred width ends with the spacing after its last letter: one font pixel, which is
            // the font size / 10.24 for Munro.
            var width = iconWidth + Gap + _label.preferredWidth - _label.fontSize / 10.24f;
            var left = Mathf.Round((((RectTransform)transform).rect.width - width) / 2f / 4f) * 4f;
            _icon.anchoredPosition = new Vector2(left + iconWidth / 2f, _icon.anchoredPosition.y);
            var label = _label.rectTransform;
            label.anchoredPosition = new Vector2(left + iconWidth + Gap, label.anchoredPosition.y);
        }
    }
}
