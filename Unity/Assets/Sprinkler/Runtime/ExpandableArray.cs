using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Sprinkler
{
    public class ExpandableArray<T> where T : struct
    {
        private T[] _array;
        private int _length;
        private int _capacity;

        public ExpandableArray(int sz)
        {
            SetCapacity(sz);
        }

        public void SetCapacity(int sz)
        {
            if (_array == null)
            {
                _array = new T[sz];
                _capacity = sz;
                _length = 0;
                return;
            }

            if (_capacity >= sz) return;

            var newArray = new T[sz];
            _array.CopyTo(newArray, 0);
            _capacity = sz;
            _array = newArray;
        }

        public int Length => _length;
        public int Capacity => _capacity;
        public T[] Array => _array;
        public void Clear() => _length = 0;

        public void Add(T v)
        {
            if (_length >= _capacity)
            {
                SetCapacity(_capacity * 2);
            }

            _array[_length++] = v;
        }

        public ref T this[int idx]
        {
            get
            {
                Assert.IsTrue(0 <= idx);
                Assert.IsTrue(idx < _length);
                return ref _array[idx];
            }
            //set
            //{
            //    Assert.IsTrue(0 <= idx);
            //    Assert.IsTrue(idx < _length);
            //    ref _array[idx] = value;
            //}
        }
    }

    public class ExpandableCharArray : ExpandableArray<char>
    {
        public ExpandableCharArray(int sz) : base(sz)
        {
        }

        public void Add(string s)
        {
            for (int i = 0; i < s.Length; ++i) Add(s[i]);
        }
    }
}
