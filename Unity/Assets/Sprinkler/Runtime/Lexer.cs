using System;
using System.Collections;
using System.Collections.Generic;

namespace Sprinkler
{
    // テキストをタグとそれ以外に分ける
    public struct Lexer
    {
        public readonly string Source;

        public Lexer(string src)
        {
            Source = src;
        }

        public int Count()
        {
            var count = 0;
            foreach (var _ in this) ++count;
            return count;
        }

        public struct Enumerator : IEnumerator<ReadOnlySpan>
        {
            private readonly string _src;
            private int _start;
            private int _end;

            public Enumerator(string src)
            {
                _src = src;
                _start = _end = 0;
            }

            public void Dispose() {}

            public ReadOnlySpan Current => new ReadOnlySpan(_src, _start, _end - _start);
            object IEnumerator.Current => Current;

            public void Reset()
            {
                _start = _end = 0;
            }

            public bool MoveNext()
            {
                _start = _end;
                _end = -1;
                if (_start >= _src.Length) return false;

                bool isTag = _src[_start] == TextProcessor.TagStartChar;

                for (int i = _start; i < _src.Length; ++i)
                {
                    var c = _src[i];

                    if (!isTag && c == TextProcessor.TagStartChar)
                    {
                        _end = i;
                        break;
                    }
                    if (isTag && c == TextProcessor.TagEndChar)
                    {
                        _end = i + 1;
                        break;
                    }
                }
                if (_end < 0) _end = _src.Length;

                return true;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(Source);
        }
    }
}
