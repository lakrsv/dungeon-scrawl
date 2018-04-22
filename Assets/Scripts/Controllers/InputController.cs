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

        [SerializeField]
        private WordSystem _wordSystem;

        [SerializeField]
        private AttackSystem _attackSystem;

        [SerializeField]
        private ItemSystem _itemSystem;

        [SerializeField]
        private MoveHints _moveHints;

        private int _attackDifficulty = WordSystem.MinWordLength + 1;

        private int _movementDifficulty = WordSystem.MinWordLength;

        private int _lootDifficulty = WordSystem.MaxWordLength;

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
            else if (_requiredAttackWord.ContainsKey(inputWord))
            {
                var player = ActorCache.Instance.Player;
                if (player == null) return;

                var target = _requiredAttackWord[inputWord];

                var playerPos = player.Entity.GetComponent<GridPositionComponent>();
                var enemyPos = target.GetComponent<GridPositionComponent>();

                var newAttackWord = _wordSystem.GetNextWord(_attackDifficulty);
                var spellHint = _entitySpellHints[enemyPos.Owner];
                spellHint.Initialize(enemyPos.Owner.GameObject.transform, newAttackWord);

                _requiredAttackWord.Remove(inputWord);
                _requiredAttackWord.Add(newAttackWord, target);

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
            if (_requiredDirectionWord.Count == 0)
            {
                SetMovementWords();
            }

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
        }

        private void UpdateInputHintVisibility()
        {
            for (var i = 0; i < 4; i++)
            {
                var direction = (Direction)i;
                var player = ActorCache.Instance.Player;
                var nextPos = player.Entity.GetComponent<GridPositionComponent>().Position;
                var directionVector = direction.ToVector2Int();

                if (_requiredDirectionWord.ContainsValue(direction))
                {
                    if (!MapSystem.Instance.IsWalkable(nextPos + directionVector))
                    {
                        _moveHints.SetHint(direction, false);
                        _requiredDirectionWord.Remove(_moveHints.GetHint(direction).Word);
                    }
                }
                else
                {
                    if (MapSystem.Instance.IsWalkable(nextPos + directionVector))
                    {
                        var requiredWord = _wordSystem.GetNextWord(_movementDifficulty);
                        _requiredDirectionWord.Add(requiredWord, direction);
                        _moveHints.SetHint(direction, true, requiredWord);
                    }
                }
            }
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
            }
        }

        private void MovePlayer(Direction direction)
        {
            var directionVector = direction.ToVector2Int();
            var player = ActorCache.Instance.Player;
            player.Entity.GetComponent<GridPositionComponent>().Position += directionVector;

            _requiredDirectionWord.Clear();
        }

        private void Start()
        {
            _attackSystem.OnEntityLeaveAttackRange += OnEntityLeaveAttackRange;
            _attackSystem.OnEntityEnterAttackRange += OnEntityEnterAttackRange;

            _itemSystem.OnPlayerEnterLootArea += OnPlayerEnterLootArea;
            _itemSystem.OnPlayerLeaveLootArea += OnPlayerLeaveLootArea;

            InvokeRepeating("UpdateInputHintVisibility", 0.25f, 0.25f);
        }

        private void OnPlayerLeaveLootArea(Chest chest)
        {
            if (!_chestSpellHints.ContainsKey(chest)) return;

            var spellHint = _chestSpellHints[chest];
            _chestSpellHints.Remove(chest);

            var word = spellHint.Word;
            if (_requiredLootWord.ContainsKey(word))
            {
                _requiredLootWord.Remove(word);
            }
        }

        private void OnPlayerEnterLootArea(Chest chest)
        {
            var requiredLootWord = _wordSystem.GetNextWord(_lootDifficulty);
            _requiredLootWord.Add(requiredLootWord, chest);

            var spellHint = ObjectPools.Instance.GetPooledObject<SpellHint>();
            spellHint.transform.position = (Vector2)chest.gameObject.transform.position + Vector2.up;
            spellHint.Initialize(chest.gameObject.transform, requiredLootWord);

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

            var requiredWord = _wordSystem.GetNextWord(_attackDifficulty);
            _requiredAttackWord.Add(requiredWord, entity);

            spellHint.transform.position = enemyPos.Position + Vector2.up;
            spellHint.Initialize(entity.GameObject.transform, requiredWord);

            _entitySpellHints.Add(entity, spellHint);
        }

        private void OnEntityLeaveAttackRange(Entity entity, SpellHint spellHint)
        {
            if (_entitySpellHints.ContainsKey(entity))
            {
                if (_requiredAttackWord.ContainsKey(spellHint.Word))
                {
                    _requiredAttackWord.Remove(spellHint.Word);
                }

                _entitySpellHints.Remove(entity);
            }
        }
    }
}