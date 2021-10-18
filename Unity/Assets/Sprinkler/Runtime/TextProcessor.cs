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
            public readonly int Start;
            public readonly int Length;
            public readonly int AttrStart;
            public readonly int AttrLength;
            public readonly int CommandStart;
            public readonly int CommandLength;

            public PageSpan(int start, int len, int astart, int alen, int cstart, int clen)
            {
                Start = start;
                Length = len;
                AttrStart = astart;
                AttrLength = alen;
                CommandStart = cstart;
                CommandLength = clen;
            }
        }

        private struct TagParam
        {
            public ReadOnlySpan Value;
            public int Start;
        }

        private delegate void TagProc(bool isOpen, ref TagParser tag, ReadOnlySpan span);

        private readonly TMP_Text _text;
        //private TextEffects.TypeFlag _currentFlag;
        private CharAttribute _currentAttr;
        private ExpandableArray<Command> _commands = new ExpandableArray<Command>(128);
        private ExpandableCharArray _buffer = new ExpandableCharArray(128);
        private ExpandableArray<CharAttribute> _attrs = new ExpandableArray<CharAttribute>(128);
        private Dictionary<string, TagParam> _openedTags = new Dictionary<string, TagParam>();
        private Dictionary<ReadOnlySpan, TagProc> _tagProc = new Dictionary<ReadOnlySpan, TagProc>();
        private List<PageSpan> _pages = new List<PageSpan>(8);
        private (int BufStart, int AttrStart, int CommandStart) _pageStart;

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
                //_attrs.Add(new CharAttribute{ AnimType = _currentFlag, Time = 0.0f });
                _attrs.Add(_currentAttr);
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
            _currentAttr = new CharAttribute();
            _openedTags.Clear();
            _pageStart = (0, 0, 0);
            _pages.Clear();

            foreach (var span in lex)
            {
                if (span[0] == TagStartChar)
                {
                    var tag = new TagParser(span);

                    if (tag.IsCloseTag)
                    {
                        CloseTag(ref tag, src, span);
                    }
                    else
                    {
                        OpenTag(ref tag, span);
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
        }

        private void PageBreak()
        {
            var start = _pageStart.BufStart;
            var len = _buffer.Length - start;
            var astart = _pageStart.AttrStart;
            var alen = _attrs.Length - astart;
            var cstart = _pageStart.CommandStart;
            var clen = _commands.Length - cstart;
            _pages.Add(new PageSpan(start, len, astart, alen, cstart, clen));
            _pageStart = (_buffer.Length, _attrs.Length, _commands.Length);
        }

        private void OpenTag(ref TagParser tag, ReadOnlySpan span)
        {
            if (_tagProc.ContainsKey(tag.Name))
            {
                _tagProc[tag.Name](true, ref tag, span);
                return;
            }

            if (tag.Name.Equals(Tags.Wait))
            {
                var vals = new TextSplitter(tag.Value);
                Assert.AreEqual(vals.Count(), 1);
                foreach (var e in vals)
                {
                    var cmd = new Command{ Type = CommandType.Wait };
                    cmd.Wait.Second = NumberParser.Parse(e);
                    _commands.Add(cmd);
                    return;
                }
            }
            if (tag.Name.Equals(Tags.Break))
            {
                PageBreak();
                return;
            }
            if (tag.Name.Equals(Tags.Speed))
            {
                var vals = new TextSplitter(tag.Value);
                Assert.AreEqual(vals.Count(), 1);
                foreach (var e in vals)
                {
                    var cmd = new Command{ Type = CommandType.Speed };
                    cmd.Speed.Scale = 1.0f / NumberParser.Parse(e);
                    _commands.Add(cmd);
                    return;
                }
            }
            if (tag.Name.Equals(Tags.Ruby))
            {
                Assert.IsFalse(_openedTags.ContainsKey(Tags.Ruby));
                _openedTags[Tags.Ruby] = new TagParam{ Value = tag.Value, Start = span.End };
                return;
            }

            foreach (var s in span)
            {
                AddChar(s, false);
            }
        }

        private void CloseTag(ref TagParser tag, string src, ReadOnlySpan span)
        {
            if (_tagProc.ContainsKey(tag.Name))
            {
                _tagProc[tag.Name](false, ref tag, span);
                return;
            }

            if (tag.Name.Equals(Tags.Ruby))
            {
                Assert.IsTrue(_openedTags.ContainsKey(Tags.Ruby));
                // @todo string...
                var ruby = _openedTags[Tags.Ruby].Value.ToString();
                var tagStart = _openedTags[Tags.Ruby].Start;
                var body = (new ReadOnlySpan(src, tagStart, span.Start - tagStart)).ToString();
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
                Assert.IsTrue((_currentAttr.AnimType & flag) == 0);
                _currentAttr.AnimType |= flag;
            }
            else
            {
                Assert.IsTrue((_currentAttr.AnimType & flag) != 0);
                _currentAttr.AnimType &= ~flag;
            }
        }

        private void TagQuake(bool isOpen, ref TagParser tag, ReadOnlySpan span)
        {
            EffectTag(isOpen, TextEffects.TypeFlag.Quake);
        }

        private void TagShout(bool isOpen, ref TagParser tag, ReadOnlySpan span)
        {
            if (isOpen)
            {
                var values = new TextSplitter(tag.Value, ',');
                var count = values.Count();
                Assert.IsTrue(0 <= count && count <= 3);

                if (count == 3)
                {
                    _currentAttr.Shout.Scale = (short)(256 * NumberParser.Parse(values[0]));
                    _currentAttr.Shout.GrowSpeed = (short)(256 * NumberParser.Parse(values[1]));
                    _currentAttr.Shout.ShrinkSpeed = (short)(256 * NumberParser.Parse(values[2]));
                }
                else if (count == 2)
                {
                    _currentAttr.Shout.Scale = (short)(256 * NumberParser.Parse(values[0]));
                    _currentAttr.Shout.GrowSpeed = _currentAttr.Shout.ShrinkSpeed = (short)(256 * NumberParser.Parse(values[1]) * 0.5f);
                }
                else if (count == 1)
                {
                    _currentAttr.Shout.Scale = (short)(256 * NumberParser.Parse(values[0]));
                    _currentAttr.Shout.GrowSpeed = _currentAttr.Shout.ShrinkSpeed = (short)(256 * 0.125f);
                }
                else
                {
                    _currentAttr.Shout.Scale = (short)(256 * 1.25f);
                    _currentAttr.Shout.GrowSpeed = _currentAttr.Shout.ShrinkSpeed = (short)(256 * 0.125f);
                }
            }

            EffectTag(isOpen, TextEffects.TypeFlag.Shout);
        }
        private void TagFade(bool isOpen, ref TagParser tag, ReadOnlySpan span) => EffectTag(isOpen, TextEffects.TypeFlag.Fade);
    }
}
