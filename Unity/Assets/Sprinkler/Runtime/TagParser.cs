using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sprinkler
{
    public struct TagParser
    {
        public bool IsCloseTag { get; private set; }
        public bool HasValue { get; private set; }
        public ReadOnlySpan Name { get; private set; }
        public ReadOnlySpan Value { get; private set; }

        public TagParser(ReadOnlySpan span)
        {
            IsCloseTag = false;
            HasValue = false;
            Name = ReadOnlySpan.Empty;
            Value = ReadOnlySpan.Empty;
            Parse(span);
        }

        public TagParser(string s)
        {
            IsCloseTag = false;
            HasValue = false;
            Name = ReadOnlySpan.Empty;
            Value = ReadOnlySpan.Empty;
            Parse(new ReadOnlySpan(s, 0, s.Length));
        }

        private void Parse(ReadOnlySpan src)
        {
            IsCloseTag = false;
            HasValue = false;
            Name = ReadOnlySpan.Empty;
            Value = ReadOnlySpan.Empty;

            int keyS = -1;
            int keyE = -1;
            int valS = -1;
            int valE = -1;

            var span = src.Trim();
            for (int i = 1; i < span.Length - 1; ++i)
            {
                var c = span[i];

                if (keyS < 0)
                {
                    if (c == '/')
                    {
                        IsCloseTag = true;
                        continue;
                    }
                    keyS = i;
                    continue;
                }

                if (keyE < 0)
                {
                    if (!char.IsLetter(c)) keyE = i;
                    if (c == '=')
                    {
                        HasValue = true;
                    }
                    continue;
                }

                valS = i;
                break;
            }
            if (keyE < 0) keyE = span.Length - 1;

            Name = span.Slice(keyS, keyE - keyS);

            if (!HasValue || valS < 0) return;

            valE = span.Length - 1;

            Value = span.Slice(valS, valE - valS).Trim();
        }
    }
}
