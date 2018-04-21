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
    using Utilities.Game.ECSCache;

    public class CameraMovement : MonoBehaviour
    {
        private const float MovementSpeed = 5.0f;

        private Transform _followTarget;

        private void LateUpdate()
        {
            if (_followTarget == null)
            {
                var player = ActorCache.Instance.Player;
                if (player == null) return;

                _followTarget = player.Entity.GameObject.transform;
                transform.position = new Vector3(_followTarget.position.x, _followTarget.position.y, -10);
            }

            var newPos = Vector3.Lerp(
                transform.position,
                new Vector3(_followTarget.position.x, _followTarget.position.y, -10),
                MovementSpeed * Time.deltaTime);

            transform.position = newPos;
        }
    }
}