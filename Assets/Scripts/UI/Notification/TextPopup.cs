// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="TextPopup.cs" author="Lars-Kristian Svenoy" company="Cardlike">
// //  Copyright @ 2018 Cardlike. All rights reserved.
// // </copyright>
// // <summary>
// //   TODO - Insert file description
// // </summary>
// // --------------------------------------------------------------------------------------------------------------------

namespace UI.Notification
{
    using System.Collections;
    using System.Collections.Generic;

    using DG.Tweening;

    using TMPro;

    using UnityEngine;

    using Utilities.Game.ObjectPool;

    [RequireComponent(typeof(TextMeshPro))]
    public class TextPopup : MonoBehaviour, IPopupNotification, IPoolable
    {
        // The outline is the palette's ink.
        private static readonly Color Ink = new Color(0.078f, 0.047f, 0.11f);

        private static readonly Vector2[] OutlineOffsets = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

        private readonly List<TextMeshPro> _outline = new List<TextMeshPro>();

        [SerializeField]
        private TextMeshPro _text;

        private float _baseSize;

        public bool IsEnabled { get; private set; }

        public void Disable()
        {
            IsEnabled = false;
            _text.SetText(string.Empty);
            foreach (var copy in _outline) copy.SetText(string.Empty);
            gameObject.SetActive(false);
        }

        public void Enable(string message, Vector2 position, float duration)
        {
            Enable(message, position, duration, 1f);
        }

        /// <summary>
        /// Shows a message. Size 1 draws its letters a pixel to the art's pixel; announcements use 2.
        /// </summary>
        public void Enable(string message, Vector2 position, float duration, float size)
        {
            IsEnabled = true;
            SetText(message, size);
            transform.position = position;
            transform.localScale = Vector3.zero;

            DoAppearAnimation(duration);
        }

        public void Enable()
        {
            gameObject.SetActive(true);
        }

        // An ink outline one font pixel round the letters, so they read over the dungeon: four copies of the text,
        // drawn behind it. The pixel font has no outline of its own.
        private void Awake()
        {
            _baseSize = _text.fontSize;
            var renderer = _text.GetComponent<MeshRenderer>();
            foreach (var unused in OutlineOffsets)
            {
                var copy = new GameObject("Outline").AddComponent<TextMeshPro>();
                copy.transform.SetParent(_text.transform, false);
                copy.font = _text.font;
                copy.fontSharedMaterial = _text.fontSharedMaterial;
                copy.alignment = _text.alignment;
                copy.textWrappingMode = _text.textWrappingMode;
                copy.overflowMode = _text.overflowMode;
                copy.rectTransform.sizeDelta = _text.rectTransform.sizeDelta;
                copy.color = Ink;
                var copyRenderer = copy.GetComponent<MeshRenderer>();
                copyRenderer.sortingLayerID = renderer.sortingLayerID;
                copyRenderer.sortingOrder = renderer.sortingOrder - 1;
                _outline.Add(copy);
            }
        }

        private void SetText(string message, float size)
        {
            _text.fontSize = _baseSize * size;
            _text.SetText(message);

            // World-space text is a tenth of a unit per point: one font pixel is a sixteenth of the size.
            var pixel = _text.fontSize * 0.1f / 16f;
            for (var i = 0; i < _outline.Count; i++)
            {
                _outline[i].fontSize = _text.fontSize;
                _outline[i].SetText(message);
                _outline[i].transform.localPosition = OutlineOffsets[i] * pixel;
            }
        }

        private void DoAppearAnimation(float duration)
        {
            _text.transform.DOScale(Vector3.one, duration * 0.333f).SetEase(Ease.OutElastic);
            _text.transform.DOMove(transform.position + new Vector3(0, duration * 0.166f), duration);
            _text.transform.DOScale(Vector3.zero, 0.1f).SetEase(Ease.OutQuint).SetDelay(duration * 0.667f).OnComplete(Disable);
        }
    }
}