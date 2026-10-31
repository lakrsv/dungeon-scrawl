// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="CameraMovement.cs" author="Lars" company="None">
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

using UnityEngine;

namespace Utilities.Camera
{
    using Controllers;

    using DG.Tweening;

    using Utilities.Game.ECSCache;

    public class CameraMovement : MonoBehaviour
    {
        private const float MovementSpeed = 5.0f;

        private Transform _followTarget;

        private Vector3 _position;

        private Vector3 _shake;

        private void LateUpdate()
        {
            if (!GameController.Instance.IsPlaying) return;

            if (_followTarget == null)
            {
                var player = ActorCache.Instance.Player;
                if (player == null) return;

                _followTarget = player.Entity.GameObject.transform;
                _position = new Vector3(_followTarget.position.x, _followTarget.position.y, -10);
            }

            // Follow without the shake, then add it on top, so shaking never drags the camera off course.
            _position = Vector3.Lerp(
                _position,
                new Vector3(_followTarget.position.x, _followTarget.position.y, -10),
                MovementSpeed * Time.deltaTime);

            transform.position = _position + _shake;
        }

        public void SetPosition(Vector2 position)
        {
            _position = new Vector3(position.x, position.y, -10);
            transform.position = _position;
        }

        public void Shake(float strength, float duration)
        {
            DOTween.Kill(this);
            DOTween.Shake(() => _shake, x => _shake = x, duration, new Vector3(strength, strength, 0), 25)
                .SetTarget(this)
                .OnComplete(() => _shake = Vector3.zero);
        }
    }
}