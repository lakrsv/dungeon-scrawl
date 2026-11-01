// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="ScrawlHand.cs" author="Lars" company="None">
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
// //   One of the Scrawl's hands: its poses and how it moves between them.
// // </summary>
// // --------------------------------------------------------------------------------------------------------------------

namespace Boss
{
    using System.Collections;

    using DG.Tweening;

    using UnityEngine;

    using Utilities.Game;

    /// <summary>
    /// One of the Scrawl's hands, rising out of the void beside the bridge. It waits beside the bridge, rises and
    /// burns as it charges, slams down onto the lanes, grips the edge while it recovers (when it can be hurt), and
    /// sinks back into the void when it breaks. Drawn by Tools/PixelArt/boss.py; the left hand is the art, the right
    /// hand is it mirrored.
    /// </summary>
    public class ScrawlHand : MonoBehaviour
    {
        // Sorting: under the knight while it waits or grips, over the knight when it comes down on the bridge.
        private const int Behind = 8;

        private const int InFront = 12;

        // Where the wrist is (art pixels from the middle of the left hand's sprite), so the arm meets it.
        private static readonly Vector2 SpreadWrist = new Vector2(-19f, 2f);

        private static readonly Vector2 GripWrist = new Vector2(-17f, 0f);

        [SerializeField]
        private SpriteRenderer _body;

        [SerializeField]
        private SpriteRenderer _arm;

        [SerializeField]
        private SpriteFlash _flash;

        [SerializeField]
        private Sprite _spread;

        [SerializeField]
        private Sprite _charging;

        [SerializeField]
        private Sprite _grip;

        // -1 for the hand left of the bridge, 1 for the right.
        [SerializeField]
        private int _side = -1;

        private Vector3 _idle;

        private Tween _bob;

        private Tween _tremble;

        public int Side
        {
            get { return _side; }
        }

        public int Health { get; set; }

        public int MaxHealth { get; private set; }

        public bool Alive
        {
            get { return Health > 0; }
        }

        public void Setup(Vector3 idle, int health)
        {
            _idle = idle;
            Health = MaxHealth = health;
            SetPose(_spread, SpreadWrist, Behind);
        }

        /// <summary>
        /// Claws up out of the void to wait beside the bridge.
        /// </summary>
        public IEnumerator Rise(float duration)
        {
            gameObject.SetActive(true);
            transform.position = _idle + new Vector3(0f, -8f, 0f);
            yield return transform.DOMove(_idle, duration).SetEase(Ease.OutBack).WaitForCompletion();
            Bob();
        }

        /// <summary>
        /// Rises over the lanes it means to hit, talons splayed, burning and trembling.
        /// </summary>
        public void Raise(Vector3 over, float duration)
        {
            StopBob();
            SetPose(_charging, SpreadWrist, InFront);
            transform.DOKill();
            transform.DOMove(over, Mathf.Min(0.6f, duration * 0.4f)).SetEase(Ease.OutQuad);
            _tremble = _body.transform.DOShakePosition(duration, new Vector3(0.06f, 0.06f, 0f), 30, 90f, false, false)
                .SetLoops(-1);
        }

        /// <summary>
        /// Comes down on the bridge.
        /// </summary>
        public IEnumerator Slam(Vector3 at)
        {
            StopTremble();
            transform.DOKill();
            yield return transform.DOMove(at, 0.12f).SetEase(Ease.InQuad).WaitForCompletion();
            SetPose(_spread, SpreadWrist, InFront);
            _body.transform.DOPunchScale(new Vector3(0.15f, -0.15f, 0f), 0.25f, 1, 0f);
        }

        /// <summary>
        /// Drags back to grip the edge of the bridge while it recovers: the window to hurt it.
        /// </summary>
        public void Grip(Vector3 at)
        {
            SetPose(_grip, GripWrist, Behind);
            transform.DOKill();
            transform.DOMove(at, 0.3f).SetEase(Ease.OutQuad);
        }

        /// <summary>
        /// Lets go and returns to wait beside the bridge.
        /// </summary>
        public void Lift()
        {
            SetPose(_spread, SpreadWrist, Behind);
            transform.DOKill();
            transform.DOMove(_idle, 0.5f).SetEase(Ease.InOutQuad).OnComplete(Bob);
        }

        public void Hit()
        {
            if (_flash != null) _flash.Flash();
            _body.transform.DOComplete();
            _body.transform.DOShakePosition(0.25f, 0.2f, 30);
        }

        /// <summary>
        /// Broken: it goes limp and sinks back into the void.
        /// </summary>
        public IEnumerator Sink()
        {
            StopBob();
            StopTremble();
            SetPose(_spread, SpreadWrist, Behind);
            transform.DOKill();
            _body.DOFade(0f, 1.4f);
            _arm.DOFade(0f, 1.2f);
            yield return transform.DOMove(transform.position + new Vector3(0f, -6f, 0f), 1.4f).SetEase(Ease.InCubic).WaitForCompletion();
            gameObject.SetActive(false);
        }

        private void SetPose(Sprite sprite, Vector2 wrist, int order)
        {
            _body.sprite = sprite;
            _body.flipX = _side > 0;
            _arm.flipX = _side > 0;
            _body.sortingOrder = order;
            _arm.sortingOrder = order - 1;
            // The right hand is the left one mirrored, wrist and all.
            _arm.transform.localPosition = new Vector3(wrist.x / 16f * (_side < 0 ? 1f : -1f), wrist.y / 16f, 0f);
        }

        private void Bob()
        {
            StopBob();
            _bob = transform.DOMoveY(_idle.y + 0.2f, 1.6f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
        }

        private void StopBob()
        {
            if (_bob != null) _bob.Kill();
            _bob = null;
        }

        private void StopTremble()
        {
            if (_tremble != null) _tremble.Kill();
            _tremble = null;
            _body.transform.localPosition = Vector3.zero;
        }
    }
}
