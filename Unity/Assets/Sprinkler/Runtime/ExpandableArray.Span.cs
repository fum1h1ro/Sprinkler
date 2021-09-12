using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Sprinkler
{
    public partial class ExpandableArray<T> where T : struct
    {
        public struct Span
        {
            private readonly ExpandableArray<T> _ref;
            private readonly int _start;
            private readonly int _length;

            internal Span(ExpandableArray<T> array, int start, int length)
            {
                _ref = array;
                _start = start;
                _length = length;
            }

            public ref T this[int idx]
            {
                get
                {
                    Assert.IsTrue(0 <= idx);
                    Assert.IsTrue(idx < _length);
                    return ref _ref[_start + idx];
                }
            }

            public int Length => _length;
        }
    }
}
