// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="ScrawlHead.cs" author="Lars" company="None">
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
// //   The Scrawl's head: its mask, and the beam it speaks.
// // </summary>
// // --------------------------------------------------------------------------------------------------------------------

namespace Boss
{
    using System.Collections;
    using System.Text;

    using DG.Tweening;

    using TMPro;

    using UnityEngine;

    using Utilities.Game;

    /// <summary>
    /// The Scrawl's head: a bone mask in a hood of ink at the far end of the bridge. It drifts out of the dark,
    /// charges and speaks its beam (a stream of glowing letters down one lane), cracks when badly hurt, and
    /// shatters when it dies. Drawn by Tools/PixelArt/boss.py.
    /// </summary>
    public class ScrawlHead : MonoBehaviour
    {
        private const string BeamWords = "SCRAWLEDINKUNWRITTENWORDS";

        [SerializeField]
        private SpriteRenderer _mask;

        [SerializeField]
        private SpriteFlash _flash;

        [SerializeField]
        private Sprite _whole;

        [SerializeField]
        private Sprite _wholeCharging;

        [SerializeField]
        private Sprite _cracked;

        [SerializeField]
        private Sprite _crackedCharging;

        // The beam: a glowing column, tiled down the lane, with the letters running down it.
        [SerializeField]
        private SpriteRenderer _beam;

        [SerializeField]
        private TextMeshPro _letters;

        private bool _charging;

        private Tween _bob;

        private Tween _tremble;

        public int Health { get; set; }

        public int MaxHealth { get; private set; }

        public bool Alive
        {
            get { return Health > 0; }
        }

        private bool Cracked
        {
            get { return Health * 2 <= MaxHealth; }
        }

        public void Setup(int health)
        {
            Health = MaxHealth = health;
            _beam.gameObject.SetActive(false);
            _letters.gameObject.SetActive(false);
            ShowFace();
        }

        /// <summary>
        /// Drifts down out of the dark at the far end of the bridge.
        /// </summary>
        public IEnumerator Appear(Vector3 at, float duration)
        {
            gameObject.SetActive(true);
            transform.position = at + new Vector3(0f, 4f, 0f);
            _mask.color = new Color(1f, 1f, 1f, 0f);
            _mask.DOFade(1f, duration);
            yield return transform.DOMove(at, duration).SetEase(Ease.OutCubic).WaitForCompletion();
            Bob(at);
        }

        /// <summary>
        /// Drifts closer along the bridge: what it does once both hands are gone.
        /// </summary>
        public IEnumerator Advance(Vector3 to)
        {
            if (_bob != null) _bob.Kill();
            yield return transform.DOMove(to, 2f).SetEase(Ease.InOutSine).WaitForCompletion();
            Bob(to);
        }

        /// <summary>
        /// Its eyes turn gold and the stitches of its mouth burst: the beam is coming.
        /// </summary>
        public void Charge(float duration)
        {
            _charging = true;
            ShowFace();
            _tremble = _mask.transform.DOShakePosition(duration, new Vector3(0.05f, 0.05f, 0f), 30, 90f, false, false).SetLoops(-1);
        }

        /// <summary>
        /// Speaks: a stream of glowing letters down a lane, from its mouth to the bottom of the bridge.
        /// </summary>
        public IEnumerator Fire(float laneX, float bottom, float duration)
        {
            StopTremble();
            // The mouth's seam is 14 art pixels below the middle of the mask.
            var mouth = transform.position.y - 0.875f;
            var length = mouth - bottom;

            _beam.gameObject.SetActive(true);
            _beam.transform.position = new Vector3(laneX, (mouth + bottom) / 2f, 0f);
            _beam.size = new Vector2(_beam.size.x, length);
            _beam.color = Color.white;

            var text = new StringBuilder();
            var start = Random.Range(0, BeamWords.Length);
            for (var i = 0; i < Mathf.CeilToInt(length * 2f) + 2; i++)
            {
                text.Append(BeamWords[(start + i) % BeamWords.Length]);
                text.Append('\n');
            }

            _letters.gameObject.SetActive(true);
            _letters.text = text.ToString();
            _letters.color = Color.white;
            _letters.transform.position = new Vector3(laneX, mouth, 0f);
            _letters.transform.DOMoveY(mouth - 1f, duration).SetEase(Ease.Linear);

            yield return new WaitForSeconds(duration);
            _beam.DOFade(0f, 0.2f);
            DOTween.To(() => _letters.alpha, a => _letters.alpha = a, 0f, 0.2f);
            yield return new WaitForSeconds(0.2f);
            _beam.gameObject.SetActive(false);
            _letters.gameObject.SetActive(false);
            _charging = false;
            ShowFace();
        }

        public void Hit()
        {
            if (_flash != null) _flash.Flash();
            _mask.transform.DOComplete();
            _mask.transform.DOShakePosition(0.3f, 0.2f, 30);
            ShowFace();
        }

        /// <summary>
        /// Dead: it shakes, flashes and shatters, and the ink drains away into the void.
        /// </summary>
        public IEnumerator Shatter()
        {
            if (_bob != null) _bob.Kill();
            StopTremble();
            _charging = false;
            _beam.gameObject.SetActive(false);
            _letters.gameObject.SetActive(false);
            ShowFace();
            for (var i = 0; i < 4; i++)
            {
                if (_flash != null) _flash.Flash();
                yield return _mask.transform.DOShakePosition(0.35f, 0.3f, 40).WaitForCompletion();
            }

            _mask.DOFade(0f, 1.2f);
            transform.DOScale(1.15f, 1.2f).SetEase(Ease.OutQuad);
            yield return transform.DOMoveY(transform.position.y - 2f, 1.2f).SetEase(Ease.InQuad).WaitForCompletion();
            gameObject.SetActive(false);
        }

        public void StopAll()
        {
            StopTremble();
            _charging = false;
            _beam.gameObject.SetActive(false);
            _letters.gameObject.SetActive(false);
            ShowFace();
        }

        private void ShowFace()
        {
            _mask.sprite = Cracked ? (_charging ? _crackedCharging : _cracked) : (_charging ? _wholeCharging : _whole);
        }

        private void Bob(Vector3 at)
        {
            if (_bob != null) _bob.Kill();
            transform.position = at;
            _bob = transform.DOMoveY(at.y + 0.15f, 2.2f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
        }

        private void StopTremble()
        {
            if (_tremble != null) _tremble.Kill();
            _tremble = null;
            _mask.transform.localPosition = Vector3.zero;
        }
    }
}
