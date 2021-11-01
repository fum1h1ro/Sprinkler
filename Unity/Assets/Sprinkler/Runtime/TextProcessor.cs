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
    public class TextProcessor
    {
        public const char TagStartChar = '<';
        public const char TagEndChar = '>';
        public const char SpecialStartChar = '&';
        public const char SpecialEndChar = ';';
        public const char VariableStartChar = '{';
        public const char VariableEndChar = '}';

        public enum CommandType
        {
            Put,
            Wait,
            Speed,
            Callback,
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
            public struct CallbackParam
            {
                public int Index;
            }

            [FieldOffset(4)] public PutParam Put;
            [FieldOffset(4)] public WaitParam Wait;
            [FieldOffset(4)] public SpeedParam Speed;
            [FieldOffset(4)] public CallbackParam Callback;
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

        public interface ICustomReplaceTagProcessor
        {
            void Process(Result result, ref TagParser tag);
        }

        public interface ICustomVariableProcessor
        {
            void Process(Result result, ReadOnlySpan name);
        }

        private delegate void TagProc(bool isOpen, ref TagParser tag, ReadOnlySpan span);

        private static HashSet<ReadOnlySpan> _customThroughTags = new HashSet<ReadOnlySpan>();
        private static Dictionary<ReadOnlySpan, ICustomReplaceTagProcessor> _customReplaceTags = new Dictionary<ReadOnlySpan, ICustomReplaceTagProcessor>();
        private static HashSet<ReadOnlySpan> _customCallbackTags = new HashSet<ReadOnlySpan>();
        private static ICustomVariableProcessor _customVariableProcessor = null;

        private readonly TMP_Text _text;
        private Dictionary<string, TagParam> _openedTags = new Dictionary<string, TagParam>();
        private Dictionary<ReadOnlySpan, TagProc> _tagProc = new Dictionary<ReadOnlySpan, TagProc>();
        private string _sourceText;
        private CharAttribute _currentAttr;

        private Vector2 GetPreferredValues(string text)
        {
            return (_text == null)? Vector2.one : _text.GetPreferredValues(text);
        }

        public class Result
        {
            private CharAttribute _currentAttr;
            private (int BufStart, int AttrStart, int CommandStart) _pageStart;
            //
            private ExpandableArray<Command> _commands = new ExpandableArray<Command>(128);
            private ExpandableCharArray _buffer = new ExpandableCharArray(128);
            private ExpandableArray<CharAttribute> _attrs = new ExpandableArray<CharAttribute>(128);
            private List<PageSpan> _pages = new List<PageSpan>(8);
            private ExpandableArray<(ReadOnlySpan, ReadOnlySpan)> _callbackParams = new ExpandableArray<(ReadOnlySpan, ReadOnlySpan)>(16);

            public int Length => _buffer.Length;
            public char[] ToArray() => _buffer.Array;
            public ExpandableArray<Command> Commands => _commands;
            public ExpandableArray<CharAttribute> Attributes => _attrs;
            public ExpandableArray<(ReadOnlySpan, ReadOnlySpan)> CallbackParams => _callbackParams;
            public int PageCount => _pages.Count;
            public PageSpan GetPageSpan(int idx) => _pages[idx];

            public Result()
            {
                Clear();
            }

            public void Clear()
            {
                _currentAttr = new CharAttribute();
                _pageStart = (0, 0, 0);
                //
                _commands.Clear();
                _buffer.Clear();
                _attrs.Clear();
                _pages.Clear();
                _callbackParams.Clear();
            }

            public void SetCurrentAttribute(CharAttribute attr)
            {
                _currentAttr = attr;
            }

            public void AddChar(char c, bool isVisible)
            {
                _buffer.Add(c);
                if (isVisible)
                {
                    _attrs.Add(_currentAttr);
                    var cmd = new Command{ Type = CommandType.Put };
                    cmd.Put.Count = 1;
                    _commands.Add(cmd);
                }
            }

            public void AddString(string s, bool isVisible)
            {
                foreach (var c in s) AddChar(c, isVisible);
            }

            public void AddCommand(Command cmd)
            {
                _commands.Add(cmd);
            }

            public void PageBreak()
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
        }

        static TextProcessor()
        {
            AddCustomThroughTag("color");
        }

        // 何も処理せずそのままTMPに渡されるタグ
        public static void AddCustomThroughTag(string tag)
        {
            _customThroughTags.Add(new ReadOnlySpan(tag));
        }

        // パース時に特殊処理を挟むタグ
        public static void AddCustomReplaceTag(string tag, ICustomReplaceTagProcessor custom)
        {
            var key = new ReadOnlySpan(tag);
            //Assert.IsFalse(_customReplaceTags.ContainsKey(key));
            _customReplaceTags[key] = custom;
        }

        // 表示時に特殊処理をしたいタグ
        public static void AddCustomCallbackTag(string tag)
        {
            _customCallbackTags.Add(new ReadOnlySpan(tag));
        }

        // 
        public static void SetCustomVariableProcessor(ICustomVariableProcessor proc)
        {
            _customVariableProcessor = proc;
        }

        public TextProcessor(TMP_Text tmptext)
        {
            _text = tmptext;
            _tagProc[new ReadOnlySpan(Tags.Quake)] = TagQuake;
            _tagProc[new ReadOnlySpan(Tags.Shout)] = TagShout;
            _tagProc[new ReadOnlySpan(Tags.Fade)] = TagFade;
        }

        public void Parse(string src, Result result)
        {
            var lex = new Lexer(src);
            _openedTags.Clear();
            _sourceText = src;
            _currentAttr = new CharAttribute();

            //var result = new Result();
            result.Clear();

            foreach (var span in lex)
            {
                if (span[0] == TagStartChar)
                {
                    var tag = new TagParser(span);

                    if (tag.IsCloseTag)
                    {
                        CloseTag(result, ref tag, src, span);
                    }
                    else
                    {
                        OpenTag(result, ref tag, span);
                    }

                    result.SetCurrentAttribute(_currentAttr);
                }
                else if (span[0] == SpecialStartChar)
                {
                    var c = (new SpecialParser(span)).Result;
                    result.AddChar(c, true);
                }
                else if (span[0] == VariableStartChar)
                {
                    if (_customVariableProcessor != null)
                    {
                        _customVariableProcessor.Process(result, span.Slice(1, span.Length - 2).Trim());
                    }
                }
                else if (_openedTags.Keys.Count == 0)
                {
                    for (int i = 0; i < span.Length; ++i)
                    {
                        result.AddChar(span[i], true);
                    }
                }
            }

            result.PageBreak();
        }

        private void PreventInnerText(ref TagParser tag, ReadOnlySpan span)
        {
            _openedTags[tag.Name.ToString()] = new TagParam{ Value = tag.Value, Start = span.End };
        }

        private ReadOnlySpan GetInnerText(ref TagParser tag, ReadOnlySpan span)
        {
            var tagStart = _openedTags[tag.Name.ToString()].Start;
            return new ReadOnlySpan(_sourceText, tagStart, span.Start - tagStart);
        }

        private void ApproveInnerText(ref TagParser tag)
        {
            _openedTags.Remove(tag.Name.ToString());
        }

        private void OpenTag(Result result, ref TagParser tag, ReadOnlySpan span)
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
                    result.AddCommand(cmd);
                    return;
                }
            }
            if (tag.Name.Equals(Tags.Break))
            {
                result.PageBreak();
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
                    result.AddCommand(cmd);
                    return;
                }
            }
            if (tag.Name.Equals(Tags.Ruby))
            {
                Assert.AreEqual(_openedTags.Keys.Count, 0);
                PreventInnerText(ref tag, span);
                return;
            }
            if (_customReplaceTags.ContainsKey(tag.Name))
            {
                _customReplaceTags[tag.Name].Process(result, ref tag);
                return;
            }
            if (_customCallbackTags.Contains(tag.Name))
            {
                var cmd = new Command{ Type = CommandType.Callback };
                cmd.Callback.Index = result.CallbackParams.Length;
                result.AddCommand(cmd);
                result.CallbackParams.Add((tag.Name, tag.Value));
                return;
            }
            if (_customThroughTags.Contains(tag.Name))
            {
                foreach (var s in span) result.AddChar(s, false);
                return;
            }

            throw new Exception($"invalid tag: {tag.Name.ToString()}\nin: \"{_sourceText}\"");
        }

        private void CloseTag(Result result, ref TagParser tag, string src, ReadOnlySpan span)
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
                var rubySize = GetPreferredValues($"<size=50%>{ruby}</size>");
                var bodySize = GetPreferredValues(body);

                var prefix = (bodySize.x < rubySize.x)? (rubySize.x - bodySize.x) * 0.5f : 0;
                var offset0 = -(bodySize.x + rubySize.x) * 0.5f;
                var offset1 = (-rubySize.x + bodySize.x) * 0.5f + prefix;
                result.AddString($"<space={prefix}>", false);
                result.AddString(body, true);
                result.AddString($"<space={offset0}><voffset=1em><size=50%>", false);
                result.AddString(ruby, true);
                result.AddString($"</size></voffset><space={offset1}>", false);

                for (int i = 0; i < body.Length - 1; ++i)
                {
                    var cmd = new Command{ Type = CommandType.Put };
                    cmd.Put.Count = 1;
                    result.AddCommand(cmd);
                }
                {
                    var cmd = new Command{ Type = CommandType.Put };
                    cmd.Put.Count = 1 + ruby.Length;
                    result.AddCommand(cmd);
                }

                ApproveInnerText(ref tag);
                return;
            }
            if (_customReplaceTags.ContainsKey(tag.Name))
            {
                _customReplaceTags[tag.Name].Process(result, ref tag);
                return;
            }
            if (_customThroughTags.Contains(tag.Name))
            {
                foreach (var s in span) result.AddChar(s, false);
                return;
            }

            throw new Exception($"invalid tag: {tag.Name.ToString()}\nin: \"{_sourceText}\"");
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
            if (isOpen)
            {
                var values = new TextSplitter(tag.Value, ',');
                var count = values.Count();
                Assert.IsTrue(0 <= count && count <= 2);

                if (count == 2)
                {
                    _currentAttr.Quake.Horizontal = NumberParser.Parse(values[0]).ToFX8();
                    _currentAttr.Quake.Vertical = NumberParser.Parse(values[1]).ToFX8();
                }
                else if (count == 1)
                {
                    _currentAttr.Quake.Horizontal = _currentAttr.Quake.Vertical = NumberParser.Parse(values[0]).ToFX8();
                }
                else
                {
                    _currentAttr.Quake.Horizontal = _currentAttr.Quake.Vertical = 0.1f.ToFX8();
                }
            }
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
                    _currentAttr.Shout.Scale = NumberParser.Parse(values[0]).ToFX8();
                    _currentAttr.Shout.GrowSpeed = NumberParser.Parse(values[1]).ToFX8();
                    _currentAttr.Shout.ShrinkSpeed = NumberParser.Parse(values[2]).ToFX8();
                }
                else if (count == 2)
                {
                    _currentAttr.Shout.Scale = NumberParser.Parse(values[0]).ToFX8();
                    _currentAttr.Shout.GrowSpeed = _currentAttr.Shout.ShrinkSpeed = (NumberParser.Parse(values[1]) * 0.5f).ToFX8();
                }
                else if (count == 1)
                {
                    _currentAttr.Shout.Scale = NumberParser.Parse(values[0]).ToFX8();
                    _currentAttr.Shout.GrowSpeed = _currentAttr.Shout.ShrinkSpeed = 0.125f.ToFX8();
                }
                else
                {
                    _currentAttr.Shout.Scale = 1.25f.ToFX8();
                    _currentAttr.Shout.GrowSpeed = _currentAttr.Shout.ShrinkSpeed = 0.125f.ToFX8();
                }
            }
            EffectTag(isOpen, TextEffects.TypeFlag.Shout);
        }

        private void TagFade(bool isOpen, ref TagParser tag, ReadOnlySpan span) => EffectTag(isOpen, TextEffects.TypeFlag.Fade);
    }
}
