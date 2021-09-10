using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TMPro;

namespace Sprinkler
{
    public class TextProcessor : IDisposable
    {
        public const char TagStartChar = '<';
        public const char TagEndChar = '>';

        public enum CommandType
        {
            Put,
            Wait,
        }

        public enum AnimType : sbyte
        {
            Normal,
            Quake,
            Shout,
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct Command
        {
            [FieldOffset(0)] public CommandType Type;
            [FieldOffset(4)] public int Count;
            [FieldOffset(4)] public float Wait;
        }

        public struct CharAttribute
        {
            public AnimType AnimType;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct CharParameter
        {
            public struct QuakeParam
            {
                public float Time;
            }

            public struct ShoutParam
            {
                public float Time;
            }

            [FieldOffset(0)] public QuakeParam Quake;
            [FieldOffset(0)] public ShoutParam Shout;
        }

        public struct TagParam
        {
            public ReadOnlySpan Value;
            public int Start;
        }

        private readonly TMP_Text _text;
        private AnimType _currentAnim;
        private TagParser _tag = new TagParser();
        private List<Command> _commands = new List<Command>();
        private ExpandableCharArray _buffer = new ExpandableCharArray(128);
        private ExpandableArray<CharAttribute> _attrs = new ExpandableArray<CharAttribute>(128);
        private ExpandableArray<CharParameter> _params = new ExpandableArray<CharParameter>(128);
        private Dictionary<string, TagParam> _openedTags = new Dictionary<string, TagParam>();

        public int Length => _buffer.Length;
        public char[] ToArray() => _buffer.Array;
        public List<Command> Commands => _commands;
        public ExpandableArray<CharAttribute> Attributes => _attrs;
        public ExpandableArray<CharParameter> Parameters => _params;

        public TextProcessor(TMP_Text tmptext)
        {
            _text = tmptext;
            SetText(_text.text);
        }

        public void Dispose()
        {
        }

        public void SetText(string t)
        {
            Parse(t);
        }

        private void AddChar(char c, bool isVisible)
        {
            //Debug.Log($"{_currentAnim}");
            _buffer.Add(c);
            if (isVisible)
            {
                _attrs.Add(new CharAttribute{ AnimType = _currentAnim });
                _params.Add(new CharParameter());
            }
        }

        private void AddString(string s, bool isVisible)
        {
            foreach (var c in s) AddChar(c, isVisible);
        }

        private void Parse(string src)
        {
            var lex = new Lexer(src);
            _commands.Clear();
            _buffer.Clear();
            _attrs.Clear();
            _params.Clear();
            _currentAnim = AnimType.Normal;
            _openedTags.Clear();

            foreach (var span in lex)
            {
                //Debug.Log($"{span.ToString()}");
                if (span[0] == TagStartChar)
                {
                    _tag.Parse(span);

                    if (_tag.IsCloseTag)
                    {
                        CloseTag(src, span);
                    }
                    else
                    {
                        OpenTag(span);
                    }
                }
                else if (_openedTags.Keys.Count == 0)
                {
                    for (int i = 0; i < span.Length; ++i)
                    {
                        _commands.Add(new Command{ Type = CommandType.Put, Count = 1 });
                        AddChar(span[i], true);
                    }
                }
            }
        }

        private void OpenTag(ReadOnlySpan span)
        {
            if (_tag.Name.Equals("wait"))
            {
                var vals = new TextSplitter(_tag.Value);
                Assert.AreEqual(vals.Count(), 1);
                foreach (var e in vals)
                {
                    _commands.Add(new Command{ Type = CommandType.Wait, Wait = NumberParser.Parse(e) });
                    return;
                }
            }
            if (_tag.Name.Equals("quake"))
            {
                Assert.AreEqual(_currentAnim, AnimType.Normal);
                _currentAnim = AnimType.Quake;
                return;
            }
            if (_tag.Name.Equals("shout"))
            {
                Assert.AreEqual(_currentAnim, AnimType.Normal);
                _currentAnim = AnimType.Shout;
                return;
            }
            if (_tag.Name.Equals("ruby"))
            {
                Assert.IsFalse(_openedTags.ContainsKey("ruby"));
                _openedTags["ruby"] = new TagParam{ Value = _tag.Value, Start = span.End + 1 };
                return;
            }

            foreach (var s in span)
            {
                AddChar(s, false);
            }
        }

        private void CloseTag(string src, ReadOnlySpan span)
        {
            if (_tag.Name.Equals("quake"))
            {
                //Assert.IsTrue(_openedTags.ContainsKey("quake"));
                Assert.AreEqual(_currentAnim, AnimType.Quake);
                _currentAnim = AnimType.Normal;
                return;
            }
            if (_tag.Name.Equals("shout"))
            {
                //Assert.IsTrue(_openedTags.ContainsKey("quake"));
                Assert.AreEqual(_currentAnim, AnimType.Shout);
                _currentAnim = AnimType.Normal;
                return;
            }
            if (_tag.Name.Equals("ruby"))
            {
                Assert.IsTrue(_openedTags.ContainsKey("ruby"));
                // @todo string...
                var ruby = _openedTags["ruby"].Value.ToString();
                var body = (new ReadOnlySpan(src, _openedTags["ruby"].Start, span.Start - 1)).ToString();
                var rubySize = _text.GetPreferredValues($"<size=50%>{ruby}</size>");
                var bodySize = _text.GetPreferredValues(body);

                var prefix = (bodySize.x < rubySize.x)? (rubySize.x - bodySize.x) * 0.5f : 0;
                var offset0 = -(bodySize.x + rubySize.x) * 0.5f;
                var offset1 = (-rubySize.x + bodySize.x) * 0.5f + prefix;
                AddString($"<space={prefix}>", false);
                AddString(body, true);
                AddString($"<space={offset0}><voffset=1em><size=50%>", false);
                AddString(ruby, true);
                AddString($"</size></voffset><space={offset1}>", false);

                for (int i = 0; i < body.Length - 1; ++i)
                {
                    _commands.Add(new Command{ Type = CommandType.Put, Count = 1 });
                }
                _commands.Add(new Command{ Type = CommandType.Put, Count = 1 + ruby.Length });

                _openedTags.Remove("ruby");
                return;
            }

            foreach (var s in span)
            {
                AddChar(s, false);
            }
        }
    }
}
