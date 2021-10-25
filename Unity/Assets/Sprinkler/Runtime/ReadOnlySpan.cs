using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Sprinkler
{
    // @todo いつかサポートされるその時まで
    public struct ReadOnlySpan :
        //IEnumerable<char>,
        IEquatable<ReadOnlySpan>,
        IEquatable<string>,
        IEnumerable
    {
        private readonly string _ref;
        private readonly int _start;
        private readonly int _end;

        public static readonly ReadOnlySpan Empty = new ReadOnlySpan();

        public ReadOnlySpan(string r)
        {
            _ref = r;
            _start = 0;
            _end = r.Length;
        }

        public ReadOnlySpan(string r, int s, int len)
        {
            Assert.IsNotNull(r);
            Assert.IsTrue(0 <= s);
            //Assert.IsTrue(0 <= e);
            //Assert.IsTrue(s <= e);

            _ref = r;
            _start = s;
            _end = s + len;
        }

        public int Start => _start;
        public int End => _end;
        public int Length => _end - _start;

        public char this[int idx]
        {
            get
            {
                Assert.IsTrue(0 <= idx);
                Assert.IsTrue(_start + idx < _end);
                return _ref[_start + idx];
            }
        }

        public new string ToString()
        {
            if (_ref == null || string.IsNullOrEmpty(_ref)) return "";
            return _ref.Substring(_start, Length);
        }

        [System.Diagnostics.Contracts.Pure]
        public ReadOnlySpan Slice(int s, int len) => new ReadOnlySpan(_ref, _start + s, len);

        public ReadOnlySpan Trim()
        {
            if (Length == 0) return this;

            int start = -1;
            int end = -1;

            for (int i = 0; i < Length; ++i)
            {
                var c = this[i];
                if (char.IsWhiteSpace(c)) continue;
                start = i;
                break;
            }

            for (int i = Length - 1; i >= start; --i)
            {
                var c = this[i];
                if (char.IsWhiteSpace(c)) continue;
                end = i + 1;
                break;
            }

            return Slice(start, end - start);
        }

        public struct Enumerator : IEnumerator<char>
        {
            private readonly ReadOnlySpan _src;
            private int _index;

            public Enumerator(ReadOnlySpan src)
            {
                _src = src;
                _index = -1;
            }

            public void Dispose() {}

            public char Current => _src[_index];
            object IEnumerator.Current => Current;

            public void Reset() => _index = -1;

            public bool MoveNext()
            {
                if (++_index < _src.Length) return true;
                return false;
            }
        }

        // 明示的に構造体を返さないとboxing
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        // @todo 本当はこっちも実装しておきたいのだが
        //public IEnumerator<char> GetEnumerator()
        //{
        //    return new Enumerator(this);
        //}

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Equals(ReadOnlySpan b)
        {
            if (Length != b.Length) return false;

            for (int i = 0; i < Length; ++i)
            {
                if (this[i] != b[i]) return false;
            }

            return true;
        }

        public bool Equals(string b)
        {
            return this.Equals(new ReadOnlySpan(b));
        }

        public override int GetHashCode()
        {
            int c = 0;
            for (int i = 0; i < Length; ++i)
            {
                c ^= this[i];
            }
            return c;
        }

        public bool Contains(string value) => IndexOf(value) >= 0;

        public int IndexOf(char c)
        {
            for (var i = 0; i < Length; ++i)
            {
                if (this[i] == c) return i;
            }
            return -1;
        }

        public int IndexOf(string value)
        {
            for (var i = 0; i < Length; ++i)
            {
                if (this[i] == value[0])
                {
                    bool isSame = true;
                    for (var j = 1; j < value.Length; ++j)
                    {
                        if (i+j >= Length || this[i+j] != value[j])
                        {
                            isSame = false;
                            break;
                        }
                    }
                    if (isSame) return i;
                }
            }
            return -1;
        }
    }
}
