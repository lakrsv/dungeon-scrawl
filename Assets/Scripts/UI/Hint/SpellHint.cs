// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="SpellHint.cs" author="Lars" company="None">
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

namespace UI.Hint
{
    using System;
    using System.Collections.Generic;

    using DG.Tweening;

    using TMPro;

    using UnityEngine;
    using UnityEngine.UI;

    using Utilities.Game.ObjectPool;

    public class SpellHint : MonoBehaviour, IPoolable
    {
        public enum IconType
        {
            Attack,
            Chest,
            DodgeLeft,
            DodgeRight
        }

        // One art pixel, in the prompts' units (a world unit is 16).
        private const float Px = 1f / 16f;

        // The plaque round a word: its frame is 3 wide at the sides, and the word sits 2 in from that. An icon takes
        // 7 more, and 2 between it and the word.
        private const int Frame = 3;

        private const int Margin = 2;

        private const int IconWidth = 7;

        private const int IconGap = 2;

        // The palette: cream letters, gold for the ones typed so far, red for a word that a wrong letter broke.
        private const string TypedColor = "#dad45e";

        private static readonly Color TextColor = new Color(0.871f, 0.933f, 0.839f);

        private static readonly Color CompleteColor = new Color(0.855f, 0.831f, 0.369f);

        private static readonly Color RejectColor = new Color(0.816f, 0.275f, 0.282f);

        private readonly List<Tween> _currentTweens = new List<Tween>();

        private readonly List<Sequence> _currentSequences = new List<Sequence>();

        private static readonly Vector2 AboveTarget = new Vector2(0f, 1.0f);

        private Vector2 _followOffset = AboveTarget;

        [SerializeField]
        private float _appearDuration = 0.75f;

        [SerializeField]
        private float _wordCompleteAnimationDuration = 0.25f;

        private Canvas _canvas;

        private bool _follow;

        private Transform _followTarget;

        [SerializeField]
        private float _movementSpeed = 2.5f;

        [SerializeField]
        private TextMeshProUGUI _text;

        [SerializeField]
        private Image _icon;

        [SerializeField]
        private Sprite _chestIcon;

        [SerializeField]
        private Sprite _attackIcon;

        [SerializeField]
        private Sprite _dodgeLeftIcon;

        [SerializeField]
        private Sprite _dodgeRightIcon;

        // A draining bar under the plaque, for a prompt with a deadline (the boss's dodges).
        [SerializeField]
        private GameObject _timer;

        [SerializeField]
        private RectTransform _timerFill;

        private Tween _timerTween;

        private Vector2 _originalLocalPosition;

        // Where the word sits in its box when nothing is jolting it.
        private Vector3 _wordPosition;

        private Tween _jolt;

        private Tween _flash;

        public bool IsEnabled { get; private set; }

        public bool WordWasInvalid { get; private set; }

        public bool WordComplete { get; private set; }

        public string Word { get; private set; }

        public void Disable()
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            if (HintCache.Instance.Contains(this)) HintCache.Instance.Remove(this);
            if (_timerTween != null) _timerTween.Kill();

            DoDisappearAnimation(
                _appearDuration,
                () =>
                    {
                        IsEnabled = false;
                        _text.SetText(string.Empty);
                        gameObject.SetActive(false);
                    });
        }

        public void Enable()
        {
            IsEnabled = true;

            Word = null;
            _text.SetText(string.Empty);
            gameObject.SetActive(true);

            if (_originalLocalPosition == Vector2.zero)
            {
                _originalLocalPosition = transform.localPosition;
            }

            transform.localPosition = _originalLocalPosition;

            StopJolt();
            if (_flash != null) _flash.Kill();
            _text.color = TextColor;
            _text.transform.localScale = Vector3.one;

            if (_canvas == null)
            {
                _canvas = GetComponent<Canvas>();
                _canvas.worldCamera = Camera.main;
            }

            if (!HintCache.Instance.Contains(this)) HintCache.Instance.Add(this);

            DoAppearAnimation(_appearDuration);
        }

        public void Initialize(Transform follow, string word)
        {
            _followTarget = follow;
            _followOffset = AboveTarget;
            HideTimer();
            _text.SetText(word);

            Word = word;
            FitBox();
        }

        public void SetIcon(IconType icon)
        {
            switch (icon)
            {
                case IconType.Attack:
                    _icon.sprite = _attackIcon;
                    break;
                case IconType.Chest:
                    _icon.sprite = _chestIcon;
                    break;
                case IconType.DodgeLeft:
                    _icon.sprite = _dodgeLeftIcon;
                    break;
                case IconType.DodgeRight:
                    _icon.sprite = _dodgeRightIcon;
                    break;
            }
        }

        /// <summary>
        /// Shows the word at an offset from its target, there at once rather than flying in, and following from
        /// then on. For the boss fight, where a prompt must appear right where it is needed.
        /// </summary>
        public void Initialize(Transform follow, string word, Vector2 offset)
        {
            Initialize(follow, word);
            _followOffset = offset;
            KillCurrentTweens();
            transform.position = (Vector2)follow.position + offset;
            transform.localScale = Vector3.zero;
            _currentTweens.Add(transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack));
            _follow = true;
        }

        /// <summary>
        /// Drains the bar under the plaque over the given time, gold, then red as it runs out.
        /// </summary>
        public void ShowTimer(float seconds)
        {
            if (_timer == null) return;
            _timer.SetActive(true);
            var fill = _timerFill.GetComponent<Image>();
            _timerFill.localScale = Vector3.one;
            if (_timerTween != null) _timerTween.Kill();
            _timerTween = _timerFill.DOScaleX(0f, seconds).SetEase(Ease.Linear)
                .OnUpdate(() => fill.color = _timerFill.localScale.x > 0.35f ? CompleteColor : RejectColor);
        }

        private void HideTimer()
        {
            if (_timerTween != null) _timerTween.Kill();
            if (_timer != null) _timer.SetActive(false);
        }

        /// <summary>
        /// A letter of this word was typed: the word hops.
        /// </summary>
        public void Bump()
        {
            StopJolt();
            _jolt = _text.transform.DOPunchPosition(new Vector3(0f, 1.5f * ArtPixel(), 0f), 0.15f, 1, 0f);
        }

        /// <summary>
        /// This word was being typed and a wrong letter broke it: it flashes red and shakes its head.
        /// </summary>
        public void Reject()
        {
            StopJolt();
            _jolt = _text.transform.DOShakePosition(0.3f, new Vector3(2f * ArtPixel(), 0f, 0f), 30, 0f);

            // The colour belongs to the word-complete flash while that plays.
            foreach (var sequence in _currentSequences)
                if (sequence.IsActive() && sequence.IsPlaying()) return;

            if (_flash != null) _flash.Kill();
            _flash = DOTween.Sequence()
                .Append(_text.DOColor(RejectColor, 0.05f))
                .AppendInterval(0.15f)
                .Append(_text.DOColor(TextColor, 0.2f));
        }

        public void OnWordTyped(string letters)
        {
            WordWasInvalid = false;
            WordComplete = false;

            // Match against Word, not _text.GetParsedText(): TMP only re-parses at the next canvas rebuild,
            // so after Initialize() the parsed text is the previous word until the end of the frame, and
            // writing it back below would leave the prompt showing a word that no longer maps to anything.
            var word = Word;
            if (word == null)
            {
                WordWasInvalid = true;
                return;
            }

            var newText = word;

            if (string.Equals(letters, word))
            {
                if (_flash != null) _flash.Kill();
                DoCompleteWordAnimation(_wordCompleteAnimationDuration);
                WordComplete = true;
                newText = string.Format("<color={0}>{1}</color>", TypedColor, word);
            }
            else if (word.StartsWith(letters))
            {
                var firstPart = word.Substring(0, letters.Length);
                newText = string.Format("<color={0}>{1}</color>{2}", TypedColor, firstPart, word.Substring(letters.Length));
            }
            else
            {
                WordWasInvalid = true;
            }

            _text.SetText(newText);
        }

        private void DoAppearAnimation(float duration)
        {
            KillCurrentTweens();

            var scaleTween = transform.DOScale(Vector3.one, duration * 1.0f).SetEase(Ease.OutBack);
            _currentTweens.Add(scaleTween);

            if (_followTarget == null)
            {
               
                var moveTween = DOTween.To(
                    () => transform.localPosition,
                    x => transform.localPosition = x,
                    new Vector3(0, duration * 0.333f),
                    duration).SetRelative(true).OnStart(() => transform.localPosition = _originalLocalPosition).OnComplete(() => _follow = true);

                _currentTweens.Add(moveTween);
            }
            else
            {
                var moveTween = transform.DOMove(new Vector3(0, duration * 0.333f), duration).SetRelative(true)
                    .OnComplete(() => _follow = true);

                _currentTweens.Add(moveTween);
            }
        }

        private void DoDisappearAnimation(float duration, Action onCompleted)
        {
            KillCurrentTweens();

            var scaleTween = transform.DOScale(Vector3.zero, duration * 1.0f).SetEase(Ease.OutBack)
                .SetDelay(_wordCompleteAnimationDuration).OnComplete(() => onCompleted());
            _currentTweens.Add(scaleTween);

            if (_followTarget == null)
            {
                var moveTween = DOTween
                    .To(
                        () => transform.localPosition,
                        x => transform.localPosition = x,
                        -new Vector3(0, duration * 0.333f),
                        duration).SetDelay(_wordCompleteAnimationDuration).SetRelative();

                _currentTweens.Add(moveTween);
            }
            else
            {
                var moveTween = transform.DOMove(new Vector3(0, duration * 0.333f), duration)
                    .SetDelay(_wordCompleteAnimationDuration).SetRelative(true);
                _currentTweens.Add(moveTween);
            }
        }

        private void DoCompleteWordAnimation(float duration)
        {
            if (_currentSequences.Count > 0)
            {
                foreach (var sequence in _currentSequences)
                {
                    sequence.Kill();
                    sequence.Rewind(false);
                }

                _currentSequences.Clear();

                _text.transform.localScale = Vector3.one;
                _text.color = TextColor;
            }

            var scaleSequence = DOTween.Sequence();
            scaleSequence.Append(_text.transform.DOScale(0.25f, duration).SetRelative().SetEase(Ease.OutBack));
            scaleSequence.Append(_text.transform.DOScale(-0.25f, duration).SetRelative().SetEase(Ease.OutBack));

            _currentSequences.Add(scaleSequence);

            var colorSequence = DOTween.Sequence();
            colorSequence.Append(_text.DOColor(CompleteColor, duration).SetEase(Ease.OutBack));
            colorSequence.Append(_text.DOColor(TextColor, duration).SetEase(Ease.OutBack));

            _currentSequences.Add(colorSequence);
        }

        // Sizes the plaque to hug its word, on whole art pixels, with the icon (if any) inside it on the left.
        private void FitBox()
        {
            var box = (RectTransform)_text.transform.parent;

            // The preferred width ends with the spacing after the last letter, one pixel.
            var textWidth = Mathf.Round(_text.GetPreferredValues(Word).x / Px) - 1;
            var left = Frame + Margin + (_icon != null ? IconWidth + IconGap : 0);
            box.sizeDelta = new Vector2((left + textWidth + Margin + Frame) * Px, box.sizeDelta.y);

            // The word keeps its distance from the plaque's left edge, so its place under the plaque's centre moves
            // with the width: keep it as the place its hops and shakes return to.
            StopJolt();
            var text = _text.rectTransform;
            text.anchoredPosition = new Vector2(left * Px, text.anchoredPosition.y);
            _wordPosition = text.localPosition;
        }

        // One pixel of the game's art (1/16 of a world unit), in the word's own units. The hints are world-space
        // canvases, and the move hints sit under a scaled parent.
        private float ArtPixel()
        {
            var parentScale = transform.parent != null ? transform.parent.lossyScale.y : 1f;
            return 1f / 16f / parentScale;
        }

        private void StopJolt()
        {
            if (_jolt == null) return;
            _jolt.Kill();
            _jolt = null;
            _text.transform.localPosition = _wordPosition;
        }

        private void KillCurrentTweens()
        {
            foreach (var tween in _currentTweens)
                if (tween.IsPlaying())
                {
                    tween.Kill(false);
                    tween.Rewind(false);
                }

            _currentTweens.Clear();
        }

        private void LateUpdate()
        {
            if (!_follow) return;
            if (_followTarget == null) return;

            var newPos = Vector3.Lerp(
                transform.position,
                new Vector2(_followTarget.position.x, _followTarget.position.y) + _followOffset,
                _movementSpeed * Time.deltaTime);

            transform.position = newPos;
        }

        private void Awake()
        {
            _wordPosition = _text.transform.localPosition;
        }

        private void Start()
        {
            if (!HintCache.Instance.Contains(this)) HintCache.Instance.Add(this);
        }
    }
}