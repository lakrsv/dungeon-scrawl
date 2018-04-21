// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="HintCache.cs" author="Lars" company="None">
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

namespace UI.Hint
{
    using System;
    using System.Collections.Generic;

    using Utilities.Game;

    public class HintCache : MonoSingleton<HintCache>
    {
        private readonly List<SpellHint> _hints = new List<SpellHint>();

        public List<SpellHint> GetCached()
        {
            return _hints;
        }

        public void Add(SpellHint hint)
        {
            if (_hints.Contains(hint)) throw new InvalidOperationException("Hints already contains this hint!");
            _hints.Add(hint);
        }

        public bool Contains(SpellHint hint)
        {
            return _hints.Contains(hint);
        }

        public void Remove(SpellHint hint)
        {
            if (!_hints.Contains(hint)) throw new InvalidOperationException("Hints do not contain this hint!");
            _hints.Remove(hint);
        }
    }
}