// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="WordSystem.cs" author="Lars" company="None">
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

namespace ECS.Systems
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using UnityEngine;

    using Utilities;

    using Random = System.Random;

    public class WordSystem : MonoBehaviour, IInitializeSystem
    {
        public const int MaxWordLength = 9;

        public const int MinWordLength = 3;

        private const string Adjectives = "adjectives";

        private const string Nouns = "nouns";

        private const string Verbs = "verbs";

        private const string WordFilePath = "Words/{0}";

        private readonly Dictionary<int, List<string>> _wordsByLength = new Dictionary<int, List<string>>();

        private readonly Dictionary<int, List<string>> _usedWordsByLength = new Dictionary<int, List<string>>();

        private Random _random;

        public string GetNextWord(int wordLength)
        {
            if (wordLength > MaxWordLength || wordLength < MinWordLength)
            {
                throw new ArgumentOutOfRangeException(
                    "wordLength",
                    string.Format(
                        "Requested word with {0} characters, but the word has to be between {1}-{2} characters long",
                        wordLength,
                        MinWordLength,
                        MaxWordLength));
            }

            var wordsWithLength = _wordsByLength[wordLength];
            var count = wordsWithLength.Count;

            if (count == 0)
            {
                _wordsByLength[wordLength] = new List<string>(_usedWordsByLength[wordLength]);
                _usedWordsByLength[wordLength].Clear();

                wordsWithLength = _wordsByLength[wordLength];
                count = wordsWithLength.Count;
            }

            var index = _random.Next(0, count);

            var word = wordsWithLength[index];
            wordsWithLength.Remove(word);

            if (_usedWordsByLength.ContainsKey(wordLength)) _usedWordsByLength[wordLength].Add(word);
            else _usedWordsByLength.Add(wordLength, new List<string> { word });

            return word;
        }

        public void Initialize()
        {
            _random = new Random(Constants.GetRandomSeed());

            PrepareWords(Adjectives);
            PrepareWords(Nouns);
            PrepareWords(Verbs);
        }

        private void PrepareWords(string fileName)
        {
            var filePath = string.Format(WordFilePath, fileName);
            var file = Resources.Load<TextAsset>(filePath);

            using (var reader = new StringReader(file.text))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var length = line.Length;

                    if (length > MaxWordLength || length < MinWordLength) continue;

                    if (_wordsByLength.ContainsKey(length)) _wordsByLength[length].Add(line);
                    else _wordsByLength.Add(length, new List<string> { line });
                }
            }
        }
    }
}