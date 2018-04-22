// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="GameOverScreenTweener.cs" author="Lars" company="None">
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

namespace UI.Tweeners
{
    using System.Collections;

    using DG.Tweening;

    using UnityEngine;
    using UnityEngine.UI;

    public class GameOverScreenTweener : MonoBehaviour
    {
        [SerializeField]
        private Button _continueButton;

        [SerializeField]
        private Button _exitButton;

        [SerializeField]
        private Text _gameOverText;

        private void Awake()
        {
            _gameOverText.gameObject.SetActive(false);

            _continueButton.gameObject.SetActive(false);
            _exitButton.gameObject.SetActive(false);
        }

        private IEnumerator Start()
        {
            yield return null;

            _gameOverText.transform.DOMoveY(-10, 1.0f).From().SetEase(Ease.OutBack)
                .OnStart(() => _gameOverText.gameObject.SetActive(true));

            _continueButton.transform.DOMoveY(-10, 1.0f).From().SetEase(Ease.OutBack).SetDelay(0.25f)
                .OnStart(() => _continueButton.gameObject.SetActive(true));

            _exitButton.transform.DOMoveY(-10, 1.0f).From().SetEase(Ease.OutBack).SetDelay(0.5f)
                .OnStart(() => _exitButton.gameObject.SetActive(true));
        }
    }
}