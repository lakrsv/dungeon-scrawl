// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="SplashScreenTweener.cs" author="Lars" company="None">
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
    using UnityEngine.SceneManagement;
    using UnityEngine.UI;

    public class SplashScreenTweener : MonoBehaviour
    {
        [SerializeField]
        private Text _title;

        [SerializeField]
        private Text _continueText;


        private IEnumerator DoTitleTweenSequence()
        {
            var appearSequence = DOTween.Sequence().OnStart(() => _title.gameObject.SetActive(true));

            var titleAppear = _title.rectTransform.DOScale(new Vector3(1.75f, 1.75f, 1.75f), 0.75f)
                .From()
                .SetEase(Ease.InOutElastic);
            appearSequence.Append(titleAppear);

            var titleShake = _title.rectTransform.DOShakeAnchorPos(1.0f, 2f);
            appearSequence.Append(titleShake);

            yield return null;
        }

        private IEnumerator DoContinueTweenSequence()
        {
            _continueText.DOFade(0f, 1.0f).From().OnStart(() => _continueText.gameObject.SetActive(true))
                .SetDelay(0.5f);

            yield return new WaitForSeconds(0.5f);

            _continueText.DOText("Press any key to Continue", 1.0f);

            var flashText = DOTween.Sequence();
            flashText.Append(_continueText.DOText("Press any key to Continue_", 0f));
            flashText.AppendInterval(0.5f);
            flashText.Append(_continueText.DOText("Press any key to Continue", 0f));
            flashText.AppendInterval(0.5f);
            flashText.SetLoops(-1);
        }

        private IEnumerator Start()
        {
            DOTween.Init();
            yield return null;
            StartCoroutine(DoTitleTweenSequence());
            StartCoroutine(DoContinueTweenSequence());
        }

        private void Awake()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            _title.gameObject.SetActive(false);
            _continueText.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (Input.anyKeyDown)
            {
                SceneManager.LoadScene("MainMenu");
            }
        }
    }
}