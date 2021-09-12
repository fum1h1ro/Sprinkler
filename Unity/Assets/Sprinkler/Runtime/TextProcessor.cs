using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TMPro;
using Sprinkler.TextEffects;

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
            Speed,
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct Command
        {
            [FieldOffset(0)] public CommandType Type;

            public struct PutParam
            {
                public int Count;
            }
            public struct WaitParam 
            {
                public float Second;
            }
            public struct SpeedParam 
            {
                public float Scale;
            }

            [FieldOffset(4)] public PutParam Put;
            [FieldOffset(4)] public WaitParam Wait;
            [FieldOffset(4)] public SpeedParam Speed;
        }

        public struct PageSpan
        {
            public int Start;
            public int Length;
            public int AttrStart;
            public int AttrLength;

            public PageSpan(int start, int len, int astart, int alen)
            {
                Start = start;
                Length = len;
                AttrStart = astart;
                AttrLength = alen;
            }
        }

        private struct TagParam
        {
            public ReadOnlySpan Value;
            public int Start;
        }

        private readonly TMP_Text _text;
        private TextEffects.TypeFlag _currentFlag;
        private TagParser _tag = new TagParser();
        private ExpandableArray<Command> _commands = new ExpandableArray<Command>(128);
        private ExpandableCharArray _buffer = new ExpandableCharArray(128);
        private ExpandableArray<CharAttribute> _attrs = new ExpandableArray<CharAttribute>(128);
        private Dictionary<string, TagParam> _openedTags = new Dictionary<string, TagParam>();
        private Dictionary<ReadOnlySpan, Action<bool, ReadOnlySpan>> _tagProc = new Dictionary<ReadOnlySpan, Action<bool, ReadOnlySpan>>();
        private List<PageSpan> _pages = new List<PageSpan>(8);
        private (int, int) _pageStart;

        public int Length => _buffer.Length;
        public char[] ToArray() => _buffer.Array;
        public ExpandableArray<Command> Commands => _commands;
        public ExpandableArray<CharAttribute> Attributes => _attrs;
        public int PageCount => _pages.Count;
        public PageSpan GetPageSpan(int idx) => _pages[idx];

        public TextProcessor(TMP_Text tmptext)
        {
            _text = tmptext;

            _tagProc[new ReadOnlySpan(Tags.Quake)] = TagQuake;
            _tagProc[new ReadOnlySpan(Tags.Shout)] = TagShout;
            _tagProc[new ReadOnlySpan(Tags.Fade)] = TagFade;

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
            _buffer.Add(c);
            if (isVisible)
            {
                _attrs.Add(new CharAttribute{ AnimType = _currentFlag, Time = 0.0f });
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
            _currentFlag = 0;
            _openedTags.Clear();
            _pageStart = (0, 0);
            _pages.Clear();

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
                        var cmd = new Command{ Type = CommandType.Put };
                        cmd.Put.Count = 1;
                        _commands.Add(cmd);
                        AddChar(span[i], true);
                    }
                }
            }

            PageBreak();
            Debug.Log($"{_pages.Count}");
            foreach (var p in _pages)
            {
                Debug.Log($"{p.Start} {p.Length} {p.AttrStart} {p.AttrLength}");
            }
        }

        private void PageBreak()
        {
            var start = _pageStart.Item1;
            var len = _buffer.Length - start;
            var astart = _pageStart.Item2;
            var alen = _attrs.Length - astart;
            _pages.Add(new PageSpan(start, len, astart, alen));
            _pageStart = (_buffer.Length, _attrs.Length);
        }

        private void OpenTag(ReadOnlySpan span)
        {
            if (_tagProc.ContainsKey(_tag.Name))
            {
                _tagProc[_tag.Name](true, span);
                return;
            }

            if (_tag.Name.Equals(Tags.Wait))
            {
                var vals = new TextSplitter(_tag.Value);
                Assert.AreEqual(vals.Count(), 1);
                foreach (var e in vals)
                {
                    var cmd = new Command{ Type = CommandType.Wait };
                    cmd.Wait.Second = NumberParser.Parse(e);
                    _commands.Add(cmd);
                    return;
                }
            }
            if (_tag.Name.Equals(Tags.Break))
            {
                PageBreak();
                return;
            }
            if (_tag.Name.Equals(Tags.Speed))
            {
                var vals = new TextSplitter(_tag.Value);
                Assert.AreEqual(vals.Count(), 1);
                foreach (var e in vals)
                {
                    var cmd = new Command{ Type = CommandType.Speed };
                    cmd.Speed.Scale = 1.0f / NumberParser.Parse(e);
                    _commands.Add(cmd);
                    return;
                }
            }
            if (_tag.Name.Equals(Tags.Ruby))
            {
                Assert.IsFalse(_openedTags.ContainsKey(Tags.Ruby));
                _openedTags[Tags.Ruby] = new TagParam{ Value = _tag.Value, Start = span.End + 1 };
                return;
            }

            foreach (var s in span)
            {
                AddChar(s, false);
            }
        }

        private void CloseTag(string src, ReadOnlySpan span)
        {
            if (_tagProc.ContainsKey(_tag.Name))
            {
                _tagProc[_tag.Name](false, span);
                return;
            }

            if (_tag.Name.Equals(Tags.Ruby))
            {
                Assert.IsTrue(_openedTags.ContainsKey(Tags.Ruby));
                // @todo string...
                var ruby = _openedTags[Tags.Ruby].Value.ToString();
                var body = (new ReadOnlySpan(src, _openedTags[Tags.Ruby].Start, span.Start - 1)).ToString();
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
                    var cmd = new Command{ Type = CommandType.Put };
                    cmd.Put.Count = 1;
                    _commands.Add(cmd);
                }
                {
                    var cmd = new Command{ Type = CommandType.Put };
                    cmd.Put.Count = 1 + ruby.Length;
                    _commands.Add(cmd);
                }

                _openedTags.Remove(Tags.Ruby);
                return;
            }

            foreach (var s in span)
            {
                AddChar(s, false);
            }
        }

        private void EffectTag(bool isOpen, TextEffects.TypeFlag flag)
        {
            if (isOpen)
            {
                Assert.IsTrue((_currentFlag & flag) == 0);
                _currentFlag |= flag;
            }
            else
            {
                Assert.IsTrue((_currentFlag & flag) != 0);
                _currentFlag &= ~flag;
            }
        }

        private void TagQuake(bool isOpen, ReadOnlySpan span) => EffectTag(isOpen, TextEffects.TypeFlag.Quake);
        private void TagShout(bool isOpen, ReadOnlySpan span) => EffectTag(isOpen, TextEffects.TypeFlag.Shout);
        private void TagFade(bool isOpen, ReadOnlySpan span) => EffectTag(isOpen, TextEffects.TypeFlag.Fade);
    }
}
