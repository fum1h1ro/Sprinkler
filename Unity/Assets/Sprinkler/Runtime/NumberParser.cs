using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEngine;

namespace Sprinkler
{
    [StructLayout(LayoutKind.Explicit)]
    public struct NumberParser
    {
        private enum Type
        {
            Uint,
            Float,
        }

        [FieldOffset(0)]
        public readonly uint UintValue;
        [FieldOffset(0)]
        public readonly float FloatValue;
        [FieldOffset(4)]
        private readonly Type ValueType;

        public bool IsUint => ValueType == Type.Uint;
        public bool IsFloat => ValueType == Type.Float;

        public NumberParser(ReadOnlySpan src)
        {
            var span = src.Trim();

            UintValue = 0;
            FloatValue = 0.0f;

            // hex
            if (span[0] == '#')
            {
                uint m = 1;
                for (int i = span.Length - 1; i >= 1; --i)
                {
                    var c = span[i];
                    if (char.IsDigit(c))
                    {
                        UintValue += (uint)(c - '0') * m;
                    }
                    else if ('a' <= c && c <= 'f')
                    {
                        UintValue += (uint)(c - 'a' + 10) * m;
                    }
                    else if ('A' <= c && c <= 'F')
                    {
                        UintValue += (uint)(c - 'A' + 10) * m;
                    }
                    else
                    {
                        throw new Exception("invalid character");
                    }
                    m *= 16;
                }
                ValueType = Type.Uint;
                return;
            }

            int sign = (span[0] == '-')? -1 : 1;
            int start = -1;
            int end = -1;
            bool hasPoint = false;
            for (int i = 0; i < span.Length; ++i)
            {
                var c = span[i];
                if (start < 0)
                {
                    if (!char.IsDigit(c)) continue;
                    start = i;
                    continue;
                }
                if (!char.IsDigit(c))
                {
                    hasPoint = c == '.';
                    end = i;
                    break;
                }
            }
            if (end < 0) end = span.Length;
            FloatValue = ParseSpan(span.Slice(start, end - start));

            if (hasPoint)
            {
                start = end + 1;
                end = -1;
                for (int i = start; i < span.Length; ++i)
                {
                    var c = span[i];
                    if (!char.IsDigit(c))
                    {
                        end = i;
                        break;
                    }
                }
                if (end < 0) end = span.Length;
                var r = span.Slice(start, end - start);
                float m = 1.0f;
                for (int j = 0; j < r.Length; ++j) m *= 10.0f;
                FloatValue += (float)ParseSpan(r) * (1.0f/m);
            }

            FloatValue *= sign;
            ValueType = Type.Float;
        }

        private static int ParseSpan(ReadOnlySpan span)
        {
            int v = 0;
            int m = 1;
            for (int i = span.Length - 1; i >= 0; --i)
            {
                v += (span[i] - '0') * m;
                m *= 10;
            }
            return v;
        }

        public static float Parse(ReadOnlySpan src)
        {
            return (new NumberParser(src)).FloatValue;
        }
    }
}
