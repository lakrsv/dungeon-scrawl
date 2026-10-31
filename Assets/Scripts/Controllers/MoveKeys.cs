// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="MoveKeys.cs" author="Lars" company="None">
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
// //   Which keys move the player when nothing hostile is in sight.
// // </summary>
// // --------------------------------------------------------------------------------------------------------------------

namespace Controllers
{
    using UnityEngine;

    using Utilities.Game.Navigation;

    /// <summary>
    /// With nothing hostile in sight, the move prompts are single keys. They are either W, A, S and D (the default),
    /// or a random letter per direction. The player picks on the main menu, and the choice is kept.
    /// </summary>
    public static class MoveKeys
    {
        private const string PrefsKey = "MoveKeys";

        private static int _wasd = -1;

        public static bool Wasd
        {
            get
            {
                if (_wasd < 0) _wasd = PlayerPrefs.GetInt(PrefsKey, 1);
                return _wasd == 1;
            }

            set
            {
                _wasd = value ? 1 : 0;
                PlayerPrefs.SetInt(PrefsKey, _wasd);
                PlayerPrefs.Save();
            }
        }

        public static string For(Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    return "w";
                case Direction.Left:
                    return "a";
                case Direction.Down:
                    return "s";
                default:
                    return "d";
            }
        }

        /// <summary>
        /// Whether a word would get in the way of the WASD keys: with them on, the first letter of a word must never
        /// be one of them, or typing it would move instead.
        /// </summary>
        public static bool Blocks(string word)
        {
            return Wasd && word.Length > 0 && "wasd".IndexOf(word[0]) >= 0;
        }
    }
}
