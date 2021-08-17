using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sprinkler
{
    // テキストを空白で分ける
    public struct TextSplitter
    {
        public readonly ReadOnlySpan Source;
        public readonly char Separator;

        public TextSplitter(string src, char sep=' ')
        {
            Source = (new ReadOnlySpan(src)).Trim();
            Separator = sep;
        }

        public TextSplitter(ReadOnlySpan src, char sep=' ')
        {
            Source = src.Trim();
            Separator = sep;
        }

        // @todo IEnumerable<>が実装されてないので
        public int Count()
        {
            int count = 0;
            foreach (var _ in this) ++count;
            return count;
        }

        public struct Enumerator : IEnumerator<ReadOnlySpan>
        {
            private readonly ReadOnlySpan _src;
            private readonly char _sep;
            private int _start;
            private int _end;

            public Enumerator(ReadOnlySpan src, char sep)
            {
                _src = src;
                _sep = sep;
                _start = _end = -1;
            }

            public void Dispose() {}

            public ReadOnlySpan Current => _src.Slice(_start, _end);
            object IEnumerator.Current => Current;

            public void Reset()
            {
                _start = _end = -1;
            }

            public bool MoveNext()
            {
                _start = _end + 1;
                _end = -1;

                for (int i = _start; i < _src.Length; ++i)
                {
                    var c = _src[i];
                    if (c == _sep) continue;
                    _start = i;
                    break;
                }

                if (_start >= _src.Length) return false;

                for (int i = _start + 1; i < _src.Length; ++i)
                {
                    var c = _src[i];
                    if (c != _sep) continue;
                    _end = i - 1;
                    break;
                }
                if (_end < 0) _end = _src.Length - 1;

                return true;
            }
        }

        // @todo 本当はIEnumerable<>を実装したいんだけど、そうさせてくれない
        public Enumerator GetEnumerator()
        {
            return new Enumerator(Source, Separator);
        }
    }
}
