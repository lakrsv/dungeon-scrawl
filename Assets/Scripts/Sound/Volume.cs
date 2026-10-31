// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="Volume.cs" author="Lars" company="None">
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
// //   The player's music and sound volume.
// // </summary>
// // --------------------------------------------------------------------------------------------------------------------

namespace Sound
{
    using UnityEngine;

    /// <summary>
    /// The player's music and sound volume: a level each, from full down to off, kept between sessions.
    /// </summary>
    public static class Volume
    {
        public const int MaxLevel = 3;

        private const string MusicKey = "MusicVolume";

        private const string SoundKey = "SoundVolume";

        private static int _music = -1;

        private static int _sound = -1;

        public static int MusicLevel
        {
            get
            {
                if (_music < 0) _music = PlayerPrefs.GetInt(MusicKey, MaxLevel);
                return _music;
            }

            set
            {
                _music = Save(MusicKey, value);
            }
        }

        public static int SoundLevel
        {
            get
            {
                if (_sound < 0) _sound = PlayerPrefs.GetInt(SoundKey, MaxLevel);
                return _sound;
            }

            set
            {
                _sound = Save(SoundKey, value);
            }
        }

        /// <summary>
        /// Gets the gain for the music and the ambience.
        /// </summary>
        public static float Music
        {
            get { return Gain(MusicLevel); }
        }

        /// <summary>
        /// Gets the gain for everything else.
        /// </summary>
        public static float Sound
        {
            get { return Gain(SoundLevel); }
        }

        // Each level down halves the volume, which sounds like an even step.
        private static float Gain(int level)
        {
            return level <= 0 ? 0f : Mathf.Pow(0.5f, MaxLevel - level);
        }

        private static int Save(string key, int level)
        {
            level = Mathf.Clamp(level, 0, MaxLevel);
            PlayerPrefs.SetInt(key, level);
            PlayerPrefs.Save();
            return level;
        }
    }
}
