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
// //   TODO - Insert file description
// // </summary>
// // --------------------------------------------------------------------------------------------------------------------

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

        private Random _random;

        public Sprite GetDeathSprite()
        {
            if (_random == null) _random = new Random(Constants.RandomSeed);
            var index = _random.Next(0, _deathSprites.Length);

            return _deathSprites[index];
        }

        public Sprite GetDemonSprite()
        {
            if (_random == null) _random = new Random(Constants.RandomSeed);
            var index = _random.Next(0, _demonSprites.Length);

            return _demonSprites[index];
        }

        public Sprite GetPestSprite()
        {
            if (_random == null) _random = new Random(Constants.RandomSeed);
            var index = _random.Next(0, _pestSprites.Length);

            return _pestSprites[index];
        }

        public Sprite GetSlimeSprite()
        {
            if (_random == null) _random = new Random(Constants.RandomSeed);
            var index = _random.Next(0, _slimeSprites.Length);

            return _slimeSprites[index];
        }

        public Sprite GetChestSprite()
        {
            if (_random == null) _random = new Random(Constants.RandomSeed);
            var index = _random.Next(0, _chestSprites.Length);

            return _chestSprites[index];
        }
    }
}