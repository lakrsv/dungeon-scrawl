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

    using DG.Tweening;

    using TMPro;

    using UnityEngine;

    using Utilities.Game.ObjectPool;

    [RequireComponent(typeof(TextMeshPro))]
    public class TextPopup : MonoBehaviour, IPopupNotification, IPoolable
    {
        [SerializeField]
        private TextMeshPro _text;

        public bool IsEnabled { get; private set; }

        public void Disable()
        {
            IsEnabled = false;
            _text.SetText(string.Empty);
            gameObject.SetActive(false);
        }

        public void Enable(string message, Vector2 position, float duration)
        {
            IsEnabled = true;
            _text.SetText(message);
            transform.position = position;
            transform.localScale = Vector3.zero;

            DoAppearAnimation(duration);
        }

        public void Enable()
        {
            gameObject.SetActive(true);
        }

        private void DoAppearAnimation(float duration)
        {
            _text.transform.DOScale(Vector3.one, duration * 0.333f).SetEase(Ease.OutElastic);
            _text.transform.DOMove(transform.position + new Vector3(0, duration * 0.166f), duration);
            _text.transform.DOScale(Vector3.zero, 0.1f).SetEase(Ease.OutQuint).SetDelay(duration * 0.667f).OnComplete(Disable);
        }
    }
}