// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="MoveHints.cs" author="Lars" company="None">
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

    using UnityEngine;

    using Utilities.Game.ECSCache;
    using Utilities.Game.Navigation;

    public class MoveHints : MonoBehaviour
    {
        [SerializeField]
        private SpellHint _downHint;

        private Transform _followTarget;

        [SerializeField]
        private SpellHint _leftHint;

        [SerializeField]
        private float _movementSpeed;

        [SerializeField]
        private SpellHint _rightHint;

        [SerializeField]
        private SpellHint _upHint;

        public void SetHint(Direction direction, bool enabled, string word = null)
        {
            SpellHint hint;
            switch (direction)
            {
                case Direction.Up:
                    hint = _upHint;
                    break;
                case Direction.Down:
                    hint = _downHint;
                    break;
                case Direction.Left:
                    hint = _leftHint;
                    break;
                case Direction.Right:
                    hint = _rightHint;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("direction");
            }

            if (enabled)
            {
                hint.Enable();
                hint.Initialize(null, word);
            }
            else if (!enabled)
            {
                hint.Disable();
            }
        }

        public void SetAllHints(bool enabled)
        {
            for (var i = 0; i < 4; i++)
            {
                var direction = (Direction)i;
                SetHint(direction, enabled);
            }
        }

        private void LateUpdate()
        {
            if (_followTarget == null)
            {
                var player = ActorCache.Instance.Player;
                if (player == null) return;

                _followTarget = player.Entity.GameObject.transform;
                transform.position = new Vector2(_followTarget.position.x, _followTarget.position.y);
            }

            var newPos = Vector2.Lerp(
                transform.position,
                new Vector2(_followTarget.position.x, _followTarget.position.y),
                _movementSpeed * Time.deltaTime);

            transform.position = newPos;
        }
    }
}