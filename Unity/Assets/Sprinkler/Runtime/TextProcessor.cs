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
        public const char TagOpenChar = '<';
        public const char TagCloseChar = '>';

        public enum CommandType
        {
            Put,
            Wait,
        }

        [System.Flags]
        public enum AnimFlag
        {
            Normal = 0,
            Quake = (1<<0),
            Wave = (1<<1),
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct Command
        {
            [FieldOffset(0)] public CommandType Type;
            [FieldOffset(4)] public int Count;
            [FieldOffset(4)] public float Wait;
        }

        public struct Attr
        {
            public AnimFlag Flag;
        }

        public struct TagParam
        {
            public ReadOnlySpan Value;
            public int Start;
        }

        private readonly TMP_Text _text;
        private AnimFlag _currentFlag;
        private TagParser _tag = new TagParser();
        private List<Command> _commands = new List<Command>();
        private ExpandableCharArray _buffer = new ExpandableCharArray(128);
        private ExpandableArray<Attr> _attrs = new ExpandableArray<Attr>(128);
        private Dictionary<string, TagParam> _openedTags = new Dictionary<string, TagParam>();

        public int Length => _buffer.Length;
        public char[] ToArray() => _buffer.Array;
        public List<Command> Commands => _commands;
        public ExpandableArray<Attr> Attributes => _attrs;

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

        private void AddChar(char c)
        {
            _buffer.Add(c);
            _attrs.Add(new Attr{ Flag = _currentFlag });
        }

        private void AddString(string s)
        {
            foreach (var c in s) AddChar(c);
        }

        private void Parse(string src)
        {
            var lex = new Lexer(src);
            _commands.Clear();
            _buffer.Clear();
            _attrs.Clear();
            _currentFlag = AnimFlag.Normal;

            foreach (var span in lex)
            {
                if (span[0] == TagOpenChar)
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
                        AddChar(span[i]);
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
                _currentFlag |= AnimFlag.Quake;
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
                AddChar(s);
            }
        }

        private void CloseTag(string src, ReadOnlySpan span)
        {
            if (_tag.Name.Equals("quake"))
            {
                Assert.IsTrue(_openedTags.ContainsKey("quake"));
                _currentFlag &= ~AnimFlag.Quake;
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
                AddString($"<space={prefix}>{body}<space={offset0}><voffset=1em><size=50%>{ruby}</size></voffset><space={offset1}>");

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
                AddChar(s);
            }
        }
    }
}
