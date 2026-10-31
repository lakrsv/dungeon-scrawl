// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="PauseMenu.cs" author="Lars" company="None">
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
// //   The pause menu, and the way into the options.
// // </summary>
// // --------------------------------------------------------------------------------------------------------------------

namespace UI
{
    using System.Collections;

    using Controllers;

    using DG.Tweening;

    using Sound;

    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.SceneManagement;
    using UnityEngine.UI;

    /// <summary>
    /// Escape during a level stops time and opens the pause menu: resume, options, or quit to the title. In the
    /// options, Escape goes back; in the pause menu, it resumes. Everything here runs on unscaled time.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        private const float FadeDuration = 0.2f;

        // How dark the game gets behind the menu.
        private const float DimAlpha = 0.8f;

        [SerializeField]
        private Image _dim;

        [SerializeField]
        private CanvasGroup _pause;

        [SerializeField]
        private OptionsMenu _options;

        [SerializeField]
        private GameObject _resumeButton;

        [SerializeField]
        private GameObject _optionsButton;

        private bool _open;

        private bool _leaving;

        public void Open()
        {
            _open = true;
            GameController.Instance.SetPaused(true);
            Jukebox.Select();

            _dim.gameObject.SetActive(true);
            _dim.DOKill();
            _dim.DOFade(DimAlpha, FadeDuration).SetUpdate(true);
            Show(_pause, _resumeButton);
        }

        public void Resume()
        {
            _open = false;
            GameController.Instance.SetPaused(false);
            Jukebox.Select();

            Hide(_pause);
            _dim.DOKill();
            _dim.DOFade(0f, FadeDuration).SetUpdate(true).OnComplete(() => _dim.gameObject.SetActive(false));
        }

        public void OpenOptions()
        {
            Jukebox.Select();
            Hide(_pause);
            _options.Open();
        }

        public void CloseOptions()
        {
            Jukebox.Select();
            _options.Close();
            Show(_pause, _optionsButton);
        }

        public void QuitToTitle()
        {
            if (_leaving) return;
            Jukebox.Select();
            StartCoroutine(Leave());
        }

        private IEnumerator Leave()
        {
            // Fade all the way to the game's own dark, and the splash screen fades in out of it.
            _leaving = true;
            _pause.interactable = false;
            _dim.DOKill();
            _dim.DOFade(1f, 0.35f).SetUpdate(true);
            _pause.DOFade(0f, 0.35f).SetUpdate(true);
            yield return new WaitForSecondsRealtime(0.35f);
            GameController.Instance.AbandonRun();
            SceneManager.LoadScene("SplashScreen");
        }

        private void Show(CanvasGroup screen, GameObject selected)
        {
            screen.gameObject.SetActive(true);
            screen.interactable = true;
            screen.DOKill();
            screen.alpha = 0f;
            screen.DOFade(1f, FadeDuration).SetUpdate(true);
            EventSystem.current.SetSelectedGameObject(selected);
        }

        private void Hide(CanvasGroup screen)
        {
            screen.interactable = false;
            screen.DOKill();
            screen.alpha = 0f;
            screen.gameObject.SetActive(false);
            EventSystem.current.SetSelectedGameObject(null);
        }

        private void Awake()
        {
            _dim.gameObject.SetActive(false);
            _pause.gameObject.SetActive(false);
            _options.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_leaving || !Input.GetKeyDown(KeyCode.Escape)) return;

            if (_options.gameObject.activeSelf) CloseOptions();
            else if (_open) Resume();
            else if (GameController.Instance.IsPlaying) Open();
        }
    }
}
