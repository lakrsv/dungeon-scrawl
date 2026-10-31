// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="RunReport.cs" author="Lars" company="None">
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
// //   Shows the numbers of the run that just ended.
// // </summary>
// // --------------------------------------------------------------------------------------------------------------------

namespace UI
{
    using System;
    using System.Collections;
    using System.Globalization;

    using Controllers;

    using DG.Tweening;

    using Sound;

    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// The panel on the game over and victory screens. It tallies up the run one number at a time, each with a
    /// soft tick, then the score, then whether it beat the best.
    /// </summary>
    public class RunReport : MonoBehaviour
    {
        private const string Gold = "#dad45e";

        private const string Steel = "#8595a1";

        private const string Cream = "#deeed6";

        // How often the tally may tick while a number counts up.
        private const float TickInterval = 0.05f;

        private static readonly string[] DifficultyNames = { "NORMAL", "HARD", "IMPOSSIBLE" };

        private static readonly string[] DifficultyColors = { "#6daa2c", "#d27d2c", "#d04648" };

        [SerializeField]
        private Text _speed;

        [SerializeField]
        private Text _accuracy;

        [SerializeField]
        private Text _level;

        [SerializeField]
        private Text _kills;

        [SerializeField]
        private Text _chests;

        [SerializeField]
        private Text _time;

        [SerializeField]
        private Text _score;

        [SerializeField]
        private Text _difficulty;

        [SerializeField]
        private Text _best;

        [SerializeField]
        private float _delay = 0.75f;

        private float _lastTick;

        private static string Thousands(int value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture);
        }

        private static string Clock(int seconds)
        {
            return string.Format("{0}:{1:00}", seconds / 60, seconds % 60);
        }

        private IEnumerator Start()
        {
            foreach (var text in new[] { _speed, _accuracy, _level, _kills, _chests, _time, _score, _difficulty, _best })
                text.text = string.Empty;

            yield return new WaitForSeconds(_delay);

            yield return Count(_speed, RunStats.WordsPerMinute, v => v + " WPM");
            yield return Count(_kills, RunStats.Kills, v => v.ToString());
            yield return Count(_accuracy, RunStats.Accuracy, v => v + "%");
            yield return Count(_chests, RunStats.Chests, v => v.ToString());
            yield return Count(_level, RunStats.LevelReached, v => v + " OF " + RunStats.Levels);
            yield return Count(_time, Mathf.RoundToInt(RunStats.Seconds), Clock);

            var difficulty = Mathf.Clamp(RunStats.Difficulty, 1, DifficultyNames.Length) - 1;
            _difficulty.text = string.Format(
                "<color={0}>{1}</color>{2}",
                DifficultyColors[difficulty],
                DifficultyNames[difficulty],
                difficulty > 0 ? string.Format(" <color={0}>x{1}</color>", Steel, difficulty + 1) : string.Empty);
            Pop(_difficulty);

            yield return Count(_score, RunStats.Score, Thousands, 1.0f);

            if (RunStats.NewBest)
            {
                _best.text = string.Format("<color={0}>NEW BEST!</color>", Gold);
                _best.transform.DOPunchScale(new Vector3(0.3f, 0.3f, 0f), 0.4f, 1, 0f);
                Jukebox.Select();

                // Blinks like the cursors of the typed prompts.
                DOTween.Sequence()
                    .AppendInterval(0.5f)
                    .AppendCallback(() => _best.enabled = !_best.enabled)
                    .SetLoops(-1)
                    .SetTarget(_best);
            }
            else
            {
                _best.text = string.Format("BEST <color={0}>{1}</color>", Cream, Thousands(RunStats.Best));
                Pop(_best);
            }
        }

        // A button can leave the screen mid-tally.
        private void OnDestroy()
        {
            DOTween.Kill(this);
            DOTween.Kill(_best);
        }

        // Counts the number up from zero, ticking as it goes, and gives it a little pop when it lands.
        private IEnumerator Count(Text text, int target, Func<int, string> format, float duration = 0.3f)
        {
            var shown = -1;
            var count = DOTween.To(() => 0, value =>
                {
                    if (value == shown) return;
                    shown = value;
                    text.text = format(value);
                    Tick();
                }, target, target > 0 ? duration : 0f).SetEase(Ease.OutQuad).SetTarget(this);

            yield return count.WaitForCompletion();
            text.text = format(target);
            Pop(text);
            yield return new WaitForSeconds(0.08f);
        }

        private void Pop(Text text)
        {
            text.transform.DOPunchScale(new Vector3(0.15f, 0.15f, 0f), 0.2f, 1, 0f);
            Tick();
        }

        private void Tick()
        {
            if (Time.time - _lastTick < TickInterval) return;
            _lastTick = Time.time;
            Jukebox.Hover();
        }
    }
}
