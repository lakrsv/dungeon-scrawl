// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="VolumeButton.cs" author="Lars" company="None">
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
// //   A main menu button that sets the music or the sound volume.
// // </summary>
// // --------------------------------------------------------------------------------------------------------------------

namespace Sound
{
    using DG.Tweening;

    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    /// <summary>
    /// A main menu button that turns the music or the sound down a level per click, from full to off and round
    /// again. Its icon shows the level.
    /// </summary>
    public class VolumeButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public enum Channel
        {
            Music,

            Sound
        }

        [SerializeField]
        private Channel _channel;

        [SerializeField]
        private Image _icon;

        [SerializeField]
        [Tooltip("One icon per level, from off to full.")]
        private Sprite[] _levels;

        private int Level
        {
            get
            {
                return _channel == Channel.Music ? Volume.MusicLevel : Volume.SoundLevel;
            }

            set
            {
                if (_channel == Channel.Music) Volume.MusicLevel = value;
                else Volume.SoundLevel = value;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Level = Level == 0 ? Volume.MaxLevel : Level - 1;
            ShowLevel();

            // Played at the new sound volume, so turning the sound down is heard as it happens.
            Jukebox.Select();
            _icon.transform.DOKill(true);
            _icon.transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0f), 0.25f, 1, 0f);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            transform.DOScale(new Vector3(1.25f, 1.25f, 1f), 0.25f).SetEase(Ease.OutBack);
            Jukebox.Hover();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
        }

        private void Start()
        {
            ShowLevel();
        }

        private void ShowLevel()
        {
            _icon.sprite = _levels[Level];
        }
    }
}
