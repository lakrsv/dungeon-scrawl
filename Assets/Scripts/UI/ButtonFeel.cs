// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="ButtonFeel.cs" author="Lars" company="None">
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
// //   How a menu button answers the pointer and the keyboard.
// // </summary>
// // --------------------------------------------------------------------------------------------------------------------

namespace UI
{
    using DG.Tweening;

    using Sound;

    using UnityEngine;
    using UnityEngine.EventSystems;

    /// <summary>
    /// Grows a button and ticks when it is pointed at or selected with the keys, like the main menu's buttons. It runs
    /// on unscaled time, so it works in the pause menu while time stands still.
    /// </summary>
    public class ButtonFeel : MonoBehaviour, IPointerEnterHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField]
        private float _scale = 1.1f;

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Pointing at a button selects it, so the mouse and the keys never disagree about which one is lit.
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(gameObject);
        }

        public void OnSelect(BaseEventData eventData)
        {
            transform.DOKill();
            transform.DOScale(new Vector3(_scale, _scale, 1f), 0.2f).SetEase(Ease.OutBack).SetUpdate(true);
            Jukebox.Hover();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            transform.DOKill();
            transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        private void OnDisable()
        {
            transform.DOKill();
            transform.localScale = Vector3.one;
        }
    }
}
