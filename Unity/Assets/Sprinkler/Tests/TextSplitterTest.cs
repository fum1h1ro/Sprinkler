using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Profiling;

namespace Sprinkler.Tests
{
    public class TextSplitterTest
    {
        [TestCase("hoge", 1)]
        [TestCase(" hoge ", 1)]
        [TestCase("hoge hage", 2)]
        [TestCase(" hoge hage ", 2)]
        [TestCase("hoge hage hige", 3)]
        [TestCase(" hoge hage hige ", 3)]
        [TestCase("  hoge  hage  hige  ", 3)]
        public void CountTest(string str, int count)
        {
            Assert.AreEqual((new TextSplitter(str, ' ')).Count(), count);
        }
    }
}
