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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using ECS.Components;
    using ECS.Components.Type;
    using ECS.Entities;
    using ECS.Systems;

    using UI;
    using UI.Hint;

    using UnityEngine;

    using Utilities;
    using Utilities.Game.ECSCache;
    using Utilities.Game.Navigation;
    using Utilities.Game.ObjectPool;

    public class InputController : MonoBehaviour
    {
        private readonly StringBuilder _currentInputWord = new StringBuilder();

        private readonly Dictionary<string, Direction> _requiredDirectionWord = new Dictionary<string, Direction>();

        private readonly Dictionary<string, Entity> _requiredAttackWord = new Dictionary<string, Entity>();

        private readonly Dictionary<string, Chest> _requiredLootWord = new Dictionary<string, Chest>();

        private readonly Dictionary<Chest, SpellHint> _chestSpellHints = new Dictionary<Chest, SpellHint>();

        private readonly Dictionary<Entity, SpellHint> _entitySpellHints = new Dictionary<Entity, SpellHint>();

        private bool _shouldRemoveMovementWords = false;

        [SerializeField]
        private WordSystem _wordSystem;

        [SerializeField]
        private AttackSystem _attackSystem;

        [SerializeField]
        private ItemSystem _itemSystem;

        [SerializeField]
        private MoveHints _moveHints;

        private int _attackDifficulty = WordSystem.MinWordLength + GameController.CurrentLevel;

        private int _movementDifficulty = WordSystem.MinWordLength;

        private int _lootDifficulty = WordSystem.MaxWordLength;

        private bool _movingSimple;

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

        private void SubmitInput()
        {
            var inputWord = _currentInputWord.ToString();
            _currentInputWord.Length = 0;

            if (_requiredDirectionWord.ContainsKey(inputWord))
            {
                GameController.Instance.PlayerHasMoved = true;
                MovePlayer(_requiredDirectionWord[inputWord]);
            }
            else if (_requiredAttackWord.ContainsKey(inputWord))
            {
                var player = ActorCache.Instance.Player;
                if (player == null) return;

                var target = _requiredAttackWord[inputWord];

                var playerPos = player.Entity.GetComponent<GridPositionComponent>();
                var enemyPos = target.GetComponent<GridPositionComponent>();

                var newAttackWord = GetWordNonConflicting(_attackDifficulty);
                var spellHint = _entitySpellHints[enemyPos.Owner];
                spellHint.Initialize(enemyPos.Owner.GameObject.transform, newAttackWord);
                spellHint.SetIcon(SpellHint.IconType.Attack);

                _requiredAttackWord.Remove(inputWord);
                _requiredAttackWord.Add(newAttackWord, target);

                GameController.Instance.PlayerHasMoved = true;

                if (Vector2.Distance(playerPos.Position, enemyPos.Position) <= 1)
                {
                    _attackSystem.AttackMelee(player.Entity, target);
                }
                else
                {
                    _attackSystem.AttackRanged(player.Entity, target);
                }
            }
            else if (_requiredLootWord.ContainsKey(inputWord))
            {
                GameController.Instance.PlayerHasMoved = true;

                var chest = _requiredLootWord[inputWord];
                _itemSystem.ChestPickup(chest);
                chest.Open();

                _requiredLootWord.Remove(inputWord);
                var spellHint = _chestSpellHints[chest];
                _chestSpellHints.Remove(chest);

                spellHint.Disable();
            }
        }

        // Update is called once per frame
        private void Update()
        {
            if (!GameController.Instance.IsPlaying) return;

            UpdateMovementType();

            if (_shouldRemoveMovementWords)
            {
                _shouldRemoveMovementWords = false;
                _requiredDirectionWord.Clear();
            }

            if (_requiredDirectionWord.Count == 0)
            {
                UpdateInputHints();
            }

            UpdateInputHintVisibility();

            foreach (var c in Input.inputString.ToLower())
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
            /*
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.W))
            {
                MovePlayer(Direction.Up);
            }
            else if (Input.GetKeyDown(KeyCode.A))
            {
                MovePlayer(Direction.Left);
            }
            else if (Input.GetKeyDown(KeyCode.S))
            {
                MovePlayer(Direction.Down);
            }
            else if (Input.GetKeyDown(KeyCode.D))
            {
                MovePlayer(Direction.Right);
            }
#endif
*/
        }

        private void UpdateInputHints()
        {
            for (var i = 0; i < 4; i++)
            {
                var direction = (Direction)i;
                var player = ActorCache.Instance.Player;
                var nextPos = player.Entity.GetComponent<GridPositionComponent>().Position;
                var directionVector = direction.ToVector2Int();

                if (MapSystem.Instance.IsWalkable(nextPos + directionVector))
                {
                    CreateMovementHint(direction);
                }
                else
                {
                    _moveHints.SetHint(direction, false);
                }
            }
        }

        private void UpdateInputHintVisibility()
        {
            for (var i = 0; i < 4; i++)
            {
                var direction = (Direction)i;
                var player = ActorCache.Instance.Player;
                var nextPos = player.Entity.GetComponent<GridPositionComponent>().Position;
                var directionVector = direction.ToVector2Int();

                if (!MapSystem.Instance.IsWalkable(nextPos + directionVector) && _requiredDirectionWord.ContainsValue(direction))
                {
                    RemoveMovementHint(direction);
                }

                if (_requiredDirectionWord.ContainsValue(direction)
                    && !_requiredDirectionWord.ContainsKey(_moveHints.GetHint(direction).Word))
                {
                    _shouldRemoveMovementWords = true;
                }
            }
        }

        private void UpdateMovementType()
        {
            var renderers = ComponentCache.Instance.GetCached<RenderComponent>();

            if (renderers == null)
            {
                _movingSimple = true;
                return;
            }

            var shouldMoveSimple = true;
            foreach (var render in renderers)
            {
                if (ActorCache.Instance.Player.Entity == render.Owner) continue;

                if (render.Renderer.enabled && render.Owner.GetComponent<BooleanComponent>(ComponentType.Turn) != null)
                {
                    shouldMoveSimple = false;
                    break;
                }
            }

            if (shouldMoveSimple != _movingSimple)
            {
                _movingSimple = shouldMoveSimple;
                _shouldRemoveMovementWords = true;
            }
        }

        private void MovePlayer(Direction direction)
        {
            var directionVector = direction.ToVector2Int();
            var player = ActorCache.Instance.Player;
            player.Entity.GetComponent<GridPositionComponent>().Position += directionVector;

            _shouldRemoveMovementWords = true;
        }

        private void Start()
        {
            _attackSystem.OnEntityLeaveAttackRange += OnEntityLeaveAttackRange;
            _attackSystem.OnEntityEnterAttackRange += OnEntityEnterAttackRange;

            _itemSystem.OnPlayerEnterLootArea += OnPlayerEnterLootArea;
            _itemSystem.OnPlayerLeaveLootArea += OnPlayerLeaveLootArea;
        }

        private void OnPlayerLeaveLootArea(Chest chest)
        {
            if (!_chestSpellHints.ContainsKey(chest)) return;

            var spellHint = _chestSpellHints[chest];
            _chestSpellHints.Remove(chest);

            var word = spellHint.Word;

            if (word != null && _requiredLootWord.ContainsKey(word))
            {
                _requiredLootWord.Remove(word);
            }

            spellHint.Disable();
        }

        private void OnPlayerEnterLootArea(Chest chest)
        {
            var requiredLootWord = GetWordNonConflicting(_lootDifficulty);
            _requiredLootWord.Add(requiredLootWord, chest);

            var spellHint = ObjectPools.Instance.GetPooledObject<SpellHint>();
            spellHint.transform.position = (Vector2)chest.gameObject.transform.position + Vector2.up;
            spellHint.Initialize(chest.gameObject.transform, requiredLootWord);
            spellHint.SetIcon(SpellHint.IconType.Chest);

            _chestSpellHints.Add(chest, spellHint);
        }

        private void OnEntityEnterAttackRange(Entity entity, SpellHint spellHint)
        {
            var player = ActorCache.Instance.Player;
            if (player == null) return;

            var playerPos = player.Entity.GetComponent<GridPositionComponent>();
            if (playerPos == null) return;

            var enemyPos = entity.GetComponent<GridPositionComponent>();
            if (enemyPos == null) return;

            var reach = player.Entity.GetComponent<IntegerComponent>(ComponentType.Reach);
            if (reach == null) return;

            var distance = Vector2.Distance(playerPos.Position, enemyPos.Position);
            if (distance > reach.Value) return;

            var requiredWord = GetWordNonConflicting(_attackDifficulty);
            _requiredAttackWord.Add(requiredWord, entity);

            spellHint.transform.position = enemyPos.Position + Vector2.up;
            spellHint.Initialize(entity.GameObject.transform, requiredWord);
            spellHint.SetIcon(SpellHint.IconType.Attack);

            _entitySpellHints.Add(entity, spellHint);
        }

        private void OnEntityLeaveAttackRange(Entity entity, SpellHint spellHint)
        {
            if (_entitySpellHints.ContainsKey(entity))
            {
                if (spellHint.Word != null && _requiredAttackWord.ContainsKey(spellHint.Word))
                {
                    _requiredAttackWord.Remove(spellHint.Word);
                }

                spellHint.Disable();
                _entitySpellHints.Remove(entity);
            }
        }

        private void CreateMovementHint(Direction direction)
        {
            var requiredWord =
                _movingSimple ? GetLetterNonConflicting() : GetWordNonConflicting(_movementDifficulty);
            if (_requiredDirectionWord.ContainsKey(requiredWord)) return;

            _requiredDirectionWord.Add(requiredWord, direction);
            _moveHints.SetHint(direction, true, requiredWord);
        }

        private void RemoveMovementHint(Direction direction)
        {
            var moveWord = _moveHints.GetHint(direction).Word;
            _requiredDirectionWord.Remove(moveWord);
            _moveHints.SetHint(direction, false);
        }

        private string GetWordNonConflicting(int length)
        {
            string word;
            var maxIterations = 20;
            var currentIterations = 0;

            do
            {
                word = _wordSystem.GetNextWord(length);
                currentIterations++;
            }
            while ((_requiredDirectionWord.ContainsKey(word) || _requiredAttackWord.ContainsKey(word)
                   || _requiredLootWord.ContainsKey(word)) && currentIterations < maxIterations);

            return word;
        }

        private string GetLetterNonConflicting()
        {
            string letter;
            var maxIterations = 20;
            var currentIterations = 0;

            do
            {
                letter = _wordSystem.GetNextLetter();
                currentIterations++;
            }
            while (currentIterations < maxIterations && (_requiredAttackWord.Any(x => x.Key.StartsWith(letter))
                                                         || _requiredLootWord.Any(x => x.Key.StartsWith(letter))));

            return letter;
        }
    }
}