using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;

namespace Sprinkler.Tests
{
    public class ReadOnlySpanTest
    {
        [TestCase("hoge", 0, 4, "hoge")]
        [TestCase("hoge", 1, 3, "oge")]
        [TestCase("hoge", 2, 2, "ge")]
        [TestCase("hoge", 0, 3, "hog")]
        [TestCase("hoge", 1, 2, "og")]
        [TestCase("hoge", 2, 1, "g")]
        public void Simple(string str, int s, int len, string answer)
        {
            var span = new ReadOnlySpan(str, s, len);
            Assert.AreEqual(span.ToString(), answer);
        }

        [TestCase("hoge", 0, 4, "hoge")]
        [TestCase("hoge", 1, 3, "oge")]
        [TestCase("hoge", 1, 2, "og")]
        public void SliceTest(string str, int s, int len, string answer)
        {
            var span = new ReadOnlySpan(str);
            Assert.AreEqual(span.Slice(s, len).ToString(), answer);
        }

        [TestCase("hoge", "hoge")]
        [TestCase(" hoge", "hoge")]
        [TestCase("hoge ", "hoge")]
        [TestCase(" hoge ", "hoge")]
        [TestCase("  hoge  ", "hoge")]
        [TestCase("\thoge\t  ", "hoge")]
        public void TrimTest(string str, string answer)
        {
            Assert.AreEqual((new ReadOnlySpan(str)).Trim().ToString(), answer);
        }

        [TestCase("hoge", "ho", true)]
        [TestCase("hoge", "ge", true)]
        [TestCase("hogehage", "ha", true)]
        [TestCase("hogehage", "hi", false)]
        [TestCase("hoge", "hogehage", false)]
        public void ContainsTest(string src, string search, bool answer)
        {
            Assert.AreEqual((new ReadOnlySpan(src)).Contains(search), answer);
        }

        [TestCase("hoge", 'h', 0)]
        [TestCase("hoge", 'o', 1)]
        [TestCase("hogehage", 'g', 2)]
        [TestCase("hogehage", 'e', 3)]
        [TestCase("hoge", '2', -1)]
        public void IndexOfCharTest(string src, char c, int answer)
        {
            Assert.AreEqual((new ReadOnlySpan(src)).IndexOf(c), answer);
        }

        [TestCase("hoge", "ho", 0)]
        [TestCase("hoge", "ge", 2)]
        [TestCase("hogehage", "ha", 4)]
        [TestCase("hogehage", "hi", -1)]
        [TestCase("hoge", "hogehage", -1)]
        public void IndexOfStringTest(string src, string search, int answer)
        {
            Assert.AreEqual((new ReadOnlySpan(src)).IndexOf(search), answer);
        }
    }
}
