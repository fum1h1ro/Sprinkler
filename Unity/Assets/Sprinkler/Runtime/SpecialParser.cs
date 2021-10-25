using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Sprinkler
{
    public struct SpecialParser
    {
        private static Dictionary<ReadOnlySpan, char> _table;
        public char Result { get; private set; }

        static SpecialParser()
        {
            var src = new (string, char)[]{
                ("lt", '<'),
                ("gt", '>'),
                ("nbsp", ' '),
                ("amp", '&'),
                ("quot", '"'),
                ("apos", '\''),
                ("copy", '©'),
            };

            _table = new Dictionary<ReadOnlySpan, char>();
            foreach (var s in src)
            {
                var key = new ReadOnlySpan(s.Item1);
                _table[key] = s.Item2;
            }
        }

        public SpecialParser(ReadOnlySpan span)
        {
            Assert.IsTrue(span.Length > 2);

            var body = span.Slice(1, span.Length - 2);

            if (body[0] == '#')
            {
                if (body[1] == 'x' || body[1] == 'X')
                {
                    var code = new NumberParser(body.Slice(2, body.Length - 2), 16).UintValue;
                    Result = (char)code;
                }
                else
                {
                    var code = (uint)(new NumberParser(body.Slice(1, body.Length - 1), 10).FloatValue);
                    Result = (char)code;
                }
            }
            else
            {
                if (!_table.ContainsKey(body))
                {
                    throw new System.Exception($"unknown code: {body.ToString()}");
                }
                Result = _table[body];
            }
        }
    }
}
