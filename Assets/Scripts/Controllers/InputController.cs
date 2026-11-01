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

        // Words that other systems ask for, each with what typing it does: the boss's dodges and attacks.
        private readonly Dictionary<string, Action> _requiredCustomWord = new Dictionary<string, Action>();

        private bool _movementLocked;

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

        // With nothing hostile in sight the move prompts are single keys (WASD or letters, see MoveKeys). Those keys
        // are steering, not typing.
        private bool QuickMovesUp
        {
            get { return _requiredDirectionWord.Keys.Any(x => x.Length == 1); }
        }

        // Whether this letter comes from a key being held down (the OS repeats it) rather than a fresh press.
        private static bool IsHeldRepeat(char c)
        {
            return c >= 'a' && c <= 'z' && !Input.GetKeyDown((KeyCode)c);
        }

        /// <summary>
        /// Gets or sets whether the move prompts are off, as in the boss fight, where the knight only moves by
        /// dodging.
        /// </summary>
        public bool MovementLocked
        {
            get
            {
                return _movementLocked;
            }

            set
            {
                _movementLocked = value;
                _requiredDirectionWord.Clear();
                if (value) _moveHints.SetAllHints(false);
            }
        }

        /// <summary>
        /// Asks for a word of the given length; typing it runs onTyped (once: ask again for another). The word never
        /// shares its first letter with any other word on screen, so none can be the start of another.
        /// </summary>
        public string AddWord(int length, Action onTyped)
        {
            string word;
            var tries = 0;
            do
            {
                word = _wordSystem.GetNextWord(length);
                tries++;
            }
            while (tries < 300 && (MoveKeys.Blocks(word) || ActiveWords().Any(x => x[0] == word[0])));

            _requiredCustomWord[word] = onTyped;
            return word;
        }

        public void RemoveWord(string word)
        {
            if (word != null) _requiredCustomWord.Remove(word);
        }

        private IEnumerable<string> ActiveWords()
        {
            return _requiredDirectionWord.Keys.Concat(_requiredAttackWord.Keys).Concat(_requiredLootWord.Keys).Concat(_requiredCustomWord.Keys);
        }

        private void AppendLetter(char c)
        {
            var before = _currentInputWord.ToString();
            _currentInputWord.Append(c);

            // Iterate a copy: SubmitInput can disable a hint, which removes it from the cache mid-loop.
            var invalid = true;
            var completed = false;
            HintCache.Instance.GetCached().ToList().ForEach(x =>
                {
                    x.OnWordTyped(_currentInputWord.ToString());
                    if (!x.WordWasInvalid) invalid = false;

                    if (x.WordComplete)
                    {
                        completed = true;
                        SubmitInput();
                    }
                });

            if (invalid) _currentInputWord.Length = 0;

            // Switching targets: a letter that breaks the word under way but starts another one begins that word,
            // instead of being thrown away. Never a single-key move, though: mid-word, that key is far more likely a
            // slip, and walking off by accident is worse than a buzz. Only longer words can start here, so nothing
            // completes, and the hints can be told directly.
            var switched = false;
            var letter = c.ToString();
            if (invalid && before.Length > 0 && !_requiredDirectionWord.ContainsKey(letter)
                && HintCache.Instance.GetCached().Any(x => x.Word != null && x.Word.Length > 1 && x.Word.StartsWith(letter)))
            {
                _currentInputWord.Append(c);
                foreach (var hint in HintCache.Instance.GetCached()) hint.OnWordTyped(letter);
                invalid = false;
                switched = true;
            }

            // A one-letter move, or a stray key while no word is under way, counts towards neither the typing speed
            // nor the accuracy.
            var steering = before.Length == 0 && QuickMovesUp;

            // Every key answers: a finished word chimes, a letter that fits blips a step higher than the last and
            // bumps the words it fits, and a wrong one buzzes and shakes off the words it broke.
            if (completed)
            {
                if (before.Length > 0) RunStats.Letter(true);
                RunStats.Word(before.Length + 1);
                AudioPlayer.Instance.PlayAudio(AudioPlayer.Type.Word);
            }
            else if (invalid)
            {
                if (!steering) RunStats.Mistake();
                AudioPlayer.Instance.PlayAudio(AudioPlayer.Type.Typo);
                if (before.Length > 0) ForEachHintStartingWith(before, x => x.Reject());
            }
            else
            {
                RunStats.Letter(before.Length > 0 && !switched);
                AudioPlayer.Instance.PlayLetter(_currentInputWord.Length - 1);
                ForEachHintStartingWith(_currentInputWord.ToString(), x => x.Bump());
            }
        }

        private static void ForEachHintStartingWith(string letters, Action<SpellHint> action)
        {
            foreach (var hint in HintCache.Instance.GetCached())
                if (hint.Word != null && hint.Word.StartsWith(letters)) action(hint);
        }

        private void RemoveLetter()
        {
            if (_currentInputWord.Length == 0) return;
            _currentInputWord.Length -= 1;
            AudioPlayer.Instance.PlayLetter(_currentInputWord.Length, 0.5f);

            var invalid = true;
            HintCache.Instance.GetCached().ToList().ForEach(x =>
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

            Action custom;
            if (_requiredCustomWord.TryGetValue(inputWord, out custom))
            {
                _requiredCustomWord.Remove(inputWord);
                GameController.Instance.PlayerHasMoved = true;
                custom();
                return;
            }

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
                RunStats.Chest();
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
            if (!GameController.Instance.IsPlaying || GameController.Instance.IsPaused) return;

            if (!_movementLocked)
            {
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
            }

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
                        // Quick moves go by presses: while their prompts are up and no word is under way, the
                        // repeats of a held key are ignored, so holding W doesn't walk you down a corridor.
                        if (_currentInputWord.Length == 0 && QuickMovesUp && IsHeldRepeat(c)) break;
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
            string requiredWord;
            if (!_movingSimple) requiredWord = GetWordNonConflicting(_movementDifficulty, false);
            else requiredWord = MoveKeys.Wasd ? MoveKeys.For(direction) : GetLetterNonConflicting();
            if (_requiredDirectionWord.ContainsKey(requiredWord))
            {
                // Hide the prompt rather than leave the previous word up: it would no longer map to anything.
                _moveHints.SetHint(direction, false);
                return;
            }

            _requiredDirectionWord.Add(requiredWord, direction);
            _moveHints.SetHint(direction, true, requiredWord);
        }

        private void RemoveMovementHint(Direction direction)
        {
            var moveWord = _moveHints.GetHint(direction).Word;
            _requiredDirectionWord.Remove(moveWord);
            _moveHints.SetHint(direction, false);
        }

        // Attack and loot words also keep clear of the WASD keys when those are on (see MoveKeys.Blocks). Move words
        // needn't: they are only words while the WASD keys are not in use.
        private string GetWordNonConflicting(int length, bool clearOfMoveKeys = true)
        {
            string word;
            var maxIterations = 30;
            var currentIterations = 0;

            do
            {
                word = _wordSystem.GetNextWord(length);
                currentIterations++;
            }
            while ((_requiredDirectionWord.ContainsKey(word) || _requiredAttackWord.ContainsKey(word)
                   || _requiredLootWord.ContainsKey(word) || _requiredCustomWord.ContainsKey(word)
                   || (clearOfMoveKeys && MoveKeys.Blocks(word)))
                   && currentIterations < maxIterations);

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
            while (currentIterations < maxIterations && (_requiredDirectionWord.ContainsKey(letter)
                                                         || _requiredAttackWord.Any(x => x.Key.StartsWith(letter))
                                                         || _requiredLootWord.Any(x => x.Key.StartsWith(letter))));

            return letter;
        }
    }
}