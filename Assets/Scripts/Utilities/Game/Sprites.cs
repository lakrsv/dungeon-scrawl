// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="Sprites.cs" author="Lars" company="None">
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
// //   Every creature sprite, their idle animations, and the effect frames.
// // </summary>
// // --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using UnityEngine;

using Random = System.Random;

namespace Utilities.Game
{
    public class Sprites : MonoSingleton<Sprites>
    {
        [SerializeField]
        private Sprite[] _deathSprites;

        [SerializeField]
        private Sprite[] _demonSprites;

        [SerializeField]
        private Sprite[] _slimeSprites;

        [SerializeField]
        private Sprite[] _pestSprites;

        [SerializeField]
        private Sprite[] _chestSprites;

        // Idle loops. Each starts with a creature's own sprite, which actors keep as their identity.
        [SerializeField]
        private IdleAnimation[] _animations;

        [SerializeField]
        private float _animationFramesPerSecond = 2f;

        [SerializeField]
        private Sprite[] _hitFrames;

        [SerializeField]
        private Sprite[] _smokeFrames;

        private Dictionary<Sprite, Sprite[]> _framesBySprite;

        private Random _random;

        public Sprite[] HitFrames
        {
            get
            {
                return _hitFrames;
            }
        }

        public Sprite[] SmokeFrames
        {
            get
            {
                return _smokeFrames;
            }
        }

        /// <summary>
        /// The frame to draw for a sprite at a point in time. Sprites without an animation are drawn as they are.
        /// </summary>
        public Sprite GetFrame(Sprite sprite, float time)
        {
            if (sprite == null) return null;

            if (_framesBySprite == null)
            {
                _framesBySprite = new Dictionary<Sprite, Sprite[]>();
                foreach (var animation in _animations) _framesBySprite[animation.Frames[0]] = animation.Frames;
            }

            Sprite[] frames;
            if (!_framesBySprite.TryGetValue(sprite, out frames)) return sprite;

            var index = Mathf.FloorToInt(time * _animationFramesPerSecond) % frames.Length;
            return frames[index];
        }

        public Sprite GetDeathSprite()
        {
            if (_random == null) _random = new Random(Constants.GetRandomSeed());
            var index = _random.Next(0, _deathSprites.Length);

            return _deathSprites[index];
        }

        public Sprite GetDemonSprite()
        {
            if (_random == null) _random = new Random(Constants.GetRandomSeed());
            var index = _random.Next(0, _demonSprites.Length);

            return _demonSprites[index];
        }

        public Sprite GetPestSprite()
        {
            if (_random == null) _random = new Random(Constants.GetRandomSeed());
            var index = _random.Next(0, _pestSprites.Length);

            return _pestSprites[index];
        }

        public Sprite GetSlimeSprite()
        {
            if (_random == null) _random = new Random(Constants.GetRandomSeed());
            var index = _random.Next(0, _slimeSprites.Length);

            return _slimeSprites[index];
        }

        public Sprite GetChestSprite()
        {
            if (_random == null) _random = new Random(Constants.GetRandomSeed());
            var index = _random.Next(0, _chestSprites.Length);

            return _chestSprites[index];
        }

        [Serializable]
        public class IdleAnimation
        {
            public Sprite[] Frames;
        }
    }
}