// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="OptionsMenu.cs" author="Lars" company="None">
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
// //   The options: music and sound volume, and the quick-move keys.
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
    /// The options screen. Each row has arrows either side of its value for the mouse; with the keys, up and down
    /// pick a row and left and right change it. The last row is the BACK button. The same settings as the main menu's
    /// corner buttons, kept the same way.
    /// </summary>
    public class OptionsMenu : MonoBehaviour
    {
        private const int Back = 3;

        private static readonly Color Gold = new Color(0.855f, 0.831f, 0.369f);

        private static readonly Color Plum = new Color(0.267f, 0.141f, 0.204f);

        private static readonly Color Cream = new Color(0.871f, 0.933f, 0.839f);

        private static readonly Color Steel = new Color(0.522f, 0.584f, 0.631f);

        [SerializeField]
        private Image[] _musicBars;

        [SerializeField]
        private Image[] _soundBars;

        [SerializeField]
        private Text _moveKeys;

        // MUSIC, SOUND and MOVE KEYS, lit when chosen with the keys.
        [SerializeField]
        private Text[] _labels;

        [SerializeField]
        private GameObject _backButton;

        private int _row;

        public void Open()
        {
            gameObject.SetActive(true);
            var group = GetComponent<CanvasGroup>();
            group.DOKill();
            group.alpha = 0f;
            group.DOFade(1f, 0.2f).SetUpdate(true);
            Choose(0);
            Refresh();
        }

        public void Close()
        {
            EventSystem.current.SetSelectedGameObject(null);
            gameObject.SetActive(false);
        }

        public void MusicDown()
        {
            Change(0, -1);
        }

        public void MusicUp()
        {
            Change(0, 1);
        }

        public void SoundDown()
        {
            Change(1, -1);
        }

        public void SoundUp()
        {
            Change(1, 1);
        }

        public void SwitchMoveKeys()
        {
            Change(2, 1);
        }

        private void Change(int row, int step)
        {
            switch (row)
            {
                case 0:
                    Volume.MusicLevel += step;
                    break;
                case 1:
                    Volume.SoundLevel += step;
                    break;
                case 2:
                    MoveKeys.Wasd = !MoveKeys.Wasd;
                    _moveKeys.transform.DOKill(true);
                    _moveKeys.transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0f), 0.25f, 1, 0f).SetUpdate(true);
                    break;
                default:
                    return;
            }

            // Played after the change, so turning the sound down is heard as it happens.
            Jukebox.Select();
            Refresh();
        }

        private void Choose(int row)
        {
            _row = Mathf.Clamp(row, 0, Back);
            for (var i = 0; i < _labels.Length; i++) _labels[i].color = i == _row ? Cream : Steel;
            EventSystem.current.SetSelectedGameObject(_row == Back ? _backButton : null);
        }

        private void Refresh()
        {
            for (var i = 0; i < _musicBars.Length; i++) _musicBars[i].color = i < Volume.MusicLevel ? Gold : Plum;
            for (var i = 0; i < _soundBars.Length; i++) _soundBars[i].color = i < Volume.SoundLevel ? Gold : Plum;
            _moveKeys.text = MoveKeys.Wasd ? "WASD" : "LETTERS";
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow) && _row > 0)
            {
                Choose(_row - 1);
                if (_row != Back) Jukebox.Hover();
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) && _row < Back)
            {
                Choose(_row + 1);
                if (_row != Back) Jukebox.Hover();
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                Change(_row, -1);
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                Change(_row, 1);
            }
        }
    }
}
