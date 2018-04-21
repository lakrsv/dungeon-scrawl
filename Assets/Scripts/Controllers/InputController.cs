// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="InputController.cs" author="Lars" company="None">
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

namespace Controllers
{
    using System.Collections.Generic;
    using System.Text;

    using ECS.Components;
    using ECS.Entities;
    using ECS.Systems;

    using UI;

    using UnityEngine;

    using Utilities;
    using Utilities.Game.ECSCache;
    using Utilities.Game.Navigation;

    public class InputController : MonoBehaviour
    {
        private readonly StringBuilder _currentInputWord = new StringBuilder();

        private readonly Dictionary<string, Direction> _requiredDirectionWord = new Dictionary<string, Direction>();

        [SerializeField]
        private WordSystem _wordSystem;

        [SerializeField]
        private MoveHints _moveHints;

        private int _attackDifficulty = WordSystem.MinWordLength;

        private int _movementDifficulty = WordSystem.MinWordLength;

        private void AppendLetter(char c)
        {
            _currentInputWord.Append(c);

            var invalid = true;
            HintCache.Instance.GetCached().ForEach(x =>
                {
                    x.OnWordTyped(_currentInputWord.ToString());
                    if (!x.WordWasInvalid) invalid = false;

                    if (x.WordComplete)
                    {
                        SubmitInput();
                    }
                });

            if (invalid) _currentInputWord.Length = 0;
        }

        private void RemoveLetter()
        {
            if (_currentInputWord.Length == 0) return;
            _currentInputWord.Length -= 1;
        }

        private void SubmitInput()
        {
            var inputWord = _currentInputWord.ToString();
            _currentInputWord.Length = 0;

            if (_requiredDirectionWord.ContainsKey(inputWord))
            {
                MovePlayer(_requiredDirectionWord[inputWord]);
            }
        }

        // Update is called once per frame
        private void Update()
        {
            if (_requiredDirectionWord.Count == 0) SetMovementWords();

            foreach (var c in Input.inputString)
                switch (c)
                {
                    case '\b':
                        RemoveLetter();
                        break;
                    case '\n':
                    case '\r':
                        SubmitInput();
                        break;
                    default:
                        AppendLetter(c);
                        break;
                }

            ////Debug.Log(_currentInputWord.ToString());
        }

        private void SetMovementWords()
        {
            for (var i = 0; i < 4; i++)
            {
                var direction = (Direction)i;

                var player = ActorCache.Instance.Player;
                var nextPos = player.Entity.GetComponent<GridPositionComponent>().Position;

                var directionVector = direction.ToVector2Int();
                if (!MapSystem.Instance.IsWalkable(nextPos + directionVector))
                {
                    _moveHints.SetHint(direction, false);
                    continue;
                }

                var requiredWord = _wordSystem.GetNextWord(_movementDifficulty);

                _requiredDirectionWord.Add(requiredWord, direction);

                _moveHints.SetHint(direction, true, requiredWord);

                Debug.Log(string.Format("To move {0} type {1}", direction, requiredWord));
            }
        }

        private void MovePlayer(Direction direction)
        {
            var directionVector = direction.ToVector2Int();
            var player = ActorCache.Instance.Player;
            player.Entity.GetComponent<GridPositionComponent>().Position += directionVector;

            _requiredDirectionWord.Clear();
        }
    }
}