using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sprinkler
{
    public class TagParser
    {
        public bool IsCloseTag { get; private set; }
        public bool HasValue { get; private set; }
        public ReadOnlySpan Name { get; private set; }
        public ReadOnlySpan Value { get; private set; }

        public void Parse(string s) => Parse(new ReadOnlySpan(s, 0, s.Length - 1));

        public void Parse(ReadOnlySpan src)
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
                    if (!char.IsLetter(c)) keyE = i - 1;
                    if (c == '=')
                    {
                        HasValue = true;
                    }
                    continue;
                }

                valS = i;
                break;
            }
            if (keyE < 0) keyE = span.Length - 2;

            Name = span.Slice(keyS, keyE);

            if (!HasValue || valS < 0) return;

            for (int i = span.Length - 2; i >= valS; --i)
            {
                var c = span[i];
                if (char.IsWhiteSpace(c)) continue;
                valE = i;
                break;
            }

            Value = span.Slice(valS, valE);
        }
    }
}
