// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="Scrawl.cs" author="Lars" company="None">
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
// //   The fight with the Scrawl, the level 5 boss.
// // </summary>
// // --------------------------------------------------------------------------------------------------------------------

namespace Boss
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    using Controllers;

    using DG.Tweening;

    using ECS.Components;
    using ECS.Components.Type;
    using ECS.Systems;

    using Sound;

    using UI.Hint;
    using UI.Notification;

    using UnityEngine;

    using Utilities.Camera;
    using Utilities.Game;
    using Utilities.Game.ECSCache;
    using Utilities.Game.ObjectPool;

    /// <summary>
    /// The fight with the Scrawl (see BOSS.md). It runs dodge, punish, dodge:
    /// <list type="bullet">
    /// <item>A hand rises over the knight's lane and the lane on its own side, burning, while two warding words offer
    /// a step left and right. Only the step away from the hand is safe (on an edge lane only one step exists).</item>
    /// <item>After it slams it grips the edge of the bridge for a moment, and only then can it be hurt.</item>
    /// <item>The head can be hurt at any time and has much more health; killing it ends the fight. Now and then it
    /// speaks a beam down the knight's lane, which either step dodges.</item>
    /// <item>The Scrawl takes turns: left hand, right hand, beam. A broken hand sinks into the void and its turn becomes a
    /// breather; with both gone the head drifts closer and speaks every turn. Cracked, below half health, it gets
    /// faster.</item>
    /// </list>
    /// The knight only moves by dodging while it lasts.
    /// </summary>
    public class Scrawl : MonoBehaviour
    {
        // How many strikes each part takes, before difficulty. Health is counted in strikes of the knight's own damage,
        // so power still shortens the fight, but only so far: a knight with no power needs half as many again.
        private const float HandStrikes = 3f;

        private const float HeadStrikes = 12f;

        // Dodges are short, so they can be typed under pressure.
        private const int DodgeLength = 3;

        private const int HandWordLength = 6;

        private const int HeadWordLength = 8;

        // How much faster it gets once the head is cracked.
        private const float Desperate = 0.85f;

        // How dark the warning over the lanes gets.
        private const float ShadowAlpha = 0.6f;

        // Tuned per difficulty: Normal, Hard, Impossible.
        private static readonly float[] SlamWarning = { 2.5f, 1.8f, 1.3f };

        private static readonly float[] RestTime = { 3.5f, 3f, 2.6f };

        private static readonly float[] PauseTime = { 1.2f, 0.9f, 0.6f };

        private static readonly int[] SlamDamage = { 3, 3, 4 };

        private static readonly int[] BeamDamage = { 2, 2, 3 };

        private static readonly float[] HealthScale = { 1f, 1.25f, 1.5f };

        private static readonly Color ShadowColour = new Color(0.47f, 0.08f, 0.16f, 0f);

        private readonly List<Prompt> _dodges = new List<Prompt>();

        private readonly Dictionary<ScrawlHand, Prompt> _handPrompts = new Dictionary<ScrawlHand, Prompt>();

        private readonly HashSet<ScrawlHand> _resting = new HashSet<ScrawlHand>();

        [SerializeField]
        private ScrawlHand _left;

        [SerializeField]
        private ScrawlHand _right;

        [SerializeField]
        private ScrawlHead _head;

        // A red tint over the lanes about to be hit.
        [SerializeField]
        private SpriteRenderer _shadow;

        [SerializeField]
        private AudioClip _theme;

        private InputController _input;

        private AttackSystem _attack;

        private ItemSystem _items;

        private CameraMovement _camera;

        private BossBar _bar;

        private Coroutine _fight;

        private Prompt _headPrompt;

        private int _difficulty;

        private int _row;

        private float _faster = 1f;

        private static float Centre
        {
            get { return MapSystem.BridgeX + 1f; }
        }

        private static GridPositionComponent Knight
        {
            get { return ActorCache.Instance.Player.Entity.GetComponent<GridPositionComponent>(); }
        }

        private static Transform KnightTransform
        {
            get { return ActorCache.Instance.Player.Entity.GameObject.transform; }
        }

        private static int KnightLane
        {
            get { return Knight.Position.x - MapSystem.BridgeX; }
        }

        private static int PlayerDamage
        {
            get { return ActorCache.Instance.Player.Entity.GetComponent<IntegerComponent>(ComponentType.Damage).Value; }
        }

        private static bool Playing
        {
            get { return GameController.Instance.IsPlaying; }
        }

        public void Begin(InputController input, AttackSystem attack, ItemSystem items, CameraMovement camera, BossBar bar)
        {
            _input = input;
            _attack = attack;
            _items = items;
            _camera = camera;
            _bar = bar;
            _difficulty = Mathf.Clamp(GameController.Instance.GetDifficulty(), 1, 3) - 1;
            _row = Knight.Position.y;
            _fight = StartCoroutine(Fight());
        }

        private IEnumerator Fight()
        {
            yield return Intro();
            for (var turn = 0; _head.Alive && Playing; turn = (turn + 1) % 3)
            {
                yield return new WaitForSeconds(PauseTime[_difficulty] * _faster);
                if (!_head.Alive || !Playing) yield break;

                // Left hand, right hand, beam. A broken hand's turn is a breather, unless both are gone.
                var hand = turn == 0 ? _left : (turn == 1 ? _right : null);
                if (hand == null || (!_left.Alive && !_right.Alive)) yield return Beam();
                else if (hand.Alive) yield return Slam(hand);
                else yield return new WaitForSeconds(RestTime[_difficulty] * _faster);
            }
        }

        private IEnumerator Intro()
        {
            _input.MovementLocked = true;
            _camera.Focus = new Vector2(Centre, _row + 2.9f);
            _camera.Shake(0.12f, 1.8f);
            AudioPlayer.Instance.PlayAudio(AudioPlayer.Type.BossRumble);
            Jukebox.PlayMusic(_theme);

            _left.Setup(new Vector3(MapSystem.BridgeX - 2.4f, _row + 1.2f, 0f), Toughness(HandStrikes));
            _right.Setup(new Vector3(MapSystem.BridgeX + 2 + 2.4f, _row + 1.2f, 0f), Toughness(HandStrikes));
            _head.Setup(Toughness(HeadStrikes));
            _shadow.color = ShadowColour;

            StartCoroutine(_left.Rise(1.4f));
            StartCoroutine(_right.Rise(1.7f));
            yield return _head.Appear(new Vector3(Centre, _row + 5.1f, 0f), 2.4f);

            ObjectPools.Instance.GetPooledObject<TextPopup>().Enable("THE SCRAWL", new Vector2(Centre, _row + 1.7f), 2.5f, 2f);
            _bar.Show();
            OfferHead();
            yield return new WaitForSeconds(1.2f);
        }

        private int Toughness(float strikes)
        {
            var damage = Mathf.Max(1, PlayerDamage);
            return Mathf.RoundToInt(strikes * (1f + 0.5f / damage) * HealthScale[_difficulty]) * damage;
        }

        private IEnumerator Slam(ScrawlHand hand)
        {
            // It covers the knight's lane and the one on its own side; from an edge lane, only the knight's.
            var lane = KnightLane;
            var zone = lane == 1 ? new[] { 1, 1 + hand.Side } : new[] { lane };
            var first = Mathf.Min(zone[0], zone[zone.Length - 1]);
            var last = Mathf.Max(zone[0], zone[zone.Length - 1]);
            var over = MapSystem.BridgeX + (first + last) / 2f + hand.Side * 0.35f;
            var warning = SlamWarning[_difficulty] * _faster;

            hand.Raise(new Vector3(over, _row + 2.4f, 0f), warning);
            AudioPlayer.Instance.PlayAudio(AudioPlayer.Type.BossCharge);
            ShowShadow(first, last, _row - 0.5f, _row + 0.5f, warning);
            OfferDodges(lane, warning);
            yield return new WaitForSeconds(warning);
            ClearDodges();
            if (!Playing) yield break;

            yield return hand.Slam(new Vector3(over, _row + 0.15f, 0f));
            HideShadow();
            AudioPlayer.Instance.PlayAudio(AudioPlayer.Type.BossSlam);
            _camera.Shake(0.35f, 0.3f);
            for (var i = first; i <= last; i++)
                ObjectPools.Instance.GetPooledObject<Effect>().Play(Sprites.Instance.SmokeFrames, new Vector2(MapSystem.BridgeX + i, _row));
            if (Array.IndexOf(zone, KnightLane) >= 0) _attack.HurtPlayer(SlamDamage[_difficulty]);
            if (!Playing) yield break;
            yield return new WaitForSeconds(0.3f);

            // Recovering: it grips the edge of the bridge, and can be hurt until it lets go.
            hand.Grip(new Vector3(hand.Side < 0 ? MapSystem.BridgeX - 1.3f : MapSystem.BridgeX + 2 + 1.3f, _row + 0.6f, 0f));
            _resting.Add(hand);
            OfferHand(hand);
            var end = Time.time + RestTime[_difficulty] * _faster;
            while (Time.time < end && hand.Alive && _head.Alive && Playing) yield return null;
            CloseHand(hand);
            if (hand.Alive && _head.Alive) hand.Lift();
        }

        private IEnumerator Beam()
        {
            var lane = KnightLane;
            var warning = SlamWarning[_difficulty] * 0.7f * _faster;
            _head.Charge(warning);
            AudioPlayer.Instance.PlayAudio(AudioPlayer.Type.BossCharge);
            ShowShadow(lane, lane, _row - 3f, _head.transform.position.y - 1f, warning);
            OfferDodges(lane, warning);
            yield return new WaitForSeconds(warning);
            ClearDodges();
            HideShadow();
            if (!Playing || !_head.Alive) yield break;

            AudioPlayer.Instance.PlayAudio(AudioPlayer.Type.BossBeam);
            _camera.Shake(0.2f, 0.6f);
            var fire = StartCoroutine(_head.Fire(MapSystem.BridgeX + lane, _row - 3f, 0.6f));
            yield return new WaitForSeconds(0.08f);
            if (KnightLane == lane) _attack.HurtPlayer(BeamDamage[_difficulty]);
            yield return fire;
        }

        // ---- Words --------------------------------------------------------------------------------------------------

        private void OfferDodges(int lane, float seconds)
        {
            if (lane > 0) AddDodge(-1, seconds);
            if (lane < MapSystem.BridgeLanes - 1) AddDodge(1, seconds);
        }

        private void AddDodge(int direction, float seconds)
        {
            var word = _input.AddWord(DodgeLength, () => Dodge(direction));
            var hint = ObjectPools.Instance.GetPooledObject<SpellHint>();
            hint.Initialize(KnightTransform, word, new Vector2(direction * 1.9f, 0.2f));
            hint.SetIcon(direction < 0 ? SpellHint.IconType.DodgeLeft : SpellHint.IconType.DodgeRight);
            hint.ShowTimer(seconds);
            _dodges.Add(new Prompt { Word = word, Hint = hint });
        }

        private void Dodge(int direction)
        {
            Knight.Position += new Vector2Int(direction, 0);
            AudioPlayer.Instance.PlayAudio(AudioPlayer.Type.Step);
            ClearDodges();
        }

        private void ClearDodges()
        {
            foreach (var dodge in _dodges) Remove(dodge);
            _dodges.Clear();
        }

        private void OfferHand(ScrawlHand hand)
        {
            if (!hand.Alive || !_resting.Contains(hand)) return;
            var prompt = new Prompt();
            prompt.Word = _input.AddWord(HandWordLength, () =>
                {
                    _handPrompts.Remove(hand);
                    prompt.Hint.Disable();
                    Strike(hand.transform.position, () =>
                        {
                            HurtHand(hand);
                            if (!_handPrompts.ContainsKey(hand)) OfferHand(hand);
                        });
                });
            prompt.Hint = ObjectPools.Instance.GetPooledObject<SpellHint>();
            prompt.Hint.Initialize(hand.transform, prompt.Word, new Vector2(0f, 1.6f));
            prompt.Hint.SetIcon(SpellHint.IconType.Attack);
            _handPrompts[hand] = prompt;
        }

        private void CloseHand(ScrawlHand hand)
        {
            _resting.Remove(hand);
            Prompt prompt;
            if (!_handPrompts.TryGetValue(hand, out prompt)) return;
            Remove(prompt);
            _handPrompts.Remove(hand);
        }

        private void OfferHead()
        {
            if (!_head.Alive) return;
            var prompt = new Prompt();
            prompt.Word = _input.AddWord(HeadWordLength, () =>
                {
                    _headPrompt = null;
                    prompt.Hint.Disable();
                    Strike(_head.transform.position, () =>
                        {
                            HurtHead();
                            if (_headPrompt == null) OfferHead();
                        });
                });
            prompt.Hint = ObjectPools.Instance.GetPooledObject<SpellHint>();
            prompt.Hint.Initialize(_head.transform, prompt.Word, new Vector2(3.2f, 0.4f));
            prompt.Hint.SetIcon(SpellHint.IconType.Attack);
            _headPrompt = prompt;
        }

        private void Remove(Prompt prompt)
        {
            _input.RemoveWord(prompt.Word);
            prompt.Hint.Disable();
        }

        // ---- Hits ---------------------------------------------------------------------------------------------------

        private void Strike(Vector2 target, Action onHit)
        {
            ObjectPools.Instance.GetPooledObject<Fireball>().Animate(KnightTransform.position, target, 0.3f, onHit);
        }

        private void Wound(Vector2 at, int damage)
        {
            ObjectPools.Instance.GetPooledObject<TextPopup>().Enable((-damage).ToString(), at + Vector2.up, 1f);
            ObjectPools.Instance.GetPooledObject<Effect>().Play(Sprites.Instance.HitFrames, at);
            AudioPlayer.Instance.PlayAudio(AudioPlayer.Type.BossHit);
        }

        private void HurtHand(ScrawlHand hand)
        {
            if (!hand.Alive || !_head.Alive) return;
            var damage = PlayerDamage;
            hand.Health = Mathf.Max(0, hand.Health - damage);
            hand.Hit();
            Wound(hand.transform.position, damage);
            _bar.Set(hand.Side < 0 ? BossBar.LeftHand : BossBar.RightHand, (float)hand.Health / hand.MaxHealth);
            if (hand.Alive) return;

            // Broken: it sinks into the void, and its turns are a breather from now on.
            CloseHand(hand);
            RunStats.Kill();
            AudioPlayer.Instance.PlayAudio(AudioPlayer.Type.BossSink);
            _camera.Shake(0.3f, 0.6f);
            StartCoroutine(hand.Sink());
            if (!_left.Alive && !_right.Alive) StartCoroutine(_head.Advance(new Vector3(Centre, _row + 4.4f, 0f)));
        }

        private void HurtHead()
        {
            if (!_head.Alive) return;
            var damage = PlayerDamage;
            _head.Health = Mathf.Max(0, _head.Health - damage);
            _head.Hit();
            Wound(_head.transform.position, damage);
            _bar.Set(BossBar.Head, (float)_head.Health / _head.MaxHealth);
            if (_head.Alive)
            {
                if (_head.Health * 2 <= _head.MaxHealth) _faster = Desperate;
                return;
            }

            RunStats.Kill();
            RunStats.ScrawlBeaten();
            if (_fight != null) StopCoroutine(_fight);
            StartCoroutine(Defeat());
        }

        private IEnumerator Defeat()
        {
            ClearDodges();
            CloseHand(_left);
            CloseHand(_right);
            if (_headPrompt != null) Remove(_headPrompt);
            _headPrompt = null;
            HideShadow();
            _head.StopAll();

            AudioPlayer.Instance.PlayAudio(AudioPlayer.Type.BossDeath);
            _camera.Shake(0.45f, 2f);
            Jukebox.PlayMusic(null);
            if (_left.Alive) StartCoroutine(_left.Sink());
            if (_right.Alive) StartCoroutine(_right.Sink());
            yield return _head.Shatter();
            for (var i = 0; i < 5; i++)
            {
                var puff = new Vector2(Centre + UnityEngine.Random.Range(-1.5f, 1.5f), _head.transform.position.y + UnityEngine.Random.Range(-1.5f, 1.5f));
                ObjectPools.Instance.GetPooledObject<Effect>().Play(Sprites.Instance.SmokeFrames, puff);
            }

            _bar.Hide();
            yield return new WaitForSeconds(0.8f);

            // The way on is open.
            _camera.Focus = null;
            _input.MovementLocked = false;
            _items.OpenExit();
        }

        // ---- Warnings -----------------------------------------------------------------------------------------------

        private void ShowShadow(int firstLane, int lastLane, float bottom, float top, float seconds)
        {
            // The white sprite is 4 art pixels: a quarter of a unit.
            var left = MapSystem.BridgeX + firstLane - 0.5f;
            var right = MapSystem.BridgeX + lastLane + 0.5f;
            _shadow.transform.position = new Vector3((left + right) / 2f, (bottom + top) / 2f, 0f);
            _shadow.transform.localScale = new Vector3((right - left) * 4f, (top - bottom) * 4f, 1f);
            _shadow.DOKill();
            _shadow.color = ShadowColour;
            _shadow.DOFade(ShadowAlpha, seconds).SetEase(Ease.InQuad);
        }

        private void HideShadow()
        {
            _shadow.DOKill();
            _shadow.DOFade(0f, 0.15f);
        }

        private sealed class Prompt
        {
            public string Word;

            public SpellHint Hint;
        }
    }
}
