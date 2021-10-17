using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Sprinkler.Tests
{
    public class TagParserTest
    {
        [TestCase("<tag>", "tag", false, false, "")]
        [TestCase("<tag=1>", "tag", false, true, "1")]
        [TestCase("<tag=10>", "tag", false, true, "10")]
        [TestCase("<tag=100% >", "tag", false, true, "100%")]
        [TestCase("<tag=0 1 2>", "tag", false, true, "0 1 2")]
        [TestCase("</tag>", "tag", true, false, "")]
        public void Simple(string s, string name, bool isClose, bool hasValue, string value)
        {
            var tag = new TagParser(s);
            Assert.AreEqual(name, tag.Name.ToString());
            Assert.AreEqual(isClose, tag.IsCloseTag);
            Assert.AreEqual(hasValue, tag.HasValue);
            Assert.AreEqual(value, tag.Value.ToString());
        }
    }

    public class LexerTest
    {
        [TestCase("HOGE", 1)]
        [TestCase("HOGE<>", 2)]
        [TestCase("HOGE<>HAGE", 3)]
        [TestCase("HOGE<>HAGE<HIGE>", 4)]
        [TestCase("<>HAGE<HIGE>", 3)]
        [TestCase("<   >HAGE < HIGE >", 3)]
        public void CountTest(string str, int count)
        {
            Assert.AreEqual((new Lexer(str)).Count(), count);
            foreach (var l in new Lexer(str))
            {
                Debug.Log(l.ToString());
            }

        }
    }
}
