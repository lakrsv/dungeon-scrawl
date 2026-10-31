// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="RunStats.cs" author="Lars" company="None">
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
// //   The numbers of one run, for the game over and victory screens.
// // </summary>
// // --------------------------------------------------------------------------------------------------------------------

namespace Controllers
{
    using UnityEngine;

    /// <summary>
    /// The numbers of one run, from level 1 to the end, for the game over and victory screens. They are static, like
    /// <see cref="GameController.CurrentLevel"/>, so they survive the Game scene reloading for each level.
    /// </summary>
    public static class RunStats
    {
        public const int Levels = 5;

        private const string BestKey = "BestScore";

        // A gap longer than this between two letters of a word is the player thinking, not typing.
        private const float LongestKeystroke = 1.5f;

        // The score: every letter of a finished word, every kill, every chest and every level cleared, times the
        // difficulty (1 to 3).
        private const int PointsPerLetter = 1;

        private const int PointsPerKill = 25;

        private const int PointsPerChest = 50;

        private const int PointsPerLevel = 250;

        private static int _keystrokes;

        private static float _typingTime;

        private static float _lastLetter;

        public static int Difficulty { get; private set; }

        /// <summary>
        /// Gets the letters typed that fit a word.
        /// </summary>
        public static int Letters { get; private set; }

        /// <summary>
        /// Gets the letters typed that fit no word.
        /// </summary>
        public static int Mistakes { get; private set; }

        /// <summary>
        /// Gets the letters of every finished word.
        /// </summary>
        public static int WordLetters { get; private set; }

        public static int Kills { get; private set; }

        public static int Chests { get; private set; }

        public static int LevelsCleared { get; private set; }

        /// <summary>
        /// Gets the level the run ended on.
        /// </summary>
        public static int LevelReached { get; private set; }

        public static bool Won { get; private set; }

        /// <summary>
        /// Gets the time spent playing, without the level transitions.
        /// </summary>
        public static float Seconds { get; private set; }

        public static int Score { get; private set; }

        public static int Best { get; private set; }

        public static bool NewBest { get; private set; }

        /// <summary>
        /// Gets the typing speed while typing a word, in the usual five-letter words per minute.
        /// </summary>
        public static int WordsPerMinute
        {
            get { return _typingTime > 0f ? Mathf.RoundToInt(_keystrokes / 5f / (_typingTime / 60f)) : 0; }
        }

        /// <summary>
        /// Gets the share of letters that fit, in percent. Rounded down, so 100 means not a single slip.
        /// </summary>
        public static int Accuracy
        {
            get { return Letters + Mistakes > 0 ? Mathf.FloorToInt(100f * Letters / (Letters + Mistakes)) : 100; }
        }

        public static void Begin(int difficulty)
        {
            Difficulty = Mathf.Max(1, difficulty);
            Letters = Mistakes = WordLetters = Kills = Chests = LevelsCleared = LevelReached = 0;
            Won = NewBest = false;
            Seconds = 0f;
            Score = 0;
            _keystrokes = 0;
            _typingTime = 0f;
        }

        /// <summary>
        /// A letter that fits was typed. When it isn't the first of its word, the gap since the last one counts
        /// towards the typing speed.
        /// </summary>
        public static void Letter(bool continuesWord)
        {
            Letters++;
            var gap = Time.time - _lastLetter;
            if (continuesWord && gap <= LongestKeystroke)
            {
                _keystrokes++;
                _typingTime += gap;
            }

            _lastLetter = Time.time;
        }

        public static void Mistake()
        {
            Mistakes++;
        }

        public static void Word(int length)
        {
            WordLetters += length;
        }

        public static void Kill()
        {
            Kills++;
        }

        public static void Chest()
        {
            Chests++;
        }

        public static void LevelCleared()
        {
            LevelsCleared++;
        }

        public static void Tick(float deltaTime)
        {
            Seconds += deltaTime;
        }

        /// <summary>
        /// Ends the run: works out the score and keeps it if it beats the best.
        /// </summary>
        public static void Finish(bool won, int level)
        {
            Won = won;
            LevelReached = level;
            Score = Mathf.Max(1, Difficulty) * (WordLetters * PointsPerLetter + Kills * PointsPerKill
                                                + Chests * PointsPerChest + LevelsCleared * PointsPerLevel);

            Best = PlayerPrefs.GetInt(BestKey, 0);
            NewBest = Score > Best;
            if (!NewBest) return;

            Best = Score;
            PlayerPrefs.SetInt(BestKey, Best);
            PlayerPrefs.Save();
        }
    }
}
