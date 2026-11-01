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
    /// With nothing hostile in sight, the move prompts are single keys: W, A, S and D (the default); Z, Q, S and D,
    /// which sit in the same place on an AZERTY keyboard; or a random letter per direction. The player picks on the
    /// main menu or in the options, and the choice is kept.
    /// </summary>
    public static class MoveKeys
    {
        // Saved as 1 for WASD and 0 for letters, as when that was the only choice, and 2 for ZQSD.
        private const string PrefsKey = "MoveKeys";

        private static readonly Layout[] Order = { Layout.Wasd, Layout.Zqsd, Layout.Letters };

        private static int _keys = -1;

        public enum Layout
        {
            Letters = 0,

            Wasd = 1,

            Zqsd = 2
        }

        public static Layout Keys
        {
            get
            {
                if (_keys < 0) _keys = PlayerPrefs.GetInt(PrefsKey, (int)Layout.Wasd);
                return (Layout)Mathf.Clamp(_keys, 0, 2);
            }

            set
            {
                _keys = (int)value;
                PlayerPrefs.SetInt(PrefsKey, _keys);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Gets a value indicating whether each direction has its own fixed key (WASD or ZQSD), rather than a letter.
        /// </summary>
        public static bool Fixed
        {
            get { return Keys != Layout.Letters; }
        }

        /// <summary>
        /// Gets the name of the choice, as the menus show it.
        /// </summary>
        public static string Name
        {
            get { return Keys == Layout.Letters ? "LETTERS" : Keys.ToString().ToUpperInvariant(); }
        }

        // Up, left, down, right.
        private static string Set
        {
            get { return Keys == Layout.Zqsd ? "zqsd" : "wasd"; }
        }

        /// <summary>
        /// Moves to the next choice (or the previous one, with a step of -1), round and round.
        /// </summary>
        public static void Next(int step = 1)
        {
            var at = System.Array.IndexOf(Order, Keys);
            Keys = Order[((at + step) % Order.Length + Order.Length) % Order.Length];
        }

        public static string For(Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    return Set.Substring(0, 1);
                case Direction.Left:
                    return Set.Substring(1, 1);
                case Direction.Down:
                    return Set.Substring(2, 1);
                default:
                    return Set.Substring(3, 1);
            }
        }

        /// <summary>
        /// Whether a word would get in the way of the move keys: with WASD or ZQSD on, the first letter of a word must
        /// never be one of them, or typing it would move instead.
        /// </summary>
        public static bool Blocks(string word)
        {
            return Fixed && word.Length > 0 && Set.IndexOf(word[0]) >= 0;
        }
    }
}
